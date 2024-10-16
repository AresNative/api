using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class UsuariosController : BaseController
    {
        public UsuariosController(IConfiguration configuration) : base(configuration) { }

        [HttpPost("api/v1/users/register")]
        public async Task<IActionResult> RegistrarUsuario([FromBody] Usuario nuevoUsuario)
        {
            // Ajustamos la consulta SQL para incluir el campo password
            string query = @"INSERT INTO Website_users (name, email, password, date) 
                             VALUES (@name, @email, @password, @date)";

            try
            {
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(query, connection);

                // Agregamos los parámetros incluyendo el password
                command.Parameters.AddWithValue("@name", nuevoUsuario.name);
                command.Parameters.AddWithValue("@email", nuevoUsuario.email);
                command.Parameters.AddWithValue("@password", nuevoUsuario.password);  // Usamos el password del modelo
                command.Parameters.AddWithValue("@date", nuevoUsuario.date);

                var result = await command.ExecuteNonQueryAsync();

                if (result > 0)
                    return Ok(new { Message = "Usuario registrado exitosamente" });
                else
                    return BadRequest(new { Message = "No se pudo registrar el usuario" });
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
    }
}
