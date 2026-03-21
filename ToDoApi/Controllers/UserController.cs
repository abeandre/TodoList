using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ToDoApi.Models;
using ToDoApi.Services;

namespace ToDoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController(IUserService service, IAuthService authService) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
        {
            try
            {
                var created = await service.CreateAsync(request);
                created.Token = authService.GenerateToken(created.Id, created.Name, created.Email);
                return Created((string?)null, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Update(Guid id, UpdateUserRequest request)
        {
            var callerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(callerIdStr, out var callerId) || callerId != id)
                return Forbid();

            return await service.UpdateAsync(id, request) ? NoContent() : NotFound();
        }
    }
}
