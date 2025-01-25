using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        [HttpPost("api/v1/reporteria/ventas")]
        public async Task<IActionResult> ObtenerVentas(
                            [FromBody] List<BusquedaParams> filtros,
                            [FromQuery] int page = 1,
                            [FromQuery] int pageSize = 10)
        {
            if (filtros == null || !filtros.Any())
                return BadRequest("Debe proporcionar al menos un filtro.");

            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            int offset = (page - 1) * pageSize;

            var baseQuery = @"
                FROM 
                    [Temp_VentasReport]";

            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            foreach (var filter in filtros)
            {
                if (!string.IsNullOrWhiteSpace(filter.Value))
                {
                    var columnName = filter.Key;
                    var parameterName = $"@{filter.Key.Replace(".", "_")}";

                    string operatorClause = filter.Operator?.ToLower() switch
                    {
                        "like" => "LIKE",
                        "=" => "=",
                        ">=" => ">=",
                        "<=" => "<=",
                        ">" => ">",
                        "<" => "<",
                        _ => "LIKE"
                    };

                    whereClauses.Add($"{columnName} {operatorClause} {parameterName}");
                    parameters.Add(new SqlParameter(parameterName, operatorClause == "LIKE" ? $"%{filter.Value}%" : filter.Value));
                }
            }

            var whereQuery = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

            var countQuery = $"SELECT COUNT(1) {baseQuery} {whereQuery}";

            var paginatedQuery = $@"
                SELECT 
                    [ID]
                    ,[Codigo]
                    ,[Articulo]
                    ,[Nombre]
                    ,[Precio]
                    ,[Costo]
                    ,[Cantidad]
                    ,[Importe]
                    ,[Impuestos]
                    ,[CostoTotal]
                    ,[PrecioTotal]
                    ,[Unidad]
                    ,[Sucursal]
                    ,[FechaEmision]
                    ,[IVA]
                    ,[IEPS]
                    ,[ISR]
                    ,[IVA%]
                    ,[IEPS%]
                    ,[ISR%]
                {baseQuery} {whereQuery}
                ORDER BY ID ASC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            try
            {
                await using var connection = await OpenConnectionAsync();

                // Crear nuevos parámetros para la consulta de conteo
                var countCommandParameters = parameters
                    .Select(p => new SqlParameter(p.ParameterName, p.Value))
                    .ToList();

                await using var countCommand = new SqlCommand(countQuery, connection);
                countCommand.Parameters.AddRange(countCommandParameters.ToArray());
                var totalRecords = (int)await countCommand.ExecuteScalarAsync();

                // Crear nuevos parámetros para la consulta paginada
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

                var response = new
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                    Data = results
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, paginatedQuery);
            }
        }
    }
}
