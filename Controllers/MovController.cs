using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyApiProject.Controllers
{
    public class MovimientosController : BaseController
    {
        public MovimientosController(IConfiguration configuration) : base(configuration) { }

        [HttpGet("api/v1/movimientos")]
        public async Task<IActionResult> GetMovimientos([FromQuery] string? movimiento = "", [FromQuery] string? caja = "", [FromQuery] DateTime? fechaFija = null)
        {
            DateTime fechaActual = DateTime.Now;
            DateTime fecha = fechaFija ?? fechaActual;//2024-04-04 00:00:00.000 // ! ajustar fecha fija (hoy) como predeterminada 

            string query = @"
                SELECT
                    pc.FormaPago,
                    COUNT(*) AS TotalRegistros,
                    SUM(CASE WHEN pc.Importe > 0 THEN pc.Importe ELSE 0 END) AS TotalFormaPagoINGRESO,
                    SUM(CASE WHEN pc.Importe < 0 THEN pc.Importe ELSE 0 END) AS TotalFormaPagoEGRESO,
                    SUM(CASE WHEN pc.Importe <> 0 THEN pc.Importe ELSE 0 END) AS TotalFormaPago
                FROM poslcobro pc
                JOIN POSL p ON pc.ID = p.ID AND (@MOVIMIENTO = '' OR p.MOV = @MOVIMIENTO)
                WHERE pc.Importe <> 0
                  AND pc.fecha = @FECHA
                  AND (@CAJA = '' OR pc.CtaDinero = @CAJA)
                GROUP BY pc.FormaPago
                ORDER BY TotalFormaPagoINGRESO DESC, TotalFormaPagoEGRESO DESC;
            ";

            try
            {
                await using var connection = await OpenConnectionAsync();

                await using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MOVIMIENTO", movimiento ?? "");
                    command.Parameters.AddWithValue("@CAJA", caja ?? "");
                    command.Parameters.AddWithValue("@FECHA", fecha);

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        var result = new List<dynamic>();

                        while (await reader.ReadAsync())
                        {
                            result.Add(new
                            {
                                FormaPago = reader["FormaPago"],
                                TotalRegistros = reader["TotalRegistros"],
                                TotalFormaPagoINGRESO = reader["TotalFormaPagoINGRESO"],
                                TotalFormaPagoEGRESO = reader["TotalFormaPagoEGRESO"],
                                TotalFormaPago = reader["TotalFormaPago"]
                            });
                        }

                        return Ok(result);
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error al procesar la solicitud", Details = ex.Message });
            }
        }
    }
}
