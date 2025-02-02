using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class Evaluacion : BaseController
    {
        public class EvaluacionPost
        {
            public List<EvaluacionParams> Evaluacion { get; set; } = new();
        }

        [HttpPost("api/v2/insert/Evaluacion")]
        public async Task<IActionResult> InsertarEvaluacionRequest([FromBody] EvaluacionPost request)
        {
            if (request?.Evaluacion == null || !request.Evaluacion.Any())
                return BadRequest("No hay datos válidos para insertar.");

            try
            {
                await using var connection = await OpenConnectionAsync();

                var insertedIds = new List<int>();

                foreach (var filtro in request.Evaluacion)
                {
                    var properties = filtro.GetType().GetProperties()
                        .Where(p => p.GetValue(filtro) != null)
                        .ToList();

                    var columnNames = string.Join(", ", properties.Select(p => $"[{p.Name}]"));
                    var parameterNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

                    var query = $@"
                        INSERT INTO [LOCAL_TC032391E].[dbo].[Website_Evaluacion] ({columnNames})
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

                return Ok(new { Message = "Evaluacion insertadas correctamente.", Ids = insertedIds });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error al insertar promoción.");
            }
        }
    }
}
