using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class Glosarios : BaseController
    {
        public Glosarios(IConfiguration configuration) : base(configuration) { }

        [HttpGet("api/v1/glosarios/glosario-ventas")]
        public async Task<IActionResult> ObtenerGlosarioVentas()
        {
            try
            {
                // Lista para almacenar el glosario dinámico
                var glosario = new List<Dictionary<string, object>>();

                // Abre la conexión a la base de datos
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(@"
                    SELECT 
                        top (1) *
                    FROM 
                        Temp_VentasReport
                ", connection) // Solo un registro para obtener columnas
                {
                    CommandTimeout = 30
                };

                // Ejecutar el query y obtener los metadatos de las columnas
                await using var reader = await command.ExecuteReaderAsync();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    // Obtener el nombre de la columna
                    string columnName = reader.GetName(i);

                    // El alias puede estar precedido de un prefijo en el query (e.g., cb.Codigo, A.Descripcion1)
                    string columnValue = columnName;

                    // Detectar el alias de la columna, si existe
                    if (columnValue.Contains("."))
                    {
                        var parts = columnValue.Split('.');
                        columnValue = parts[0]; // Obtiene el alias de la tabla
                    }

                    // Construcción dinámica del glosario
                    var columna = new Dictionary<string, object>
                    {
                        { "Nombre", columnName },
                        { "Valor", columnValue }, // El valor ahora tiene el alias
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
