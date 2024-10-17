using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class ScrumController : BaseController
    {
        [HttpPut("api/v1/tasks/{taskId}/assign-user")]
        public async Task<IActionResult> AssignTaskToUser(int taskId, [FromBody] int userId)
        {

            string query = @"UPDATE tasks SET asignado_a = @UserId WHERE id = @TaskId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@TaskId", taskId);
            command.Parameters.AddWithValue("@UserId", userId);

            var result = await command.ExecuteNonQueryAsync();
            return Ok(result);
        }

    }
}
