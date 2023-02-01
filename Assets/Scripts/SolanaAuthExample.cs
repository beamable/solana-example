using System;
using System.Text;
using System.Threading.Tasks;
using Beamable;
using Beamable.Api.Auth;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Content;
using Solana.Unity.Rpc;
using Solana.Unity.SDK;
using Solana.Unity.Wallet.Bip39;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Account = Solana.Unity.Wallet.Account;

public class SolanaAuthExample : MonoBehaviour
{
	private BeamContext _ctx;

	[SerializeField] private GameObject _publicKeyGroup;
	[SerializeField] private TextMeshProUGUI _publicKeyValue;
	[SerializeField] private Button _connectWalletButton;
	[SerializeField] private Button _attachIdentityButton;
	[SerializeField] private Button _detachIdentityButton;
	[SerializeField] private Button _getExternalIdentitiesButton;
	[SerializeField] private Federation _federation;

	[SerializeField] private LogEntry _logEntryPrefab;
	[SerializeField] private Transform _logsParent;
	[SerializeField] private GameObject _logger;

	private readonly PhantomWalletOptions _phantomWalletOptions = new() { appMetaDataUrl = "https://beamable.com" };
	private readonly IRpcClient _rpcClient = ClientFactory.GetClient(Cluster.DevNet);
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

	private WalletBase _wallet;

	public async void Start()
	{
#if UNITY_EDITOR
		_logger.SetActive(false);
#endif

		Working = true;
		_ctx = BeamContext.Default;
		await _ctx.OnReady;
		_authService = _ctx.Api.AuthService;
		Working = false;

		_connectWalletButton.onClick.AddListener(OnConnectClicked);
		_attachIdentityButton.onClick.AddListener(OnAttachClicked);
		_detachIdentityButton.onClick.AddListener(OnDetachClicked);
		_getExternalIdentitiesButton.onClick.AddListener(OnGetExternalClicked);
	}

	private void Refresh()
	{
#if UNITY_EDITOR
		_connectWalletButton.interactable = !Working && !WalletConnected;
		_attachIdentityButton.interactable = !Working && WalletConnected && !WalletAttached;
		_detachIdentityButton.interactable = !Working && WalletConnected && WalletAttached;
		_getExternalIdentitiesButton.interactable = !Working && WalletConnected;
#elif UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
		_connectWalletButton.interactable = !Working && !WalletConnected;
		_attachIdentityButton.gameObject.SetActive(false);
		_detachIdentityButton.gameObject.SetActive(false);
		_getExternalIdentitiesButton.gameObject.SetActive(false);
#endif
		_publicKeyGroup.SetActive(WalletConnected);
		_publicKeyValue.text = WalletConnected ? _account.PublicKey.Key : String.Empty;
	}

	private async void OnConnectClicked()
	{
		// Temp
		Working = true;

		Log("Connecting to a wallet...");
		await Login();

		if (WalletConnected)
		{
			WalletAttached = await CheckIfAttached();
		}
	}

	private async void OnAttachClicked()
	{
		// Temp
		Working = true;

		Log("Attaching wallet...");
		await SendAttachRequest();
		WalletAttached = await CheckIfAttached();
	}

	private async void OnDetachClicked()
	{
		// Temp
		Working = true;

		Log("Detaching wallet...");
		await SendDetachRequest();
		WalletAttached = await CheckIfAttached();
	}

	private async void OnGetExternalClicked()
	{
		// Temp
		Working = true;

		Log("Gettting external identities info...");
		await GetExternalIdentities();
	}

	private async Promise SendAttachRequest(ChallengeSolution challengeSolution = null)
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

		await _authService
			.AttachIdentity(_account.PublicKey.Key, _federation.Service, _federation.Namespace, challengeSolution)
			.Then(HandleAttachResponse)
			.Error(HandleError);
	}

	private async void HandleAttachResponse(AttachExternalIdentityResponse response)
	{
		switch (response.result)
		{
			case "challenge":
			{
				if (!string.IsNullOrEmpty(response.challenge_token))
				{
					Log($"Received challenge token: {response.challenge_token}");

					ChallengeToken challengeToken = _authService.ParseChallengeToken(response.challenge_token);
					byte[] challengeTokenBytes = Convert.FromBase64String(challengeToken.challenge);

					Log("Signing challenge token...");

					byte[] signature = _account.Sign(challengeTokenBytes);
					string signedSignature = Convert.ToBase64String(signature);
					Log($"Signed signature: {signedSignature}");

					ChallengeSolution solution = new ChallengeSolution
					{
						challenge_token = response.challenge_token, solution = signedSignature
					};

					Log(
						$"Sending a request with PublicKey: {_account.PublicKey.Key}, " +
						$"ProviderService: {_federation.Service}, ProviderNamespace: {_federation.Namespace}");

					await SendAttachRequest(solution);
				}

				break;
			}
			case "ok":
				Log("Succesfully attached external identity");

				// Temp
				WalletAttached = true;
				Working = false;
				break;
			default:
				Log("Something gone wrong while attaching external identity");

				// Temp
				WalletAttached = false;
				Working = false;
				break;
		}
	}

	private async Promise SendDetachRequest()
	{
		await _authService.DetachIdentity(_federation.Service, _account.PublicKey, _federation.Namespace)
			.Then(HandleDetachResponse)
			.Error(HandleError);
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

	private void HandleDetachResponse(DetachExternalIdentityResponse response)
	{
		switch (response.result)
		{
			case "ok":
				Log("Succesfully detached external identity");
				break;
			default:
				Log("Something gone wrong while detaching external identity");
				break;
		}

		// Temp
		WalletAttached = false;
		Working = false;
	}

	private async Promise GetExternalIdentities()
	{
		User user = await _authService.GetUser();

		StringBuilder builder = new();

		if (user != null && user.external.Count > 0)
		{
			foreach (ExternalIdentity identity in user.external)
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

		Working = false;
	}

	private void HandleError(Exception obj)
	{
		Log(obj.Message);
		Working = false;
	}

	public async Task Login()
	{
#if UNITY_EDITOR
		Account = await LoginInGame();
#elif UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
		Account = await LoginPhantom();
#endif

		Log(_account != null
			? $"Wallet connected with PublicKey: {_account.PublicKey.Key}"
			: "Something gone wrong while connecting with a wallet");

		Working = false;
	}

	private async Task<Account> LoginInGame()
	{
		_wallet = new InGameWallet();

		var newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
		return await _wallet.Login("1234") ?? await _wallet.CreateAccount(newMnemonic.ToString(), "1234");
	}

	private async Task<Account> LoginPhantom()
	{
		_wallet = new PhantomWallet(_phantomWalletOptions);
		return await _wallet.Login();
	}

	private void Log(string message)
	{
#if UNITY_EDITOR
		Debug.Log(message);
#elif UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
		LogEntry logEntry = Instantiate(_logEntryPrefab, _logsParent, false);
		logEntry.Setup(message);
#endif
	}
}