using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class Postulaciones : BaseController
    {
        public class PostulacionesPost
        {
            public PostulacionData[] PostulacionForm { get; set; }
            public IFormFile File { get; set; }
        }

        [HttpPost("api/v2/insert/postulaciones")]
        public async Task<IActionResult> InsertarPostulacionesRequest([FromBody] PostulacionesPost request)
        {
            var Data = request.PostulacionForm;


            if (request?.PostulacionForm == null)
                return BadRequest(Data);

            try
            {
                var file = request.File;
                // Crear carpeta de destino si no existe
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                Directory.CreateDirectory(uploadsFolder);

                // Generar nombre único para el archivo
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Guardar el archivo en el servidor
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                await using var connection = await OpenConnectionAsync();

                var insertedIds = new List<int>();

                foreach (var filtro in request.PostulacionForm)
                {
                    var properties = filtro.GetType().GetProperties()
                        .Where(p => p.GetValue(filtro) != null)
                        .ToList();

                    var columnNames = string.Join(", ", properties.Select(p => $"[{p.Name}]"));
                    var parameterNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

                    var query = $@"
                        INSERT INTO [LOCAL_TC032391E].[dbo].[Website_solicitud_empleo] ({columnNames}, file)
                        OUTPUT INSERTED.ID
                        VALUES ({parameterNames}, @FilePath);
                    ";

                    var parameters = properties.Select(p =>
                        new SqlParameter($"@{p.Name}", p.GetValue(filtro) ?? DBNull.Value))
                        .Concat(new[] { new SqlParameter("@FilePath", filePath) })
                        .ToArray();

                    await using var command = new SqlCommand(query, connection);
                    command.Parameters.AddRange(parameters);

                    var insertedId = await command.ExecuteScalarAsync();
                    insertedIds.Add(Convert.ToInt32(insertedId));
                }

                return Ok(new { Message = "Postulaciones insertadas correctamente.", Ids = insertedIds });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error al insertar la postulacion.");
            }
        }
    }
}
