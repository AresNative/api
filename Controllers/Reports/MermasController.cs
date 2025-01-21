using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        [HttpPost("api/v1/reporteria/mermas")]
        public async Task<IActionResult> ObtenerMermas(
            [FromBody] List<BusquedaParams> filtros, // Recibir filtros como una lista
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validación de entrada
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            int offset = (page - 1) * pageSize;

            // Query base
            var baseQuery = @"
                FROM
                    [TC032841E].dbo.INVD inv
                LEFT JOIN
                    [TC032841E].dbo.Art art ON art.ARTICULO = inv.Articulo
                LEFT JOIN
                    [TC032841E].dbo.Inv inv_det ON inv_det.ID = inv.ID";

            // Construcción de cláusulas WHERE dinámicas
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            foreach (var filter in filtros)
            {
                if (!string.IsNullOrWhiteSpace(filter.Value) && !string.IsNullOrWhiteSpace(filter.Key))
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
                    var parameterValue = operatorClause == "LIKE" ? $"%{filter.Value}%" : filter.Value;
                    parameters.Add(new SqlParameter(parameterName, parameterValue));
                }
            }

            var whereQuery = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

            // Query para conteo total
            var countQuery = $"SELECT COUNT(1) {baseQuery} {whereQuery}";

            // Query con paginación
            var paginatedQuery = $@"
                    USE [TC032841E];
                    SELECT 
                        ROW_NUMBER() OVER (ORDER BY inv.Articulo) AS ID,
                        inv.Articulo,
                        art.Descripcion1,
                        art.Categoria,
                        art.Grupo,
                        art.Linea,
                        art.Familia,
                        inv.Unidad,
                        SUM(inv.Cantidad) AS TotalCantidad,
                        SUM(inv.Costo * inv.Cantidad) AS TotalImporte
                        {baseQuery} {whereQuery}
                    GROUP BY
                        inv.Articulo, 
                        art.Descripcion1,
                        art.Categoria,
                        art.Grupo,
                        art.Linea,
                        art.Familia,
                        inv.Unidad
                    ORDER BY inv.Articulo
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            try
            {
                await using var connection = await OpenConnectionAsync();

                // Crear los parámetros de manera independiente para countCommand
                var countCommandParameters = parameters
                    .Select(p => new SqlParameter(p.ParameterName, p.Value)) // Nueva instancia para count
                    .ToList();

                await using var countCommand = new SqlCommand(countQuery, connection);
                countCommand.Parameters.AddRange(countCommandParameters.ToArray());
                var totalRecords = (int)await countCommand.ExecuteScalarAsync();

                // Crear los parámetros de manera independiente para paginatedQuery
                var paginatedCommandParameters = parameters
                    .Select(p => new SqlParameter(p.ParameterName, p.Value)) // Nueva instancia para paginación
                    .ToList();

                paginatedCommandParameters.AddRange(new[]
                {
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@PageSize", pageSize)
                });

                await using var command = new SqlCommand(paginatedQuery, connection);
                command.Parameters.AddRange(paginatedCommandParameters.ToArray());
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
                    TotalRecords = totalRecords,
                    Page = page,
                    PageSize = pageSize,
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
