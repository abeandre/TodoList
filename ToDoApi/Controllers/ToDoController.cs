using Microsoft.AspNetCore.Mvc;
using ToDo.DataAccess.Repositories;

namespace ToDoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToDoController(IToDoRepository repository) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ToDo.DataAccess.ToDo>>> GetAll()
        {
            var todos = await repository.GetAllAsync();
            return Ok(todos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ToDo.DataAccess.ToDo>> GetById(Guid id)
        {
            var todo = await repository.GetByIdAsync(id);
            if (todo == null)
                return NotFound();

            return Ok(todo);
        }

        [HttpPost]
        public async Task<ActionResult> Create(ToDo.DataAccess.ToDo todo)
        {
            todo.Id = Guid.NewGuid();
            todo.CreatedAt = DateTime.Now;

            await repository.AddAsync(todo);
            return CreatedAtAction(nameof(GetById), new { id = todo.Id }, todo);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, ToDo.DataAccess.ToDo todo)
        {
            if (id != todo.Id)
                return BadRequest();

            await repository.UpdateAsync(todo);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            await repository.DeleteAsync(id);
            return Ok();
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult> ChangeStatus(Guid id, [FromBody] bool isCompleted)
        {
            await repository.ChangeStatusAsync(id, isCompleted);
            return Ok();
        }
    }
}
