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
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            int offset = (page - 1) * pageSize;

            var baseQuery = @"
            FROM [LOCAL_TC032391E].[dbo].[Temp_VentasReport]";

            var whereClauses = new List<string>();
            var sumaClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            // Procesar filtros
            var fechaEmisionParams = request.Filtros.Where(f => f.Key == "FechaEmision").ToList();
            if (fechaEmisionParams.Count == 2)
            {
                var minFecha = fechaEmisionParams.First(f => f.Operator == ">=");
                var maxFecha = fechaEmisionParams.First(f => f.Operator == "<=");
                whereClauses.Add("FechaEmision BETWEEN @FechaEmisionMin AND @FechaEmisionMax");
                parameters.Add(new SqlParameter("@FechaEmisionMin", DateTime.Parse(minFecha.Value)));
                parameters.Add(new SqlParameter("@FechaEmisionMax", DateTime.Parse(maxFecha.Value)));
            }
            else
            {
                foreach (var filter in request.Filtros)
                {
                    if (!string.IsNullOrWhiteSpace(filter.Value))
                    {
                        var columnName = filter.Key;
                        var parameterName = $"@{filter.Key.Replace(".", "_")}";

                        if (columnName == "FechaEmision")
                        {
                            whereClauses.Add($"{columnName} {filter.Operator} {parameterName}");
                            parameters.Add(new SqlParameter(parameterName, DateTime.Parse(filter.Value)));
                        }
                        else
                        {
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

                            whereClauses.Add($"{columnName} {operatorClause} {parameterName}");
                            parameters.Add(new SqlParameter(parameterName, operatorClause == "LIKE" ? $"%{filter.Value}%" : filter.Value));
                        }
                    }
                }
            }

            // Procesar sumas
            foreach (var suma in request.Sumas)
            {
                if (!string.IsNullOrWhiteSpace(suma.Key))
                {
                    sumaClauses.Add(suma.Key);
                }
            }

            var whereQuery = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
            var sumaQuery = sumaClauses.Any() ? $"{string.Join(", ", sumaClauses)}" : "";

            var countQuery = sum ? $@"
                SELECT COUNT(DISTINCT [Nombre]) AS TotalRegistros {baseQuery} {whereQuery}
            " : $@"
                SELECT COUNT(*) AS TotalRegistros {baseQuery} {whereQuery}";

            var paginatedQuery = sum ? $@" 
                    SELECT
                        {(string.IsNullOrEmpty(sumaQuery) ? "" : $" ROW_NUMBER() OVER(ORDER BY {sumaQuery} DESC) AS ID,")}
                        {(string.IsNullOrEmpty(sumaQuery) ? "" : $"{sumaQuery} ,")}
                        SUM(Cantidad) as Cantidad,
                        SUM(Importe) as Importe
                    {baseQuery} 
                    {whereQuery}
                        {(string.IsNullOrEmpty(sumaQuery) ? "" : $"GROUP BY {sumaQuery}")}
                    ORDER BY Importe DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                " : $@"
                SELECT
                     ID
                    ,Codigo
                    ,Articulo
                    ,Nombre
                    ,Precio
                    ,Costo
                    ,Cantidad
                    ,Importe
                    ,Impuestos
                    ,CostoTotal
                    ,PrecioTotal
                    ,Unidad
                    ,Sucursal
                    ,FechaEmision
                    ,IVA
                    ,IEPS
                    ,ISR
                    ,[IVA%]
                    ,[IEPS%]
                    ,[ISR%]
                {baseQuery} {whereQuery}
                ORDER BY ID
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            try
            {
                await using var connection = await OpenConnectionAsync();

                // Total records
                var countCommandParameters = parameters
                    .Select(p => new SqlParameter(p.ParameterName, p.Value))
                    .ToList();

                await using var countCommand = new SqlCommand(countQuery, connection);
                countCommand.Parameters.AddRange(countCommandParameters.ToArray());
                var totalRecords = (int)await countCommand.ExecuteScalarAsync();

                // Paginated data
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
                    Page = page,
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
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
