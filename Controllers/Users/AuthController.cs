using Microsoft.AspNetCore.Mvc;
using MyApiProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace MyApiProject.Controllers
{
    [Route("api/v1/users/")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TokensUtils _tokensUtils;
        private readonly AuthUtils _authUtils;

        public AuthController(TokensUtils tokensUtils, AuthUtils authUtils)
        {
            _tokensUtils = tokensUtils;
            _authUtils = authUtils;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel login)
        {
            // Aquí validarías las credenciales contra la base de datos
            if (await _authUtils.IsValidUser(login))
            {
                var token = _tokensUtils.GenerateJwtToken(login);
                var userId = await _authUtils.GetUserId(login.Email);
                await _authUtils.InsertUserSession(userId, token, '1');
                await _authUtils.InsertUserHistory(userId, "LogIn");
                return Ok(new { Token = token, UserId = userId });
            }
            else
            {
                return Unauthorized(new { Message = "Credenciales inválidas" });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> logOut(int id)
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var email = _authUtils.GetEmailFromToken(token);

            // Marcar el logout en el historial de usuario
            await _authUtils.InsertUserHistory(id, "LogOut");
            await _authUtils.Logout(id, token);

            return Ok(new { Message = "Logout exitoso.", Email = email });
        }

    }
}
