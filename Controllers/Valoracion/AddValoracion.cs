using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MyApiProject.Controllers
{
    public partial class ValoracionesController : BaseController
    {
        public ValoracionesController(IConfiguration configuration) : base(configuration) { }
        [HttpPost("api/v1/valoracion")]
        public async Task<IActionResult> InsertarValoracion(
            [FromBody] ValoracionRequest request)
        {
            // Validación de la entrada
            if (request == null || string.IsNullOrEmpty(request.Comment) || request.Valor == null)
            {
                return BadRequest(new { Message = "Faltan parámetros obligatorios." });
            }

            // Construcción del query para INSERT
            var insertQuery = @"INSERT INTO [LOCAL_TC032391E].[dbo].[Website_Valoracion] 
                                ([comment], [valor]) 
                                VALUES (@Comment, @Valor);
                                SELECT SCOPE_IDENTITY();"; // Devuelve el ID generado

            try
            {
                // Abre la conexión de forma segura
                await using var connection = await OpenConnectionAsync();

                // Ejecutar el comando
                await using var command = new SqlCommand(insertQuery, connection)
                {
                    CommandTimeout = 30
                };

                // Asignar parámetros
                command.Parameters.Add(new SqlParameter("@Comment", SqlDbType.NVarChar) { Value = request.Comment });
                command.Parameters.Add(new SqlParameter("@Valor", SqlDbType.Int) { Value = request.Valor });

                // Ejecutar y obtener el ID generado
                var newId = await command.ExecuteScalarAsync();

                return Ok(new { Message = "Registro insertado correctamente.", Id = newId });
            }
            catch (Exception ex)
            {
                return HandleException(ex, insertQuery);
            }
        }
    }

    // Clase para mapear la solicitud
    public class ValoracionRequest
    {
        public string Comment { get; set; }
        public int? Valor { get; set; }
    }
}
