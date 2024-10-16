using Microsoft.IdentityModel.Tokens;
using MyApiProject.Models;
using Microsoft.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging; // Para el manejo de logs

public class AuthUtils
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthUtils> _logger;
    DateTime now = DateTime.Now;

    // Constructor para inyectar IConfiguration y ILogger
    public AuthUtils(IConfiguration configuration, ILogger<AuthUtils> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> IsValidUser(LoginModel login)
    {
        string query = "SELECT COUNT(1) FROM Website_users WHERE email = @Email AND password = @Password";
        try
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            await using var command = new SqlCommand(query, connection);

            // Asegúrate de usar hashes para las contraseñas en un entorno real
            command.Parameters.AddWithValue("@Email", login.Email);
            command.Parameters.AddWithValue("@Password", login.Password); // Hash recomendado

            var result = (int)await command.ExecuteScalarAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar el usuario.");
            return false;
        }
    }

    public async Task<int> IsValidUserId(int id)
    {
        string query = "SELECT id FROM Website_users WHERE id = @ID";
        try
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            await using var command = new SqlCommand(query, connection);

            // Asegúrate de usar hashes para las contraseñas en un entorno real
            command.Parameters.AddWithValue("@ID", id);

            //var result = await command.ExecuteScalarAsync();
            var result = (int)await command.ExecuteScalarAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar el usuario.");
            return -1;
        }
    }

    public async Task<int> GetUserId(string email)
    {
        string query = "SELECT id FROM Website_users WHERE email = @Email";
        try
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email);

            var result = await command.ExecuteScalarAsync();
            return result != null ? (int)result : 0; // Verifica si el resultado es null
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el ID del usuario.");
            throw new Exception("Error al obtener el ID del usuario.");
        }
    }

    public async Task InsertUserHistory(int userId, string movement)
    {
        string query = "INSERT INTO Website_user_history (id_user, mov, date) VALUES (@IdUser, @Movement, @Timestamp)";
        try
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@IdUser", userId);
            command.Parameters.AddWithValue("@Movement", movement);
            command.Parameters.AddWithValue("@Timestamp", now);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al insertar el historial del usuario.");
            throw new Exception("Error al insertar el historial del usuario.");
        }
    }

    public async Task InsertUserSession(int userId, string token, int active)
    {
        string query = "INSERT INTO Website_sessions (id_user, token, active) VALUES (@IdUser, @Token, @Active)";
        try
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@IdUser", userId);
            command.Parameters.AddWithValue("@Token", token);
            command.Parameters.AddWithValue("@Active", active);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al insertar el historial del usuario.");
            throw new Exception("Error al insertar el historial del usuario.");
        }
    }
    public string GetEmailFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        // Intentamos leer el token JWT
        var jwtToken = tokenHandler.ReadJwtToken(token);

        // Buscamos el claim que contiene el correo (sub o name claim del esquema SOAP)
        var emailClaim = jwtToken.Claims.FirstOrDefault(c =>
            c.Type == "sub" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");

        // Verificamos si se encontró el claim y retornamos su valor
        return emailClaim?.Value ?? throw new Exception("El token no contiene un claim de correo.");
    }


    public async Task Logout(int userId, string token)
    {
        string query = "UPDATE Website_sessions SET active = 0 WHERE id_user = @IdUser AND token = @Token";
        try
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@IdUser", userId);
            command.Parameters.AddWithValue("@Token", token);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al hacer logout.");
            throw new Exception("Error al hacer logout.");
        }
    }
    // Método para invalidar todas las sesiones activas de un usuario (útil para cerrar sesión en todos los dispositivos)
    public async Task<bool> InvalidateUserSessions(int userId)
    {
        string query = "UPDATE Website_sessions SET active = 0 WHERE id_user = @IdUser AND active = 1";

        try
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            await using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@IdUser", userId);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                // Registro del movimiento en el historial
                await InsertUserHistory(userId, "Todas las sesiones del usuario fueron cerradas");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al invalidar las sesiones del usuario.");
            return false;
        }
    }

}
