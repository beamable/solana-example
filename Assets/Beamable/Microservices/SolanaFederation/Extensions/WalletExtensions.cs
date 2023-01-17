using Assets.Beamable.Microservices.SolanaFederation.Storage.Models;
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

		public static Valut ToValut(this Wallet wallet)
		{
			return new Valut
			{
				Created = DateTime.Now,
				Value = wallet.EncryptMnemonic(),
				Name = Configuration.RealmWalletName
			};
		}
	}
}
