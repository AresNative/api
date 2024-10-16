using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        [HttpGet("api/v1/reporteria/mermas")]
        public async Task<IActionResult> ObtenerMermas()
        {
            string query = @"USE TC032841E
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
                            FROM
                                ART art
                            RIGHT JOIN
                                INVD inv ON art.ARTICULO = inv.Articulo
                            WHERE
                                inv.ID IN (
                                    SELECT ID 
                                    FROM Inv 
                                    WHERE 
                                        Concepto IS NOT NULL
                                        AND Mov = 'SALIDA DIVERSA'
                                        AND Concepto = 'SALIDA POR MERMAS'
                                        AND Estatus = 'CONCLUIDO'
                                        AND FechaEmision > '2024-09-01 00:00:00.000'
                                        AND FechaEmision < '2024-09-30 00:00:00.000'
                                        AND Sucursal in ('1','2','3','4','0')
                                )
                            GROUP BY 
                                inv.Articulo,
                                art.Descripcion1,
                                art.Categoria,
                                art.Grupo,
                                art.Linea,
                                art.Familia,
                                inv.Unidad
                            ORDER BY
                            TotalCantidad desc";

            try
            {
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(query, connection);
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

                return Ok(results);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
    }
}
