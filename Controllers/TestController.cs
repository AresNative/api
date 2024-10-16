using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtSwaggerExample.Controllers
{
    [Route("api/test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        // Endpoint público, no requiere autenticación
        [HttpGet("public")]
        public IActionResult Public()
        {
            return Ok(new { Message = "Este es un endpoint público." });
        }

        // Endpoint protegido, requiere autenticación con JWT
        [Authorize]
        [HttpGet("protected")]
        public IActionResult Protected()
        {
            return Ok(new { Message = "Este es un endpoint protegido, necesitas un JWT válido para acceder." });
        }
    }
}
