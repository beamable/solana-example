using System;
using System.Text;
using System.Threading.Tasks;
using Beamable;
using Beamable.Api.Auth;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Content;
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
	[SerializeField] private Button _signMessageButton;
	[SerializeField] private Federation _federation;

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
		Working = true;
		_ctx = BeamContext.Default;
		await _ctx.OnReady;
		_authService = _ctx.Api.AuthService;
		Working = false;

		_connectWalletButton.onClick.AddListener(OnConnectClicked);
		_attachIdentityButton.onClick.AddListener(OnAttachClicked);
		_detachIdentityButton.onClick.AddListener(OnDetachClicked);
		_getExternalIdentitiesButton.onClick.AddListener(OnGetExternalClicked);
		_signMessageButton.onClick.AddListener(OnSignClicked);
	}

	private void Refresh()
	{
		_connectWalletButton.interactable = !Working && !WalletConnected;
		_attachIdentityButton.interactable = !Working && WalletConnected && !WalletAttached;
		_detachIdentityButton.interactable = !Working && WalletConnected && WalletAttached;
		_getExternalIdentitiesButton.interactable = !Working && WalletConnected;
		_signMessageButton.interactable = !Working && WalletConnected;

		_publicKeyGroup.SetActive(WalletConnected);
		_publicKeyValue.text = WalletConnected ? _account.PublicKey.Key : String.Empty;
	}

	private async void OnConnectClicked()
	{
		// Temp
		Working = true;

		Debug.Log("Connecting to a wallet...");
		await Login();

		if (WalletConnected)
		{
			WalletAttached = await CheckIfAttached();
		}
	}

	private async void OnSignClicked()
	{
		Debug.Log("Signing...");
		
		var message = "hello world";
		var signatureBytes = await _wallet.SignMessage(message);
		var signatureString = Convert.ToBase64String(signatureBytes);
		
		Debug.Log($"Signature (base64): {signatureString}");
	}
	
	private async void OnAttachClicked()
	{
		// Temp
		Working = true;

		Debug.Log("Attaching wallet...");
		await SendAttachRequest();
		WalletAttached = await CheckIfAttached();
	}

	private async void OnDetachClicked()
	{
		// Temp
		Working = true;

		Debug.Log("Detaching wallet...");
		await SendDetachRequest();
		WalletAttached = await CheckIfAttached();
	}

	private async void OnGetExternalClicked()
	{
		// Temp
		Working = true;

		Debug.Log("Gettting external identities info...");
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

		Debug.Log(builder.ToString());

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
					Debug.Log($"Received challenge token: {response.challenge_token}");

					ChallengeToken challengeToken = _authService.ParseChallengeToken(response.challenge_token);
					byte[] challengeBytes = Convert.FromBase64String(challengeToken.challenge);
					var challenge = Encoding.UTF8.GetString(challengeBytes); 

					Debug.Log($"Signing challenge {challenge}");
					byte[] signatureBytes = await _wallet.SignMessage(challenge);
					string signatureBase64 = Convert.ToBase64String(signatureBytes);
					Debug.Log($"Signature: {signatureBase64}");

					ChallengeSolution solution = new ChallengeSolution
					{
						challenge_token = response.challenge_token, solution = signatureBase64
					};

					Debug.Log(
						$"Sending a request with PublicKey: {_account.PublicKey.Key}, " +
						$"ProviderService: {_federation.Service}, ProviderNamespace: {_federation.Namespace}");

					await SendAttachRequest(solution);
				}

				break;
			}
			case "ok":
				Debug.Log("Successfully attached external identity");

				// Temp
				WalletAttached = true;
				Working = false;
				break;
			default:
				Debug.Log("Something gone wrong while attaching external identity");

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
				Debug.Log("Succesfully detached external identity");
				break;
			default:
				Debug.Log("Something gone wrong while detaching external identity");
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
				builder.AppendLine($"Service: {identity.providerService}, namespace: {identity.providerNamespace}, public key: {identity.userId}");
			}

			Debug.Log(builder.ToString());
		}
		else
		{
			Debug.Log("No external identities found...");
		}

		Working = false;
	}

	private void HandleError(Exception obj)
	{
		Debug.LogError(obj.Message);
		Working = false;
	}

	public async Task Login()
	{
#if UNITY_EDITOR
		Account = await LoginInGame();
#elif (UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL)
		Account = await LoginPhantom();
#endif

		Debug.Log(_account != null
			? $"Wallet connected with PublicKey: {_account.PublicKey.Key}"
			: "Something gone wrong while connecting with a wallet");

		Working = false;
	}

	private async Task<Account> LoginInGame()
	{
		_wallet = new InGameWallet(RpcCluster.DevNet, null, true);

		var newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
		return await _wallet.Login("1234") ?? await _wallet.CreateAccount(newMnemonic.ToString(), "1234");
	}

	private async Task<Account> LoginPhantom()
	{
		_wallet = new PhantomWallet(_phantomWalletOptions, RpcCluster.DevNet, string.Empty, true);
		return await _wallet.Login();
	}
}