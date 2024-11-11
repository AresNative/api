using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        [HttpGet("api/v1/reporteria/mermas")]
        public async Task<IActionResult> ObtenerMermas(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? descripcion,
            [FromQuery] string? sucursal,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;
            int offset = (page - 1) * pageSize;

            var baseQuery = @"
                FROM
                    [TC032841E].[dbo].Art art
                RIGHT JOIN
                    [TC032841E].[dbo].INVD inv ON art.ARTICULO = inv.Articulo
                WHERE
                    inv.ID IN (
                        SELECT ID 
                        FROM [TC032841E].[dbo].Inv 
                        WHERE 
                            Concepto IS NOT NULL
                            AND Mov = 'SALIDA DIVERSA'
                            AND Concepto = 'SALIDA POR MERMAS'
                            AND Estatus = 'CONCLUIDO'";

            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (startDate.HasValue)
            {
                whereClauses.Add("FechaEmision >= @StartDate");
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }
            if (endDate.HasValue)
            {
                whereClauses.Add("FechaEmision <= @EndDate");
                parameters.Add(new SqlParameter("@EndDate", endDate.Value));
            }
            if (!string.IsNullOrEmpty(sucursal))
            {
                whereClauses.Add("Sucursal = @Sucursal");
                parameters.Add(new SqlParameter("@Sucursal", sucursal));
            }
            if (!string.IsNullOrEmpty(descripcion))
            {
                whereClauses.Add("art.Descripcion1 LIKE @Descripcion");
                parameters.Add(new SqlParameter("@Descripcion", $"%{descripcion}%"));
            }
            var whereQuery = whereClauses.Any() ? $" AND {string.Join(" AND ", whereClauses)}" : "";
            baseQuery += whereQuery + ")";

            var countQueryBuilder = new StringBuilder($"SELECT COUNT(1) {baseQuery}");

            var queryBuilder = new StringBuilder($@"
                USE TC032841E
                SELECT
                    inv.Articulo,
                    art.Descripcion1,
                    art.Categoria,
                    art.Grupo,
                    art.Linea,
                    art.Familia,
                    inv.Unidad,
                    SUM(inv.Cantidad) AS TotalCantidad,
                    SUM(inv.Costo * inv.Cantidad) AS TotalImporte
                {baseQuery}
                GROUP BY 
                    inv.Articulo,
                    art.Descripcion1,
                    art.Categoria,
                    art.Grupo,
                    art.Linea,
                    art.Familia,
                    inv.Unidad
                ORDER BY
                    TotalCantidad DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");


            try
            {
                int totalRecords;
                await using var connection = await OpenConnectionAsync();

                var countParameters = parameters.Select(p => new SqlParameter(p.ParameterName, p.Value)).ToArray();
                await using (var countCommand = new SqlCommand(countQueryBuilder.ToString(), connection))
                {
                    countCommand.Parameters.AddRange(countParameters);
                    totalRecords = (int)await countCommand.ExecuteScalarAsync();
                }

                var paginatedParameters = parameters.Select(p => new SqlParameter(p.ParameterName, p.Value)).ToList();
                paginatedParameters.Add(new SqlParameter("@Offset", offset));
                paginatedParameters.Add(new SqlParameter("@PageSize", pageSize));

                await using var command = new SqlCommand(queryBuilder.ToString(), connection);
                command.Parameters.AddRange(paginatedParameters.ToArray());

                await using var reader = await command.ExecuteReaderAsync();
                var results = new List<Dictionary<string, object>>();

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
                return HandleException(ex, queryBuilder.ToString());
            }
        }
    }
}
