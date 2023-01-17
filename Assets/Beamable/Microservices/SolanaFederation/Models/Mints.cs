using System.Collections.Generic;
using System.Linq;
using Beamable.Microservices.SolanaFederation.Storage.Models;

namespace Beamable.Microservices.SolanaFederation.Models
{
    public class Mints
    {
        private readonly Dictionary<string, string> _mintsByContent;
        private readonly Dictionary<string, string> _mintsByTokens;

        public Mints(IList<Mint> mints)
        {
            _mintsByContent = mints.ToDictionary(x => x.ContentId, x => x.PublicKey);
            _mintsByTokens = mints.ToDictionary(x => x.PublicKey, x => x.ContentId);
        }

        public void AddMint(Mint mint)
        {
            _mintsByContent[mint.ContentId] = mint.PublicKey;
            _mintsByTokens[mint.PublicKey] = mint.ContentId;
        }

        public string GetByContent(string contentId) => _mintsByContent.GetValueOrDefault(contentId);

        public string GetByToken(string token) => _mintsByTokens.GetValueOrDefault(token);

        public bool ContainsContent(string contentId) => _mintsByContent.ContainsKey(contentId);

        public bool ContainsMint(string mint) => _mintsByTokens.ContainsKey(mint);

        public HashSet<string> ContentIds => _mintsByContent.Keys.ToHashSet();

        public HashSet<string> Tokens => _mintsByTokens.Keys.ToHashSet();
    }
}