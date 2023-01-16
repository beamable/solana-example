using Assets.Beamable.Microservices.SolanaFederation.Models;
using Solnet.KeyStore;
using Solnet.Wallet;
using System;
using System.Text;

namespace Assets.Beamable.Microservices.SolanaFederation.Extensions
{
	static class WalletExtensions
	{
		public static string EncryptMnemonic(this Wallet wallet)
		{
			var keystoreService = new SecretKeyStoreService();
			var stringByteArray = Encoding.UTF8.GetBytes(wallet.Mnemonic.ToString());
			return keystoreService.EncryptAndGenerateDefaultKeyStoreAsJson(Configuration.RealmSecret, stringByteArray, wallet.Account.PublicKey.Key);
		}

		public static PersistedWallet ToPersistedWallet(this Wallet wallet)
		{
			return new PersistedWallet
			{
				Created = DateTime.Now,
				KeyStore = wallet.EncryptMnemonic(),
				Name = Configuration.RealmWalletName
			};
		}
	}
}
