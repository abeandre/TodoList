using Microsoft.AspNetCore.Mvc;
using ToDoApi.Models;
using ToDoApi.Services;

namespace ToDoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToDoController(IToDoService service) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<IEnumerable<ToDoResponse>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ToDoResponse>>> GetAll()
        {
            return Ok(await service.GetAllAsync());
        }

        [HttpPost]
        [ProducesResponseType<ToDoResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ToDoResponse>> Create(CreateToDoRequest request)
        {
            var created = await service.CreateAsync(request);
            return Created((string?)null, created);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Update(Guid id, UpdateToDoRequest request)
        {
            return await service.UpdateAsync(id, request) ? NoContent() : NotFound();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            return await service.DeleteAsync(id) ? NoContent() : NotFound();
        }

        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ChangeStatus(Guid id, ChangeStatusRequest request)
        {
            return await service.ChangeStatusAsync(id, request.IsCompleted) ? NoContent() : NotFound();
        }
    }
}
