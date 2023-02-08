using System;
using System.Text;
using System.Threading.Tasks;
using Beamable;
using Beamable.Api.Auth;
using Beamable.Common;
using Beamable.Common.Api.Auth;
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

	[SerializeField] private Federation _federation;
	[SerializeField] private Transform _logsParent;

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

		_beamId.text = _ctx.Accounts.Current.GamerTag.ToString();
	}

	private void Refresh()
	{
		_connectWalletButton.interactable = !Working && !WalletConnected;
		_attachIdentityButton.interactable = !Working && WalletConnected && !WalletAttached;
		_detachIdentityButton.interactable = !Working && WalletConnected && WalletAttached;

		_getExternalIdentitiesButton.interactable = !Working && WalletConnected;
		_signMessageButton.interactable = !Working && WalletConnected;

		_walletId.text = WalletConnected ? _account.PublicKey.Key : String.Empty;
	}

	private async void OnConnectClicked()
	{
		Working = true;

		Log("Connecting to a wallet...");
		await Login();
		if (WalletConnected)
		{
			WalletAttached = await CheckIfAttached();
		}

		Working = false;
	}

	private async void OnAttachClicked()
	{
		Working = true;
		Log("Attaching wallet...");
		await SendAttachRequest();
		WalletAttached = await CheckIfAttached();
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
					_account.PublicKey.Key, ChallengeHandler);
		}
	}


	private async void OnDetachClicked()
	{
		Working = true;
		Log("Detaching wallet...");
		await _ctx.Accounts.RemoveExternalIdentity<SolanaCloudIdentity, SolanaFederationClient>();
		WalletAttached = await CheckIfAttached();
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

	private async Promise<string> ChallengeHandler(string challengeToken)
	{
		ChallengeToken parsedToken = _authService.ParseChallengeToken(challengeToken);
		byte[] challengeBytes = Convert.FromBase64String(parsedToken.challenge);
		string challenge = Encoding.UTF8.GetString(challengeBytes);

		Log($"Signing a challenge {challenge}");
		byte[] signatureBytes = await _wallet.SignMessage(challengeBytes);
		string signedSignature = Convert.ToBase64String(signatureBytes);
		Log($"Signed signature: {signedSignature}");

		return signedSignature;
	}

	private async Promise<bool> CheckIfAttached()
	{
		User user = await _authService.GetUser();

		if (user == null || user.external.Count <= 0) return false;

		ExternalIdentity externalIdentity = user.external.Find(i =>
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
		_wallet = new InGameWallet(RpcCluster.DevNet, null, true);

		Mnemonic newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
		return await _wallet.Login("1234") ?? await _wallet.CreateAccount(newMnemonic.ToString(), "1234");
	}

	private async Task<Account> LoginPhantom()
	{
		_wallet = new PhantomWallet(_phantomWalletOptions, RpcCluster.DevNet, string.Empty, true);
		return await _wallet.Login();
	}

	private void Log(string message)
	{
#if UNITY_EDITOR
		Debug.Log(message);
// #elif (UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL)
		TextMeshProUGUI log = new GameObject("LogEntry").AddComponent<TextMeshProUGUI>();
		log.text = message;
		log.transform.SetParent(_logsParent, false);
#endif
	}
}