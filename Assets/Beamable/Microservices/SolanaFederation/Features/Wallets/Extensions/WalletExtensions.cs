using System.Text;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Storage.Models;
using Solnet.KeyStore;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions
{
    static class WalletExtensions
    {
        public static string EncryptMnemonic(this Solnet.Wallet.Wallet wallet)
        {
            var keystoreService = new SecretKeyStoreService();
            var mnemonicStringByteArray = Encoding.UTF8.GetBytes(wallet.Mnemonic.ToString());
            return keystoreService.EncryptAndGenerateDefaultKeyStoreAsJson(Configuration.RealmSecret, mnemonicStringByteArray,
                wallet.Account.PublicKey.Key);
        }

        public static Valut ToValut(this Solnet.Wallet.Wallet wallet)
        {
            return new Valut
            {
                Name = Configuration.RealmWalletName,
                Value = wallet.EncryptMnemonic()
            };
        }
    }
}