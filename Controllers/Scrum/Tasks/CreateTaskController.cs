using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class ScrumController : BaseController
    {
        [HttpPost("api/v1/sprints/tasks")]
        public async Task<IActionResult> CreateTask([FromBody] TaskModel nuevoSprint)
        {
            string query = @"INSERT INTO tasks (sprint_id, nombre, estado, descripcion, prioridad, fecha_creacion) 
                     VALUES(@sprint_id, @nombre, @estado, @descripcion, @prioridad, GETDATE())";
            try
            {

                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@sprint_id", nuevoSprint.sprint_id);
                command.Parameters.AddWithValue("@nombre", nuevoSprint.nombre);
                command.Parameters.AddWithValue("@estado", nuevoSprint.estado);
                command.Parameters.AddWithValue("@descripcion", nuevoSprint.descripcion);
                command.Parameters.AddWithValue("@prioridad", nuevoSprint.prioridad);

                var result = await command.ExecuteNonQueryAsync();
                return Ok(result);

            }
            catch (Exception ex)
            {
                return HandleException(ex, query.ToString());
            }
        }

    }
}
