using Assets.Beamable.Microservices.SolanaFederation.Storage.Models;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using System.Text;

namespace Assets.Beamable.Microservices.SolanaFederation.Extensions
{
	static class ValutExtensions
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
