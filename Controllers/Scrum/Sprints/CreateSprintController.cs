using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class ScrumController : BaseController
    {
        [HttpPost("api/v1/projects/{projectId}/sprints")]
        public async Task<IActionResult> CreateSprint(int projectId, [FromBody] Sprint newSprint)
        {

            string query = @"
                INSERT INTO sprints (proyecto_id, nombre, fecha_inicio, fecha_fin, estado) 
                VALUES (@ProjectId, @Nombre, @FechaInicio, @FechaFin, @Estado);";

            await using var connection = await OpenConnectionAsync();
            await using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@ProjectId", projectId);
            command.Parameters.AddWithValue("@Nombre", newSprint.nombre);
            command.Parameters.AddWithValue("@Estado", newSprint.estado);
            command.Parameters.AddWithValue("@FechaInicio", newSprint.fecha_inicio);
            command.Parameters.AddWithValue("@FechaFin", newSprint.fecha_fin);

            var result = await command.ExecuteNonQueryAsync();
            return Ok(result);
        }

    }
}
