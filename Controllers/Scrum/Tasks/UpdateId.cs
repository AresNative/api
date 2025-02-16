using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class ScrumController : BaseController
    {
        [HttpPost("api/v1/tasks/{taskId}/update-order")]
        public async Task<IActionResult> UpdateTaskOrder(int taskId, int order)
        {

            string query = @"UPDATE tasks SET [order] = @Order WHERE id = @TaskId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@TaskId", taskId);
            command.Parameters.AddWithValue("@Order", order);

            var result = await command.ExecuteNonQueryAsync();
            return Ok(result);
        }

    }
}
