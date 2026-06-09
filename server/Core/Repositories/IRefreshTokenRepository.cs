using COM_project3.Core.Entities;
using System.Threading.Tasks;

namespace COM_project3.Core.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> GetByTokenAsync(string tokenHash);
        Task CreateAsync(RefreshToken token);
        Task UpdateAsync(RefreshToken token);
        Task RevokeAllForUserAsync(string userId);
    }
}
