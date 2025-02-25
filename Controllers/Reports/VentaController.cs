using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        [HttpPost("api/v1/reporteria/ventas")]
        public async Task<IActionResult> ObtenerVentas(
            [FromBody] ReporteriaRequest request,
            [FromQuery] bool sum = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            int offset = (page - 1) * pageSize;

            var baseQuery = @"
            FROM [LOCAL_TC032391E].[dbo].[Temp_VentasReport]";

            var whereClauses = new List<string>();
            var sumaClauses = new List<string>();
            var parameters = new List<SqlParameter>();
            var parameterCounters = new Dictionary<string, int>();

            // Procesar filtros
            var fechaEmisionParams = request.Filtros.Where(f => f.Key == "FechaEmision").ToList();
            bool fechaRangeProcessed = false;

            // Manejo de rango de fechas si hay exactamente dos filtros
            if (fechaEmisionParams.Count == 2)
            {
                var minFecha = fechaEmisionParams.FirstOrDefault(f => f.Operator == ">=");
                var maxFecha = fechaEmisionParams.FirstOrDefault(f => f.Operator == "<=");

                if (minFecha != null && maxFecha != null)
                {
                    whereClauses.Add("FechaEmision BETWEEN @FechaEmisionMin AND @FechaEmisionMax");
                    parameters.Add(new SqlParameter("@FechaEmisionMin", DateTime.Parse(minFecha.Value)));
                    parameters.Add(new SqlParameter("@FechaEmisionMax", DateTime.Parse(maxFecha.Value)));
                    fechaRangeProcessed = true;
                }
            }

            // Procesar otros filtros (excluyendo los de fecha si ya se procesaron)
            foreach (var filter in request.Filtros)
            {
                if (fechaRangeProcessed && filter.Key == "FechaEmision") continue; // Saltar fechas ya procesadas

                if (!string.IsNullOrWhiteSpace(filter.Value))
                {
                    var columnName = filter.Key;
                    string operatorClause = filter.Operator?.ToLower() switch
                    {
                        "like" => "LIKE",
                        "=" => "=",
                        ">=" => ">=",
                        "<=" => "<=",
                        ">" => ">",
                        "<" => "<",
                        "<>" => "<>",
                        _ => "LIKE"
                    };

                    // Generar nombres de parámetros únicos para otros campos
                    if (!parameterCounters.ContainsKey(columnName))
                        parameterCounters[columnName] = 0;
                    else
                        parameterCounters[columnName]++;

                    var uniqueParameterName = $"@{columnName.Replace(".", "_")}_{parameterCounters[columnName]}";
                    whereClauses.Add($"{columnName} {operatorClause} {uniqueParameterName}");

                    object paramValue = operatorClause == "LIKE"
                        ? $"%{filter.Value}%"
                        : filter.Value;

                    parameters.Add(new SqlParameter(uniqueParameterName, paramValue));
                }
            }


            // Procesar sumas (sin cambios)
            foreach (var suma in request.Sumas)
            {
                if (!string.IsNullOrWhiteSpace(suma.Key))
                {
                    sumaClauses.Add(suma.Key);
                }
            }

            // Agrupar condiciones (sin cambios)
            var groupedConditions = whereClauses
                .Select(c => new
                {
                    Key = c.Split(' ')[0],
                    Condition = c
                })
                .GroupBy(x => x.Key)
                .Select(g => g.Count() > 1
                    ? $"({string.Join(" OR ", g.Select(x => x.Condition))})"
                    : g.First().Condition)
                .ToList();

            var whereQuery = groupedConditions.Any()
                ? $"WHERE {string.Join(" AND ", groupedConditions)}"
                : "";

            var sumaQuery = sumaClauses.Any() ? $"{string.Join(", ", sumaClauses)}" : "";

            // Resto del código sin cambios (countQuery, paginatedQuery)
            var countQuery = sum ? $@"
                SELECT COUNT(DISTINCT [Nombre]) AS TotalRegistros {baseQuery} {whereQuery}
            " : $@"
                SELECT COUNT(*) AS TotalRegistros {baseQuery} {whereQuery}";

            var paginatedQuery = sum ? $@" 
                    SELECT
                        {(string.IsNullOrEmpty(sumaQuery) ? "" : $" ROW_NUMBER() OVER(ORDER BY {sumaQuery} DESC) AS ID,")}
                        {(string.IsNullOrEmpty(sumaQuery) ? "" : $"{sumaQuery} ,")}
                        SUM(Cantidad) as Cantidad,
                        SUM([ImporteTotal]) as Importe
                    {baseQuery} 
                    {whereQuery}
                        {(string.IsNullOrEmpty(sumaQuery) ? "" : $"GROUP BY {sumaQuery}")}
                    ORDER BY Importe DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                " : $@"
                SELECT
                    Cliente,
                    Tipo,
                    Movimiento,
                    Articulo,
                    Nombre,
                    Categoria,
                    Grupo,
                    Linea,
                    Familia,
                    CostoUnitario,
                    CostoTotal,
                    ImporteUnitario,
                    ImporteTotal,
                    Cantidad,
                    Almacen,
                    FechaEmision,
                    Mes,
                    Año
                {baseQuery} {whereQuery}
                ORDER BY ID
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            Console.Write(paginatedQuery);
            try
            {
                await using var connection = await OpenConnectionAsync();

                // Total records (sin cambios)
                var countCommandParameters = parameters
                    .Select(p => new SqlParameter(p.ParameterName, p.Value))
                    .ToList();

                await using var countCommand = new SqlCommand(countQuery, connection);
                countCommand.Parameters.AddRange(countCommandParameters.ToArray());
                var totalRecords = (int)await countCommand.ExecuteScalarAsync();

                // Paginated data (sin cambios)
                var paginatedParameters = parameters
                    .Select(p => new SqlParameter(p.ParameterName, p.Value))
                    .ToList();

                paginatedParameters.AddRange(new[]
                {
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@PageSize", pageSize)
                });

                await using var command = new SqlCommand(paginatedQuery, connection);
                command.Parameters.AddRange(paginatedParameters.ToArray());

                var results = new List<Dictionary<string, object>>();

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    results.Add(row);
                }

                return Ok(new
                {
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                    Page = page,
                    PageSize = pageSize,
                    Data = results
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, paginatedQuery);
            }
        }
    }
}