using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class Postulaciones : BaseController
    {
        public readonly string _rutaArchivos = @"C:\inetpub\wwwroot\publish\uploads";
        public class PostulacionesPost
        {
            public List<Postulacion> Postulacion { get; set; } = new();
            public IFormFile File { get; set; }  // Agregar la propiedad para el archivo
        }

        [HttpPost("api/v2/insert/postulaciones")]
        public async Task<IActionResult> InsertarPostulacionesRequest([FromForm] PostulacionesPost request)
        {
            if (request?.Postulacion == null || !request.Postulacion.Any())
            {
                // Log de depuración para verificar la solicitud
                return BadRequest(new { Message = "No se recibieron postulaciones válidas.", Data = request });
            }

            var file = request.File;
            // Validar entrada de archivo
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "No se ha proporcionado ningún archivo." });
            }

            try
            {
                // Crear carpeta de destino si no existe
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generar nombre único para el archivo y asegurar que la extensión sea válida
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Guardar el archivo en el servidor
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Inserción de datos en la base de datos
                await using var connection = await OpenConnectionAsync();
                var insertedIds = new List<int>();

                foreach (var filtro in request.Postulacion)
                {
                    var properties = filtro.GetType().GetProperties()
                        .Where(p => p.GetValue(filtro) != null)
                        .ToList();

                    var columnNames = string.Join(", ", properties.Select(p => $"[{p.Name}]"));
                    var parameterNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

                    var query = $@"
            INSERT INTO [LOCAL_TC032391E].[dbo].[Website_solicitud_empleo] ({columnNames}, [FilePath])
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

                return Ok(new { Message = "Postulaciones insertadas correctamente.", Ids = insertedIds, FilePath = filePath });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error al insertar postulaciones.");
            }
        }
    }
}
