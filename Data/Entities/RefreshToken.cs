using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Data.Entities
{
    public class RefreshToken
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("tokenHash")]
        public string TokenHash { get; set; } = string.Empty;

        [BsonElement("expiresAt")]
        public DateTime ExpiresAt { get; set; }

        [BsonElement("isRevoked")]
        public bool IsRevoked { get; set; }

        // Mappers
        public Core.Entities.RefreshToken ToDomainEntity()
        {
            return new Core.Entities.RefreshToken
            {
                Id = this.Id,
                UserId = this.UserId,
                TokenHash = this.TokenHash,
                ExpiresAt = this.ExpiresAt,
                IsRevoked = this.IsRevoked
            };
        }

        public static RefreshToken FromDomainEntity(Core.Entities.RefreshToken token)
        {
            return new RefreshToken
            {
                Id = token.Id,
                UserId = token.UserId,
                TokenHash = token.TokenHash,
                ExpiresAt = token.ExpiresAt,
                IsRevoked = token.IsRevoked
            };
        }
    }
}
