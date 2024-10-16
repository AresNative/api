using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class ScrumController : BaseController
    {
        [HttpGet("api/v1/tasks/{taskId}/history")]
        public async Task<IActionResult> GetTaskHistory(int taskId)
        {

            string query = @"SELECT * FROM task_history WHERE id = @TaskId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@TaskId", taskId);

            await using var reader = await command.ExecuteReaderAsync();
            var results = new List<Dictionary<string, object>>();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                results.Add(row);
            }

            return Ok(results);
        }

    }
}
