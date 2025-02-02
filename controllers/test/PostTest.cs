using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ILogger<UploadController> _logger;

    public UploadController(ILogger<UploadController> logger)
    {
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] TestDto uploadDto)
    {
        try
        {
            // Validar que el archivo y la descripción no sean nulos
            if (uploadDto.File == null || uploadDto.Description == null)
            {
                return BadRequest("Archivo y descripción son requeridos.");
            }

            // Procesar el archivo
            var file = uploadDto.File;
            var filePath = Path.Combine("Uploads", file.FileName);

            // Guardar el archivo en el servidor
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Procesar la descripción
            var description = uploadDto.Description;
            _logger.LogInformation($"Descripción recibida: Título = {description.Title}, Detalles = {description.Details}, Fecha = {description.CreatedAt}");

            // Aquí podrías guardar la información en una base de datos o realizar otras operaciones

            return Ok(new
            {
                Message = "Archivo subido exitosamente.",
                FileName = file.FileName,
                FileSize = file.Length,
                Description = description
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir el archivo.");
            return StatusCode(500, "Ocurrió un error interno al procesar la solicitud.");
        }
    }
}