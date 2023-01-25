using System.Text;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Storage.Models;
using Solana.Unity.KeyStore;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions
{
	internal static class WalletExtensions
	{
		public static string EncryptMnemonic(this Wallet wallet)
		{
			var keystoreService = new SecretKeyStoreService();
			var mnemonicStringByteArray = Encoding.UTF8.GetBytes(wallet.Mnemonic.ToString());
			return keystoreService.EncryptAndGenerateDefaultKeyStoreAsJson(Configuration.RealmSecret, mnemonicStringByteArray,
				wallet.Account.PublicKey.Key);
		}

		public static Vault ToVault(this Wallet wallet)
		{
			return new Vault
			{
				Name = Configuration.RealmWalletName,
				Value = wallet.EncryptMnemonic()
			};
		}

		public static Account GetAccount(this Wallet wallet, string name)
		{
			var signature = wallet.Sign(Encoding.UTF8.GetBytes(name));
			var namedWallet = new Wallet(signature);
			return namedWallet.Account;
		}
	}
}