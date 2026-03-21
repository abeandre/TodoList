using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToDoApi.Models;
using ToDoApi.Services;

namespace ToDoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ToDoController(IToDoService toDoService) : ControllerBase
    {
        private Guid GetUserId()
        {
            return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        [HttpGet]
        [ProducesResponseType<IEnumerable<ToDoResponse>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ToDoResponse>>> GetAll()
        {
            return Ok(await toDoService.GetAllAsync(GetUserId()));
        }

        [HttpPost]
        [ProducesResponseType<ToDoResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ToDoResponse>> Create(CreateToDoRequest request)
        {
            var created = await toDoService.CreateAsync(GetUserId(), request);
            return Created((string?)null, created);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Update(Guid id, UpdateToDoRequest request)
        {
            return await toDoService.UpdateAsync(id, GetUserId(), request) ? NoContent() : NotFound();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            return await toDoService.DeleteAsync(id, GetUserId()) ? NoContent() : NotFound();
        }

        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ChangeStatus(Guid id, ChangeStatusRequest request)
        {
            return await toDoService.ChangeStatusAsync(id, GetUserId(), request.IsCompleted) ? NoContent() : NotFound();
        }
    }
}
