using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using ToDo.DataAccess;
using ToDo.DataAccess.Repositories;
using ToDoApi.Models;

namespace ToDoApi.Services
{
    public class UserService(IUserRepository repository, IMapper mapper, ILogger<UserService> logger) : IUserService
    {
        public async Task<UserResponse> CreateAsync(CreateUserRequest request)
        {
            var existingUser = await repository.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ArgumentException("Email is already registered");
            }

            var user = mapper.Map<User>(request);
            user.Id = Guid.NewGuid();
            user.CreatedDate = DateTime.UtcNow;
            user.LastModifiedDate = DateTime.UtcNow;

            var saltBytes = new byte[32];
            RandomNumberGenerator.Fill(saltBytes);
            user.Salt = Convert.ToBase64String(saltBytes);
            user.HashedPassword = Convert.ToBase64String(
                Rfc2898DeriveBytes.Pbkdf2(request.Password, saltBytes, 100_000, HashAlgorithmName.SHA512, 64));

            await repository.CreateUserAsync(user);
            logger.LogInformation("Created user {Id} with email '{Email}'", user.Id, user.Email);
            
            return mapper.Map<UserResponse>(user);
        }

        public async Task<bool> UpdateAsync(Guid id, UpdateUserRequest request)
        {
            var user = await repository.GetUserByIdAsync(id);
            if (user is null)
            {
                logger.LogWarning("Update failed — user {Id} not found", id);
                return false;
            }

            mapper.Map(request, user);
            user.LastModifiedDate = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.Password))
            {
                var saltBytes = new byte[32];
                RandomNumberGenerator.Fill(saltBytes);
                user.Salt = Convert.ToBase64String(saltBytes);
                user.HashedPassword = Convert.ToBase64String(
                    Rfc2898DeriveBytes.Pbkdf2(request.Password, saltBytes, 100_000, HashAlgorithmName.SHA512, 64));
            }

            await repository.UpdateUserAsync(user);
            logger.LogInformation("Updated user {Id}", id);
            return true;
        }
    }
}
