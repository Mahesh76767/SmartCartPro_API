using SmartCartPro.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCartPro.DataAccess.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int userId);
        Task<int> CreateUserAsync(User user);
        Task<bool> EmailExistsAsync(string email);
        Task SaveRefreshTokenAsync(int userId, string token, DateTime expiry);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token);
        Task UpdateLastLoginAsync(int userId);

    }
}
