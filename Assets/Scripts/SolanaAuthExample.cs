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

	[SerializeField] private Federation _federation;

	private readonly PhantomWalletOptions _phantomWalletOptions = new() { appMetaDataUrl = "https://beamable.com" };

	private Account _account;
	private IAuthService _authService;

	private Account Account
	{
		set
		{
			_account = value;
			Refresh();
		}
	}

	private bool WalletConnected => _account != null;
	private bool WalletAttached { get; set; } = false;
	private bool Working { get; set; } = false;

	public async void Start()
	{
		Refresh();

		_ctx = BeamContext.Default;
		await _ctx.OnReady;

		_authService = _ctx.Api.AuthService;

		_connectWalletButton.onClick.AddListener(OnConnectClicked);
		_attachIdentityButton.onClick.AddListener(OnAttachClicked);
		_detachIdentityButton.onClick.AddListener(OnDetachClicked);
		_getExternalIdentitiesButton.onClick.AddListener(OnGetExternalClicked);
	}

	private void Refresh()
	{
		_connectWalletButton.interactable = !Working && !WalletConnected;
		_attachIdentityButton.interactable = !Working && WalletConnected && !WalletAttached;
		_detachIdentityButton.interactable = !Working && WalletConnected && WalletAttached;
		_getExternalIdentitiesButton.interactable = false;

		_publicKeyGroup.SetActive(WalletConnected);
		_publicKeyValue.text = WalletConnected ? _account.PublicKey.Key : String.Empty;
	}

	private async void OnConnectClicked()
	{
		// Temp
		Working = true;
		Refresh();
		
		Debug.Log("Connecting to a wallet...");
		await Login();
	}

	private async void OnAttachClicked()
	{
		// Temp
		Working = true;
		Refresh();
		
		Debug.Log("Attaching wallet...");
		await SendAttachRequest();
	}

	private async void OnDetachClicked()
	{
		// Temp
		Working = true;
		Refresh();
		
		Debug.Log("Detaching wallet...");
		await SendDetachRequest();
	}

	private async void OnGetExternalClicked()
	{
		// Temp
		Working = true;
		Refresh();
		
		Debug.Log("Gettting external identities info...");
		await GetExternalIdentities();
	}

	private async Promise GetExternalIdentities()
	{
		User user = await _authService.GetUser();
	}

	private async Promise SendAttachRequest(ChallengeSolution challengeSolution = null)
	{
		_attachIdentityButton.interactable = false;

		StringBuilder builder = new StringBuilder();
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

	private async Promise SendDetachRequest()
	{
		await _authService.DetachIdentity(_federation.Service, 0.ToString(), _federation.Namespace)
			.Then(HandleDetachResponse)
			.Error(HandleError);
	}

	private void HandleDetachResponse(DetachExternalIdentityResponse response)
	{
		switch (response.result)
		{
			case "ok":
				Debug.Log("Succesfully detached external identit y");
				break;
			default:
				Debug.Log("Something gone wrong while detaching external identity");
				break;
		}
		
		// Temp
		WalletAttached = false;
		Working = false;
		Refresh();
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
					byte[] challengeTokenBytes = Convert.FromBase64String(challengeToken.challenge);

					Debug.Log("Signing challenge token...");
					byte[] signature = _account.Sign(challengeTokenBytes);
					string signedSignature = Convert.ToBase64String(signature);
					Debug.Log($"Signed signature: {signedSignature}");

					ChallengeSolution solution = new ChallengeSolution
					{
						challenge_token = response.challenge_token, solution = signedSignature
					};

					Debug.Log(
						$"Sending a request with PublicKey: {_account.PublicKey.Key}, " +
						$"ProviderService: {_federation.Service}, ProviderNamespace: {_federation.Namespace}");

					await SendAttachRequest(solution);
				}
				break;
			}
			case "ok":
				Debug.Log("Succesfully attached external identity");
				
				// Temp
				WalletAttached = true;
				Working = false;
				Refresh();
				
				break;
			default:
				Debug.Log("Something gone wrong while attaching external identity");
				
				// Temp
				WalletAttached = false;
				Working = false;
				Refresh();
				
				break;
		}
	}

	private void HandleError(Exception obj)
	{
		Debug.LogError(obj.Message);
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
	}

	private async Task<Account> LoginInGame()
	{
		var inGameWallet = new InGameWallet(RpcCluster.DevNet, null, true);

		var newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
		return await inGameWallet.Login("1234") ?? await inGameWallet.CreateAccount(newMnemonic.ToString(), "1234");
	}

	private async Task<Account> LoginPhantom()
	{
		var phantomWallet = new PhantomWallet(_phantomWalletOptions, RpcCluster.DevNet, string.Empty, true);
		return await phantomWallet.Login();
	}
}