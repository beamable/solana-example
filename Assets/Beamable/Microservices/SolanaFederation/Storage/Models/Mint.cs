using MongoDB.Bson;

namespace Beamable.Microservices.SolanaFederation.Storage.Models
{
    public class Mint
    {
        public ObjectId _id { get; set; } = ObjectId.GenerateNewId();
        public string ContentId { get; set; }
        public string PublicKey { get; set; }
    }
}