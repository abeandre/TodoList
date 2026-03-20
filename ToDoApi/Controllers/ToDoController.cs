using Microsoft.AspNetCore.Mvc;
using ToDo.DataAccess.Repositories;
using ToDoApi.Models;

namespace ToDoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToDoController(IToDoRepository repository) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<IEnumerable<ToDoResponse>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ToDoResponse>>> GetAll()
        {
            var todos = await repository.GetAllAsync();
            return Ok(todos.Select(ToDoResponse.From));
        }

        [HttpGet("{id}")]
        [ProducesResponseType<ToDoResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ToDoResponse>> GetById(Guid id)
        {
            var todo = await repository.GetByIdAsync(id);
            if (todo == null)
                return NotFound();

            return Ok(ToDoResponse.From(todo));
        }

        [HttpPost]
        [ProducesResponseType<ToDoResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ToDoResponse>> Create(CreateToDoRequest request)
        {
            var todo = new ToDo.DataAccess.ToDo
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(todo);
            return CreatedAtAction(nameof(GetById), new { id = todo.Id }, ToDoResponse.From(todo));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Update(Guid id, UpdateToDoRequest request)
        {
            var todo = await repository.GetByIdAsync(id);
            if (todo == null)
                return NotFound();

            todo.Title = request.Title;
            todo.Description = request.Description ?? string.Empty;
            await repository.UpdateAsync(todo);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var found = await repository.DeleteAsync(id);
            if (!found)
                return NotFound();

            return NoContent();
        }

        [HttpPatch("{id}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ChangeStatus(Guid id, [FromBody] bool isCompleted)
        {
            var found = await repository.ChangeStatusAsync(id, isCompleted);
            if (!found)
                return NotFound();

            return NoContent();
        }
    }
}
