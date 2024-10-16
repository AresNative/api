using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class ScrumController : BaseController
    {
        [HttpPost("api/v1/projects")]
        public async Task<IActionResult> CreateProject([FromBody] Project newProject)
        {

            string query = @"
        INSERT INTO projects (nombre, descripcion, fecha_inicio, fecha_fin, activo, state) 
        VALUES (@Nombre, @Descripcion, @FechaInicio, @FechaFin, @Activo, @State);
        SELECT SCOPE_IDENTITY();"; // Devuelve el ID del proyecto insertado

            await using var connection = await OpenConnectionAsync();
            await using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Nombre", newProject.Nombre);
            command.Parameters.AddWithValue("@Descripcion", newProject.Descripcion ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@FechaInicio", newProject.FechaInicio);
            command.Parameters.AddWithValue("@FechaFin", newProject.FechaFin);
            command.Parameters.AddWithValue("@Activo", newProject.Activo);
            command.Parameters.AddWithValue("@State", newProject.State);

            var result = await command.ExecuteScalarAsync(); // Obtiene el ID del proyecto insertado
            return Ok(new { ProjectId = result });
        }

    }
}
