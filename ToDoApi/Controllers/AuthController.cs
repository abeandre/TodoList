using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ToDoApi.Models;
using ToDoApi.Services;

namespace ToDoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("login")]
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Login(AuthRequest request)
        {
            var response = await authService.AuthenticateAsync(request);
            if (response == null)
            {
                return Unauthorized(new { Message = "Invalid email or password" });
            }

            return Ok(response);
        }
    }
}
