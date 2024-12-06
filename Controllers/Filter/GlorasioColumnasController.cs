using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;
using System.Text;

namespace MyApiProject.Controllers
{
    public partial class Filtros : BaseController
    {
        [HttpPost("api/v1/filtros/glosario-dinamico")]
        public async Task<IActionResult> ObtenerGlosarioDinamico([FromBody] string query)
        {
            try
            {
                // Validar que el query no sea nulo o vacío
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("El query proporcionado no puede estar vacío.");
                }

                // Lista para almacenar el glosario dinámico
                var glosario = new List<Dictionary<string, string>>();

                // Abre la conexión a la base de datos
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(query + " OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY", connection) // Solo un registro para obtener columnas
                {
                    CommandTimeout = 30
                };

                // Ejecutar el query y obtener los metadatos de las columnas
                await using var reader = await command.ExecuteReaderAsync();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columna = new Dictionary<string, string>
            {
                { "Nombre", reader.GetName(i) },
                { "TipoDato", reader.GetDataTypeName(i) },
                { "Tamaño", reader.GetFieldType(i).ToString() }
            };

                    glosario.Add(columna);
                }

                // Devuelve el glosario generado
                return Ok(glosario);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error al generar el glosario dinámico.");
            }
        }

    }
}
