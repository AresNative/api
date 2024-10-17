using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        [HttpGet("api/v1/reporteria/almacen")]
        public async Task<IActionResult> ObtenerAlmacen([FromQuery] string? art)
        {
            // Ajustamos la consulta SQL según si se recibe el parámetro "art" o no
            string query;

            if (string.IsNullOrEmpty(art))
            {
                // Consulta sin filtro si no se recibe el parámetro "art"
                query = @"SELECT TOP (1000) *
                          FROM [LOCAL_TC032391E].[dbo].[V0_Articles]";
            }
            else
            {
                // Consulta con filtro por "art"
                query = @"SELECT TOP (1000) *
                          FROM [LOCAL_TC032391E].[dbo].[V0_Articles]
                          WHERE art = @art";
            }

            try
            {
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(query, connection);

                // Si "art" no es nulo o vacío, agregamos el parámetro
                if (!string.IsNullOrEmpty(art))
                {
                    command.Parameters.AddWithValue("@art", art);
                }

                await using var reader = await command.ExecuteReaderAsync();

                var almacen = new List<AlmacenDto>(); // Lista donde se almacenarán los resultados

                while (await reader.ReadAsync())
                {
                    almacen.Add(MapToAlmacenDto(reader)); // Usamos el método correcto para mapear los datos
                }

                return Ok(almacen); // Retornamos los resultados en formato JSON
            }
            catch (Exception ex)
            {
                return HandleException(ex); // Método para gestionar las excepciones
            }
        }

        private AlmacenDto MapToAlmacenDto(SqlDataReader reader)
        {
            return new AlmacenDto
            {
                Id = reader.IsDBNull(reader.GetOrdinal("id")) ? 0 : reader.GetInt32(reader.GetOrdinal("id")),
                IdTypeTaxes = reader.IsDBNull(reader.GetOrdinal("id_type_taxes")) ? null : reader.GetString(reader.GetOrdinal("id_type_taxes")),
                IdMov = reader.IsDBNull(reader.GetOrdinal("id_mov")) ? null : reader.GetString(reader.GetOrdinal("id_mov")),
                IdState = reader.IsDBNull(reader.GetOrdinal("id_state")) ? null : reader.GetString(reader.GetOrdinal("id_state")),
                IdCaja = reader.IsDBNull(reader.GetOrdinal("id_caja")) ? null : reader.GetString(reader.GetOrdinal("id_caja")),
                IdTypePago = reader.IsDBNull(reader.GetOrdinal("id_type_pago")) ? null : reader.GetString(reader.GetOrdinal("id_type_pago")),
                IdSucursal = reader.IsDBNull(reader.GetOrdinal("id_sucursal")) ? null : reader.GetString(reader.GetOrdinal("id_sucursal")),
                IdAlmacen = reader.IsDBNull(reader.GetOrdinal("id_almacen")) ? null : reader.GetString(reader.GetOrdinal("id_almacen")),
                Art = reader.IsDBNull(reader.GetOrdinal("art")) ? null : reader.GetString(reader.GetOrdinal("art")),
                Cant = reader.IsDBNull(reader.GetOrdinal("cant")) ? 0 : reader.GetInt32(reader.GetOrdinal("cant")),
                Price = reader.IsDBNull(reader.GetOrdinal("price")) ? 0 : reader.GetDecimal(reader.GetOrdinal("price")),
                Taxes = reader.IsDBNull(reader.GetOrdinal("taxes")) ? null : reader.GetString(reader.GetOrdinal("taxes")),
                Unit = reader.IsDBNull(reader.GetOrdinal("unit")) ? null : reader.GetString(reader.GetOrdinal("unit")),
                StateArt = reader.IsDBNull(reader.GetOrdinal("state_art")) ? null : reader.GetString(reader.GetOrdinal("state_art")),
                User = reader.IsDBNull(reader.GetOrdinal("user")) ? null : reader.GetString(reader.GetOrdinal("user")),
                Client = reader.IsDBNull(reader.GetOrdinal("client")) ? null : reader.GetString(reader.GetOrdinal("client")),
                Pago = reader.IsDBNull(reader.GetOrdinal("pago")) ? 0 : reader.GetDecimal(reader.GetOrdinal("pago")),
                Import = reader.IsDBNull(reader.GetOrdinal("import")) ? 0 : reader.GetDecimal(reader.GetOrdinal("import")),
                Currency = reader.IsDBNull(reader.GetOrdinal("currency")) ? null : reader.GetString(reader.GetOrdinal("currency"))
            };
        }
    }
}
