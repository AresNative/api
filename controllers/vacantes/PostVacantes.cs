using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class Vacantes : BaseController
    {
        public class VacantesPost
        {
            public List<VacantesParams> Articulo { get; set; } = new();
        }

        [HttpPost("api/v2/insert/vacantes")]
        public async Task<IActionResult> InsertarVacantesRequest([FromBody] VacantesPost request)
        {
            if (request?.Articulo == null || !request.Articulo.Any())
                return BadRequest("No hay datos válidos para insertar.");

            try
            {
                await using var connection = await OpenConnectionAsync();

                var insertedIds = new List<int>();

                foreach (var filtro in request.Articulo)
                {
                    var properties = filtro.GetType().GetProperties()
                        .Where(p => p.GetValue(filtro) != null)
                        .ToList();

                    var columnNames = string.Join(", ", properties.Select(p => $"[{p.Name}]"));
                    var parameterNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

                    var query = $@"
                        INSERT INTO [LOCAL_TC032391E].[dbo].[Website_article] ({columnNames})
                        OUTPUT INSERTED.ID
                        VALUES ({parameterNames});
                    ";

                    var parameters = properties.Select(p =>
                        new SqlParameter($"@{p.Name}", p.GetValue(filtro) ?? DBNull.Value)).ToArray();

                    await using var command = new SqlCommand(query, connection);
                    command.Parameters.AddRange(parameters);

                    var insertedId = await command.ExecuteScalarAsync();
                    insertedIds.Add(Convert.ToInt32(insertedId));
                }

                return Ok(new { Message = "Vacantes insertadas correctamente.", Ids = insertedIds });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error al insertar promoción.");
            }
        }
    }
}
