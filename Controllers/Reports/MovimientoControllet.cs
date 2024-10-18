using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        [HttpGet("api/v1/reporteria/movimientos")]
        public async Task<IActionResult> ObtenerMovimientos([FromQuery] string? art, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            // Validación de paginación
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10; // Limitar el tamaño de la página a un máximo de 100

            int offset = (page - 1) * pageSize;

            string query;
            string countQuery;

            if (string.IsNullOrEmpty(art))
            {
                // Consulta sin filtro si no se recibe el parámetro "art"
                query = $@"
                    SELECT * 
                    FROM [LOCAL_TC032391E].[dbo].[V0_Articles]
                    ORDER BY [Id] -- Reemplaza con la columna adecuada para ordenar los resultados
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                // Consulta para obtener el total de registros sin filtro
                countQuery = "SELECT COUNT(*) FROM [LOCAL_TC032391E].[dbo].[V0_Articles]";
            }
            else
            {
                // Consulta con filtro por "art"
                query = $@"
                    USE TC032841E
                            SELECT 
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
                    ORDER BY [Id] -- Reemplaza con la columna adecuada para ordenar los resultados
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                // Consulta para obtener el total de registros con filtro
                countQuery = "SELECT COUNT(*) FROM [LOCAL_TC032391E].[dbo].[V0_Articles] WHERE art = @art";
            }

            try
            {
                await using var connection = await OpenConnectionAsync();

                // Ejecutamos la consulta de conteo total de registros
                await using var countCommand = new SqlCommand(countQuery, connection);
                if (!string.IsNullOrEmpty(art))
                {
                    countCommand.Parameters.AddWithValue("@art", art);
                }
                int totalRecords = (int)await countCommand.ExecuteScalarAsync();

                // Ejecutamos la consulta paginada
                await using var command = new SqlCommand(query, connection);
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

                // Devolvemos el resultado con la información de la paginación
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
                return HandleException(ex); // Método para gestionar las excepciones
            }
        }
    }
}
