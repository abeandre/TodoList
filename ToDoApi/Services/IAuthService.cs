using System.Threading.Tasks;
using ToDoApi.Models;

namespace ToDoApi.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> AuthenticateAsync(AuthRequest request);
        string GenerateToken(System.Guid userId, string name, string email);
    }
}
