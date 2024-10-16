using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration; // Asegúrate de que tienes esta referencia para IConfiguration.
using System.Threading.Tasks;

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
        protected IActionResult HandleException(Exception ex)
        {
            // Aquí puedes agregar más lógica para registrar el error en un sistema de logging, si lo deseas
            return StatusCode(500, new { Message = $"Error: {ex.Message}" });
        }

        // Si deseas manejar excepciones más específicas, puedes sobrecargar el método
        protected IActionResult HandleException(Exception ex, int statusCode)
        {
            return StatusCode(statusCode, new { Message = $"Error: {ex.Message}" });
        }
    }
}
