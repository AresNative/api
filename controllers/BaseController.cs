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
