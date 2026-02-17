using Microsoft.AspNetCore.Mvc;
using YP_API.Services;

namespace YP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(
            [FromForm] string username,
            [FromForm] string password)
        {
            try
            {
                var user = await _authService.Login(username, password);
                return Ok(new
                {
                    success = true,
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    message = "Успешный вход",
                    isAdmin = user.IsAdmin
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(
            [FromForm] string username,
            [FromForm] string password,
            [FromForm] string email)
        {
            try
            {
                var user = await _authService.Register(username, email, password);

                return Ok(new
                {
                    success = true,
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    message = "Пользователь создан"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}