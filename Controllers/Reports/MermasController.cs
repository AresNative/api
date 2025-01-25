using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        [HttpPost("api/v1/reporteria/mermas")]
        public async Task<IActionResult> ObtenerMermas(
            [FromBody] List<BusquedaParams> filtros,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            int offset = (page - 1) * pageSize;

            var baseQuery = @"
                FROM [TC032841E].dbo.INVD inv
                LEFT JOIN [TC032841E].dbo.Art art ON art.Articulo = inv.Articulo
                WHERE inv.ID IN (
                    SELECT ID 
                    FROM [TC032841E].dbo.Inv 
                    WHERE 
                        Concepto = 'SALIDA POR MERMAS'
                        AND Estatus = 'CONCLUIDO')";

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
                    parameters.Add(new SqlParameter(parameterName,
                        operatorClause == "LIKE" ? $"%{filter.Value}%" : filter.Value));
                }
            }

            var whereQuery = whereClauses.Any() ? $" AND {string.Join(" AND ", whereClauses)}" : "";

            var countQuery = $@"
                USE [TC032841E];

                SELECT COUNT(*) AS TotalRegistros
                FROM (
                    SELECT 
                        art.Articulo,
                        art.Descripcion1,
                        art.Categoria,
                        art.Grupo,
                        art.Linea,
                        art.Familia,
                        art.Estatus,
                        art.Concepto,
                        inv.Unidad,
                        inv.Sucursal
                    FROM
                        dbo.INVD inv
                    LEFT JOIN
                        dbo.Art art ON art.Articulo = inv.Articulo
                    LEFT JOIN
                        dbo.INVD inv_det ON inv_det.ID = inv.ID
                    WHERE
                        inv.ID IN (
                            SELECT ID 
                            FROM dbo.Inv 
                            WHERE 
                                Concepto IS NOT NULL
                                AND Concepto = 'SALIDA POR MERMAS'
                                AND Estatus = 'CONCLUIDO'
                        )
                        {whereQuery} -- Aquí se agregan los filtros dinámicos
                    GROUP BY
                        art.Articulo,
                        art.Descripcion1,
                        art.Categoria,
                        art.Grupo,
                        art.Linea,
                        art.Familia,
                        inv.Unidad,
                        inv.Sucursal,
                        art.Concepto,
                        art.Estatus
                ) AS Contador;";


            var totalQuery = $@"
                SELECT ISNULL(SUM(inv.Costo * inv.Cantidad), 0) AS TotalImporteGeneral
                {baseQuery} {whereQuery}";

            var paginatedQuery = $@"
                SELECT 
                    ROW_NUMBER() OVER (ORDER BY art.Articulo DESC) AS ID,
                    art.Articulo,
                    art.Descripcion1,
                    art.Categoria,
                    art.Grupo,
                    art.Linea,
                    art.Familia,
                    art.Estatus,
                    art.Concepto,
                    inv.Unidad,
                    SUM(inv.Cantidad) AS TotalCantidad,
                    SUM(inv.Costo * inv.Cantidad) AS TotalImporte,
                    inv.Sucursal
                {baseQuery} {whereQuery}
                GROUP BY
                    art.Articulo,
                    art.Descripcion1,
                    art.Categoria,
                    art.Grupo,
                    art.Linea,
                    art.Familia,
                    inv.Unidad,
                    inv.Sucursal,
                    art.Concepto,
                    art.Estatus
                ORDER BY
                    art.Articulo DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            try
            {
                await using var connection = await OpenConnectionAsync();

                // Total records
                var countCommand = new SqlCommand(countQuery, connection);
                countCommand.Parameters.AddRange(parameters.ToArray());
                var totalRecords = (int)await countCommand.ExecuteScalarAsync();

                // Total general sum
                var totalCommand = new SqlCommand(totalQuery, connection);
                totalCommand.Parameters.AddRange(parameters.ToArray());
                var totalGeneral = Convert.ToDecimal(await totalCommand.ExecuteScalarAsync());

                // Paginated data
                parameters.AddRange(new[]
                {
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@PageSize", pageSize)
                });

                var command = new SqlCommand(paginatedQuery, connection);
                command.Parameters.AddRange(parameters.ToArray());

                var results = new List<Dictionary<string, object>>();
                var reader = await command.ExecuteReaderAsync();
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
                    TotalGeneral = totalGeneral,
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
