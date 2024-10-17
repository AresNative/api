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
