using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {

        [HttpGet("api/v1/reporteria/ventas")]
        public async Task<IActionResult> ObtenerVentas()
        {
            // Ajustamos la consulta SQL para incluir el campo password
            string query = @"USE TC032841E
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
                return HandleException(ex); // Método para gestionar las excepciones
            }
        }


    }
}
