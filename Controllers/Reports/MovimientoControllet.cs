using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        [HttpGet("api/v1/reporteria/movimientos")]
        public async Task<IActionResult> ObtenerMovimientos(
            [FromQuery] string? art,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validación de paginación
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            int offset = (page - 1) * pageSize;

            var baseQuery = @"
                FROM 
                    [V0_Articles]";
            var whereClause = string.Empty;
            var countQueryBuilder = new StringBuilder($"SELECT COUNT(*) {baseQuery}");
            var queryBuilder = new StringBuilder();

            if (string.IsNullOrEmpty(art))
            {
                // Consulta sin filtro si no se recibe el parámetro "art"
                queryBuilder.Append($@"
                    SELECT * 
                    {baseQuery}
                    ORDER BY [Id] -- Reemplaza con la columna adecuada para ordenar los resultados
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY");
            }
            else
            {
                // Consulta con filtro por "art"
                whereClause = @"
                    WHERE art = @art";
                countQueryBuilder.Append(whereClause);

                queryBuilder.Append($@"
                    
                    SELECT 
                        ROW_NUMBER() OVER (ORDER BY VTA.Articulo) AS ID,
                        VTA.Articulo,
                        art.Descripcion1,
                        art.Categoria,
                        art.Grupo,
                        art.Linea,
                        art.Familia,
                        VTA.Unidad,
                        SUM(VTA.Cantidad) AS TotalCantidad,
                        SUM(VTA.Precio * VTA.Cantidad) AS TotalImporte
                    FROM
                        ART art
                    RIGHT JOIN
                        VentaD VTA ON art.ARTICULO = VTA.Articulo
                    WHERE
                        VTA.ID IN (
                            SELECT ID 
                            FROM Venta 
                            WHERE 
                                Mov = 'NOTA'
                                AND Estatus IN( 'CONCLUIDO','PROCESAR')
                                AND FechaEmision > '2024-09-01 00:00:00.000'
                                AND FechaEmision < '2024-09-30 00:00:00.000'
                                AND Sucursal in (1)
                        )
                    GROUP BY 
                        VTA.Articulo,
                        art.Descripcion1,
                        art.Categoria,
                        art.Grupo,
                        art.Linea,
                        art.Familia,
                        VTA.Unidad
                    ORDER BY (SELECT NULL)
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY");
            }

            try
            {
                await using var connection = await OpenConnectionAsync();

                // Ejecutar consulta para el conteo total de registros
                await using var countCommand = new SqlCommand(countQueryBuilder.ToString(), connection);
                if (!string.IsNullOrEmpty(art))
                {
                    countCommand.Parameters.AddWithValue("@art", art);
                }
                int totalRecords = (int)await countCommand.ExecuteScalarAsync();

                // Ejecutar consulta paginada
                await using var command = new SqlCommand(queryBuilder.ToString(), connection);
                command.Parameters.AddWithValue("@offset", offset);
                command.Parameters.AddWithValue("@pageSize", pageSize);

                if (!string.IsNullOrEmpty(art))
                {
                    command.Parameters.AddWithValue("@art", art);
                }

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

                // Devolver el resultado con la información de la paginación
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
                return HandleException(ex);
            }
        }
    }
}
