using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InventarioFisico.Models.Auth;
using InventarioFisico.Services.Auth;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (!_authService.LoginValido(request.Usuario, request.Password))
            {
                _logger.LogWarning("Login inválido. Usuario={Usuario}", request?.Usuario);
                return Unauthorized(new
                {
                    success = false,
                    message = "Credenciales inválidas"
                });
            }

            _logger.LogInformation("Login exitoso. Usuario={Usuario}", request.Usuario);

            return Ok(new
            {
                success = true,
                usuario = request.Usuario,
                rol = "INVENTARIO_FISICO_ADMIN"
            });
        }
    }
}
