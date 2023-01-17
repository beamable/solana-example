using System.Text;
using Beamable.Microservices.SolanaFederation.Storage.Models;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;

namespace Beamable.Microservices.SolanaFederation.Extensions
{
    internal static class ValutExtensions
    {
        public static Wallet ToWallet(this Valut valut)
        {
            var decryptedKeystore = valut.DecryptValue();
            var mnemonicString = Encoding.UTF8.GetString(decryptedKeystore);

            var restoredMnemonic = new Mnemonic(mnemonicString);
            return new Wallet(restoredMnemonic);
        }
    }
}