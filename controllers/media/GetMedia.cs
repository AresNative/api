using Microsoft.AspNetCore.Mvc;

namespace TuProyecto.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ArchivosController : ControllerBase
{
    private readonly string _rutaArchivos = @"C:\inetpub\wwwroot\publish\uploads";

    [HttpGet("{nombreArchivo}")]
    public IActionResult ObtenerArchivo(string nombreArchivo)
    {
        // Combina la ruta base con el nombre del archivo
        var rutaCompleta = Path.Combine(_rutaArchivos, nombreArchivo);

        // Verifica si el archivo existe
        if (!System.IO.File.Exists(rutaCompleta))
        {
            return NotFound(new { mensaje = "Archivo no encontrado." });
        }

        // Obtiene el contenido del archivo
        var contenidoArchivo = System.IO.File.ReadAllBytes(rutaCompleta);
        var tipoMime = GetMimeType(nombreArchivo);

        // Devuelve el archivo como resultado
        return File(contenidoArchivo, tipoMime, nombreArchivo);
    }

    private string GetMimeType(string fileName)
    {
        // Determina el tipo MIME basado en la extensión del archivo
        return fileName.ToLower() switch
        {
            string f when f.EndsWith(".jpg") || f.EndsWith(".jpeg") => "image/jpeg",
            string f when f.EndsWith(".png") => "image/png",
            string f when f.EndsWith(".gif") => "image/gif",
            string f when f.EndsWith(".bmp") => "image/bmp",
            string f when f.EndsWith(".webp") => "image/webp",
            string f when f.EndsWith(".pdf") => "application/pdf",
            string f when f.EndsWith(".txt") => "text/plain",
            string f when f.EndsWith(".html") => "text/html",
            string f when f.EndsWith(".css") => "text/css",
            string f when f.EndsWith(".json") => "application/json",
            string f when f.EndsWith(".xml") => "application/xml",
            string f when f.EndsWith(".zip") => "application/zip",
            string f when f.EndsWith(".mp4") => "video/mp4",
            _ => "application/octet-stream" // Tipo MIME genérico
        };
    }
}