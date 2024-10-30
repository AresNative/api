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
            // Consulta SQL para verificar si ya existe un usuario con el mismo email
            string checkUserQuery = @"SELECT COUNT(1) FROM Website_users WHERE email = @Email";
            // Consulta SQL para registrar un nuevo usuario
            string insertUserQuery = @"INSERT INTO Website_users (name, email, password, date) 
                                       VALUES (@name, @Email, @password, @date)";

            try
            {
                await using var connection = await OpenConnectionAsync();

                // Verificar si ya existe un usuario con el mismo email
                await using (var checkCommand = new SqlCommand(checkUserQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Email", nuevoUsuario.email);
                    var userExists = (int)await checkCommand.ExecuteScalarAsync();

                    if (userExists > 0)
                    {
                        // Retornar respuesta de conflicto si el usuario ya existe
                        return Conflict(new { Message = "El usuario con este email ya está registrado" });
                    }
                }

                // Si el usuario no existe, procedemos con la inserción
                await using (var command = new SqlCommand(insertUserQuery, connection))
                {
                    command.Parameters.AddWithValue("@name", nuevoUsuario.name);
                    command.Parameters.AddWithValue("@Email", nuevoUsuario.email);
                    command.Parameters.AddWithValue("@password", nuevoUsuario.password);
                    command.Parameters.AddWithValue("@date", nuevoUsuario.date);

                    var result = await command.ExecuteNonQueryAsync();

                    if (result > 0)
                        return Ok(new { Message = "Usuario registrado exitosamente" });
                    else
                        return BadRequest(new { Message = "No se pudo registrar el usuario" });
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
    }
}
