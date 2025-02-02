using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace MyApiProject.Controllers
{
    public partial class Combos : BaseController
    {
        [HttpPost("api/v2/insert/combos")]

        public async Task<IActionResult> InsertarCombosRequest([FromForm] UploadCombos request)
        {
            if (request?.CombosData == null || request.File == null)
                return BadRequest("Datos de postulación o archivo no proporcionados.");

            try
            {
                var file = request.File;
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                await using var connection = await OpenConnectionAsync();

                // Deserializar la cadena JSON a un objeto CombosData
                var combosData = JsonConvert.DeserializeObject<CombosData>(request.CombosData);

                var properties = combosData.GetType().GetProperties()
                    .Where(p => p.GetValue(combosData) != null)
                    .ToList();

                var columnNames = string.Join(", ", properties.Select(p => $"[{p.Name}]"));
                var parameterNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

                var query = $@"
                    INSERT INTO [LOCAL_TC032391E].[dbo].[Website_Combos] ({columnNames}, [file])
                    OUTPUT INSERTED.ID
                    VALUES ({parameterNames}, @FilePath);
                ";

                var parameters = properties.Select(p =>
                    new SqlParameter($"@{p.Name}", p.GetValue(combosData) ?? DBNull.Value))
                    .Concat(new[] { new SqlParameter("@FilePath", filePath) })
                    .ToArray();

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddRange(parameters);

                var insertedId = await command.ExecuteScalarAsync();

                return Ok(new { Message = "Combo(s) insertado(s) correctamente.", Id = insertedId });
            }
            catch (Exception ex)
            {
                return HandleException(ex, $"Error al insertar el combo(s): {ex.Message}");
            }
        }
    }
}