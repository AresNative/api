using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        private readonly string _connectionString;

        // Constructor para inyectar IConfiguration y obtener la cadena de conexión
        public BaseController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public string GetMimeType(string fileName)
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
        // Método protegido para abrir una conexión de forma asíncrona y manejar su ciclo de vida
        protected async Task<SqlConnection> OpenConnectionAsync()
        {
            // Creamos la conexión usando el connection string
            var connection = new SqlConnection(_connectionString);

            // Abrimos la conexión y aseguramos que se cierre después del uso (a través de await using)
            await connection.OpenAsync();
            return connection;
        }

        // Método centralizado para manejar excepciones
        protected IActionResult HandleException(Exception ex, string? query = null)
        {
            // Limpia los caracteres de nueva línea en el mensaje de error y la consulta
            string sanitizedMessage = ex.Message.Replace("\r", "").Replace("\n", " ");
            string? sanitizedQuery = query?.Replace("\r", "").Replace("\n", " ");

            // Crea el objeto de respuesta con el mensaje de error limpio
            var response = new
            {
                Message = $"Error: {sanitizedMessage}",
                Query = sanitizedQuery // Se incluye solo si no es null
            };

            // Devuelve el estado de error con la respuesta
            return StatusCode(500, response);
        }


        // Si deseas manejar excepciones más específicas, puedes sobrecargar el método
        protected IActionResult HandleException(Exception ex, int statusCode)
        {
            return StatusCode(statusCode, new { Message = $"Error: {ex.Message}" });
        }
    }
}
