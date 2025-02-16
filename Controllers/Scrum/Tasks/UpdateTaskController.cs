using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class ScrumController : BaseController
    {
        [HttpPost("api/v1/tasks/{taskId}/update-status")]
        public async Task<IActionResult> UpdateTaskStatus(int taskId, [FromBody] Sprint nuevoSprint)
        {

            string query = @"UPDATE tasks SET estado = @Estado WHERE id = @TaskId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Estado", nuevoSprint.estado);
            command.Parameters.AddWithValue("@TaskId", taskId);

            var result = await command.ExecuteNonQueryAsync();
            return Ok(result);
        }

    }
}
