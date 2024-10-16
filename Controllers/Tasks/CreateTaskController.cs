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

            string query = @"INSERT INTO tasks (nombre, estado, fecha_creacion) 
                     VALUES(@Nombre, @Estado, GETDATE())";

            await using var connection = await OpenConnectionAsync();
            await using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Nombre", nuevoSprint.nombre);
            command.Parameters.AddWithValue("@Estado", nuevoSprint.estado);

            var result = await command.ExecuteNonQueryAsync();
            return Ok(result);
        }

    }
}
