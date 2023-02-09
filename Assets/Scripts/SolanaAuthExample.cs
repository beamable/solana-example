using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beamable;
using Beamable.Api.Auth;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Content;
using Beamable.Player;
using Beamable.Server.Clients;
using Solana.Unity.SDK;
using Solana.Unity.Wallet.Bip39;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Account = Solana.Unity.Wallet.Account;

public class SolanaAuthExample : MonoBehaviour
{
	private BeamContext _ctx;

	[SerializeField] private TextMeshProUGUI _beamId;
	[SerializeField] private TextMeshProUGUI _walletId;

	[SerializeField] private Button _connectWalletButton;
	[SerializeField] private Button _attachIdentityButton;
	[SerializeField] private Button _detachIdentityButton;
	[SerializeField] private Button _getExternalIdentitiesButton;
	[SerializeField] private Button _signMessageButton;
	[SerializeField] private Button _walletExplorerButton;
	[SerializeField] private Button _getInventoryButton;

	[SerializeField] private Federation _federation;
	[SerializeField] private Transform _logsParent;
	[SerializeField] private string _walletPassword = "1234";

	private readonly PhantomWalletOptions _phantomWalletOptions = new() { appMetaDataUrl = "https://beamable.com" };
	private WalletBase _wallet;
	private IAuthService _authService;

	private Account _account;

	private Account Account
	{
		set
		{
			_account = value;
			Refresh();
		}
	}

	private bool _working;

	private bool Working
	{
		get => _working;
		set
		{
			_working = value;
			Refresh();
		}
	}

	private bool _walletAttached;

	private bool WalletAttached
	{
		get => _walletAttached;
		set
		{
			_walletAttached = value;
			Refresh();
		}
	}

	private bool WalletConnected => _account != null;

	public async void Start()
	{
		_ctx = BeamContext.Default;
		_authService = _ctx.Api.AuthService;

		Working = true;
		await _ctx.OnReady;
		await _ctx.Accounts.OnReady;
		Working = false;

		_connectWalletButton.onClick.AddListener(OnConnectClicked);
		_attachIdentityButton.onClick.AddListener(OnAttachClicked);
		_detachIdentityButton.onClick.AddListener(OnDetachClicked);
		_getExternalIdentitiesButton.onClick.AddListener(OnGetExternalClicked);
		_signMessageButton.onClick.AddListener(OnSignClicked);
		_walletExplorerButton.onClick.AddListener(OnWalletExplorerClicked);
		_getInventoryButton.onClick.AddListener(OnGetInventoryClicked);

		_beamId.text = _ctx.Accounts.Current.GamerTag.ToString();
	}

	private void Refresh()
	{
		_connectWalletButton.interactable = !Working && !WalletConnected;
		_attachIdentityButton.interactable = !Working && WalletConnected && !WalletAttached;
		_detachIdentityButton.interactable = !Working && WalletConnected && WalletAttached;
		_getExternalIdentitiesButton.interactable = !Working;
		_signMessageButton.interactable = !Working && WalletConnected;
		_walletExplorerButton.interactable = WalletConnected;
		_getInventoryButton.interactable = !Working;

		_walletId.text = WalletConnected ? _account.PublicKey.Key : String.Empty;
	}

	private async void OnConnectClicked()
	{
		Working = true;

		Log("Connecting to a wallet...");
		await Login();
		if (WalletConnected)
		{
			WalletAttached = CheckIfWalletHasAttachedIdentity();
		}

		Working = false;
	}

	private async void OnAttachClicked()
	{
		Working = true;
		Log("Attaching wallet...");
		await SendAttachRequest();
		WalletAttached = CheckIfWalletHasAttachedIdentity();
		Working = false;

		async Promise SendAttachRequest(ChallengeSolution challengeSolution = null)
		{
			StringBuilder builder = new();
			builder.AppendLine("Sending a request with:");
			builder.AppendLine($"Public key: {_account.PublicKey.Key}");
			builder.AppendLine($"Provider service: {_federation.Service}");
			if (challengeSolution != null)
			{
				builder.AppendLine($"Signed solution: {challengeSolution.solution}");
			}

			Log(builder.ToString());

			RegistrationResult result =
				await _ctx.Accounts.AddExternalIdentity<SolanaCloudIdentity, SolanaFederationClient>(
					_account.PublicKey.Key, SolveChallenge);

			if (result.isSuccess)
			{
				Log("Succesfully attached an external identity...");
			}
		}
	}


	private async void OnDetachClicked()
	{
		Working = true;
		Log("Detaching wallet...");
		await _ctx.Accounts.RemoveExternalIdentity<SolanaCloudIdentity, SolanaFederationClient>();
		WalletAttached = CheckIfWalletHasAttachedIdentity();
		Working = false;
	}

	private async void OnSignClicked()
	{
		Working = true;
		Log("Signing a message...");
		string message = "Sample message to sign";
		byte[] signatureBytes = await _wallet.SignMessage(Encoding.UTF8.GetBytes(message));
		string signedSignature = Convert.ToBase64String(signatureBytes);
		Log($"Signature (base64): {signedSignature}");
		Working = false;
	}

	private void OnGetExternalClicked()
	{
		Log("Gettting external identities info...");
		if (_ctx.Accounts.Current == null) return;

		if (_ctx.Accounts.Current.ExternalIdentities.Length != 0)
		{
			StringBuilder builder = new();
			foreach (ExternalIdentity identity in _ctx.Accounts.Current.ExternalIdentities)
			{
				builder.AppendLine(
					$"Service: {identity.providerService}, namespace: {identity.providerNamespace}, public key: {identity.userId}");
			}

			Log(builder.ToString());
		}
		else
		{
			Log("No external identities found...");
		}
	}

	private void OnWalletExplorerClicked()
	{
		var address =
			$"https://explorer.solana.com/address/{_account.PublicKey}?cluster=devnet";

		Application.OpenURL(address);
	}

	private async void OnGetInventoryClicked()
	{
		InventoryView view = await _ctx.Api.InventoryService.GetCurrent();

		ParseCurrencies(view.currencies);
		ParseItems(view.items);

		void ParseCurrencies(Dictionary<string, long> currencies)
		{
			StringBuilder builder = new();
			foreach (var (currency, amount) in currencies)
			{
				builder.AppendLine($"Currency: {currency}, amount: {amount}");
			}

			Log(builder.ToString());
		}

		void ParseItems(Dictionary<string, List<ItemView>> items)
		{
			foreach (var (itemId, itemInstances) in items)
			{
				StringBuilder builder = new();
				builder.AppendLine(itemId);
				builder.AppendLine("====================");

				foreach (ItemView instance in itemInstances)
				{
					StringBuilder itemBuilder = new();
					itemBuilder.AppendLine($"Id: {instance.id}");

					if (instance.properties.Count > 0)
					{
						itemBuilder.AppendLine("  Properties:");

						foreach (var (key, value) in instance.properties)
						{
							itemBuilder.AppendLine($"	{key}, {value}");
						}
					}

					builder.AppendLine(itemBuilder.ToString());
					builder.AppendLine("====================");
				}

				Log(builder.ToString());
			}
		}
	}

	private async Promise<string> SolveChallenge(string challengeToken)
	{
		Log($"Signing a challenge: {challengeToken}");
		
		// Parsing received challenge token to a 3 part struct
		ChallengeToken parsedToken = _authService.ParseChallengeToken(challengeToken);
		// Challenge we received to solve is Base64String 
		byte[] challengeBytes = Convert.FromBase64String(parsedToken.challenge);
		// Currently connected wallet is responsible for signing passed challenge. InGameWallet (in editor) is 
		// handling this automatically. PhantomWallet (mobile and WebGL) connects either with mobile app or browser
		// extension.
		byte[] signatureBytes = await _wallet.SignMessage(challengeBytes);
		// Signature is converted back to Base64String as that's the format that server is waiting for
		string signedSignature = Convert.ToBase64String(signatureBytes);
		
		Log($"Signed signature: {signedSignature}");

		return signedSignature;
	}

	private bool CheckIfWalletHasAttachedIdentity()
	{
		if (_ctx.Accounts.Current == null)
			return false;

		if (_ctx.Accounts.Current.ExternalIdentities.Length == 0)
			return false;

		ExternalIdentity externalIdentity = _ctx.Accounts.Current.ExternalIdentities.FirstOrDefault(i =>
			i.providerNamespace == _federation.Namespace && i.providerService == _federation.Service &&
			i.userId == _account.PublicKey);

		return externalIdentity != null;
	}

	public async Task Login()
	{
#if UNITY_EDITOR
		Account = await LoginInGame();
#elif (UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL)
		Account = await LoginPhantom();
#endif

		Log(_account != null
			? $"Wallet connected with PublicKey: {_account.PublicKey.Key}"
			: "Something gone wrong while connecting with a wallet");
	}

	private async Task<Account> LoginInGame()
	{
		// InGameWallet class is used for editor operations. It automatically approves all messages and transactions.
		_wallet = new InGameWallet(RpcCluster.DevNet, null, true);

		// We are retrieving local wallet or creating a new one if none was found
		return await _wallet.Login(_walletPassword) ??
		       await _wallet.CreateAccount(new Mnemonic(WordList.English, WordCount.Twelve).ToString(),
			       _walletPassword);
	}

	private async Task<Account> LoginPhantom()
	{
		// PhantomWallet class is used for clients built for Android, iOS and WebGL. It handles communication with
		// Phantom Wallet app installed on mobile device and Phantom Wallet browser extensions installed on desktop.
		_wallet = new PhantomWallet(_phantomWalletOptions, RpcCluster.DevNet, string.Empty, true);
		return await _wallet.Login();
	}

	private void Log(string message)
	{
#if UNITY_EDITOR
		Debug.Log(message);
#endif

#if (UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL)
		TextMeshProUGUI log = new GameObject("LogEntry").AddComponent<TextMeshProUGUI>();
		log.text = $"{message}";
		log.transform.SetParent(_logsParent, false);
#endif
	}
}