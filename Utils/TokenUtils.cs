using Microsoft.IdentityModel.Tokens;
using MyApiProject.Models;
using Microsoft.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging; // Para el manejo de logs

public class TokensUtils
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokensUtils> _logger;
    DateTime now = DateTime.Now;

    // Constructor para inyectar IConfiguration y ILogger
    public TokensUtils(IConfiguration configuration, ILogger<TokensUtils> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> IsTokenActive(string email, string token)
    {
        string query = "SELECT active FROM Website_sessions WHERE token = @Token AND id_user = (SELECT id FROM Website_users WHERE email = @Email)";
        try
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Token", token);
            command.Parameters.AddWithValue("@Email", email);

            var result = await command.ExecuteScalarAsync();
            return result != null && (bool)result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar el estado del token.");
            return false;
        }
    }


    public string GenerateJwtToken(LoginModel login)
    {
        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, login.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.Name, login.Email)
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
