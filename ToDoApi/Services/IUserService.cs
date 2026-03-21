using System;
using System.Threading.Tasks;
using ToDoApi.Models;

namespace ToDoApi.Services
{
    public interface IUserService
    {
        Task<UserResponse> CreateAsync(CreateUserRequest request);
        Task<bool> UpdateAsync(Guid id, UpdateUserRequest request);
    }
}
