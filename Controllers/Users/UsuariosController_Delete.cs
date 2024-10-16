using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;


namespace MyApiProject.Controllers
{
    public partial class UsuariosController : BaseController
    {
        [Authorize]
        [HttpDelete("api/v1/users/delete/{id}")]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            string query = @"DELETE FROM Website_users WHERE Id = @Id";

            try
            {
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@Id", id);

                var result = await command.ExecuteNonQueryAsync();

                if (result > 0)
                    return Ok(new { Message = "Usuario eliminado exitosamente" });
                else
                    return NotFound(new { Message = "Usuario no encontrado" });
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
    }
}
