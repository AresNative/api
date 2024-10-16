using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class ScrumController : BaseController
    {
        [HttpPost("api/v1/tasks/{taskId}/comments")]
        public async Task<IActionResult> AddComment(int taskId, [FromBody] Comments commentRequest)
        {
            string query = @"INSERT INTO comments (tarea_id, usuario_id, contenido, fecha_creacion) 
                     VALUES (@TaskId, @UserId, @NewComment, GETDATE())";

            await using var connection = await OpenConnectionAsync();
            await using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@TaskId", taskId);
            command.Parameters.AddWithValue("@NewComment", commentRequest.NewComment);
            command.Parameters.AddWithValue("@UserId", commentRequest.UserId);

            var result = await command.ExecuteNonQueryAsync();
            return Ok(result);
        }

    }
}
