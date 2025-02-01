using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MyApiProject.Controllers
{
    public partial class Media : BaseController
    {
        public Media(IConfiguration configuration) : base(configuration) { }
        [HttpPost("api/v2/imagenes/upload")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> SubirImagen([FromForm] UploadDto uploadDto)
        {
            var image = uploadDto.File;
            // Validar entrada
            if (image == null || image.Length == 0)
            {
                return BadRequest(new { Message = "No se ha proporcionado ninguna imagen." });
            }

            // Crear carpeta de destino si no existe
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generar nombre único para la imagen y asegurar que la extensión sea válida
            var fileExtension = Path.GetExtension(image.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Query de inserción
            var query = @"
                    INSERT INTO Imagenes (Ruta, FechaSubida)
                    VALUES (@Ruta, GETDATE());
                    SELECT SCOPE_IDENTITY();";

            try
            {
                // Guardar la imagen en el servidor
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Guardar la ruta en la base de datos
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@Ruta", SqlDbType.NVarChar) { Value = filePath });

                // Ejecutar el comando y obtener el ID generado
                var newId = Convert.ToInt32(await command.ExecuteScalarAsync());

                // Respuesta exitosa
                return Ok(new
                {
                    Message = "Imagen subida y registrada en la base de datos exitosamente.",
                    FilePath = filePath,
                    ImageId = newId
                });
            }
            catch (SqlException sqlEx)
            {
                // Manejo específico para errores de base de datos
                return StatusCode(500, new { Message = "Error al acceder a la base de datos.", Detail = sqlEx.Message });
            }
            catch (IOException ioEx)
            {
                // Manejo de errores relacionados con la manipulación de archivos
                return StatusCode(500, new { Message = "Error al guardar el archivo en el servidor.", Detail = ioEx.Message });
            }
            catch (Exception ex)
            {
                // Manejo general de excepciones
                return StatusCode(500, new { Message = "Se produjo un error inesperado.", Detail = ex.Message });
            }
        }

    }
}