using Core.Repositories;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Data.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IMongoCollection<Entities.RefreshToken> _refreshTokens;

        public RefreshTokenRepository(MongoDbContext context)
        {
            _refreshTokens = context.RefreshTokens;
        }

        public async Task<Core.Entities.RefreshToken?> GetByTokenAsync(string tokenHash)
        {
            var entity = await _refreshTokens.Find(rt => rt.TokenHash == tokenHash).FirstOrDefaultAsync();
            return entity?.ToDomainEntity();
        }

        public async Task CreateAsync(Core.Entities.RefreshToken token)
        {
            var entity = Entities.RefreshToken.FromDomainEntity(token);
            await _refreshTokens.InsertOneAsync(entity);
            token.Id = entity.Id; // update domain ID after insert
        }

        public async Task UpdateAsync(Core.Entities.RefreshToken token)
        {
            var entity = Entities.RefreshToken.FromDomainEntity(token);
            await _refreshTokens.ReplaceOneAsync(rt => rt.Id == token.Id, entity);
        }

        public async Task RevokeAllForUserAsync(string userId)
        {
            var filter = Builders<Entities.RefreshToken>.Filter.Eq(rt => rt.UserId, userId);
            var update = Builders<Entities.RefreshToken>.Update.Set(rt => rt.IsRevoked, true);
            await _refreshTokens.UpdateManyAsync(filter, update);
        }
    }
}
