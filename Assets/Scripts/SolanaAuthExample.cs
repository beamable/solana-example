using System;
using System.Text;
using System.Threading.Tasks;
using Beamable;
using Beamable.Api.Auth;
using Beamable.Common.Api.Auth;
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

	[SerializeField] private string _providerNamespace = "Solana";
	[SerializeField] private string _providerService = "SolanaAuthMS";

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

	private bool LoggedIn => _account != null;

	public async void Start()
	{
		Refresh();

		_ctx = BeamContext.Default;
		await _ctx.OnReady;

		_authService = _ctx.Api.AuthService;

		_connectWalletButton.onClick.AddListener(OnConnectClicked);
		_attachIdentityButton.onClick.AddListener(OnAttachClicked);
	}

	private void Refresh()
	{
		_connectWalletButton.interactable = !LoggedIn;
		_attachIdentityButton.interactable = LoggedIn;

		_publicKeyGroup.SetActive(LoggedIn);
		_publicKeyValue.text = LoggedIn ? _account.PublicKey.Key : String.Empty;
	}

	private async void OnConnectClicked()
	{
		Debug.Log("Connecting to a wallet");
		await Login();
	}

	private async void OnAttachClicked()
	{
		await SendAttachRequest();
	}

	private async Task SendAttachRequest(ChallengeSolution challengeSolution = null)
	{
		_attachIdentityButton.interactable = false;

		StringBuilder builder = new StringBuilder();
		builder.AppendLine("Sending a request with:");
		builder.AppendLine($"Public key: {_account.PublicKey.Key}");
		builder.AppendLine($"Provider service: {_providerService}");
		if (challengeSolution != null)
		{
			builder.AppendLine($"Signed solution: {challengeSolution.solution}");
		}

		await _authService
			.AttachIdentity(_account.PublicKey.Key, _providerService, _providerNamespace, challengeSolution)
			.Then(HandleAttachResponse)
			.Error(HandleAttachError);
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
						Debug.Log($"Parsed challenge token: {challengeToken}");

						byte[] challengeTokenBytes = Convert.FromBase64String(challengeToken.challenge);

						Debug.Log("Signing challenge token...");
						byte[] signature = _account.Sign(challengeTokenBytes);
						string signedSignature = Convert.ToBase64String(signature);
						Debug.Log($"Signed signature: {signedSignature}");

						ChallengeSolution solution = new ChallengeSolution {
							challenge_token = response.challenge_token, solution = signedSignature
						};

						Debug.Log(
							$"Sending a request with PublicKey: {_account.PublicKey.Key}, " +
							$"ProviderService: {_providerService}, ProviderNamespace: {_providerNamespace}");

						await SendAttachRequest(solution);

						_attachIdentityButton.interactable = true;
					}

					break;
				}
			case "ok":
				Debug.Log("Succesfully attached external identity");
				break;
			default:
				Debug.Log("Something gone wrong while attaching external identity");
				_attachIdentityButton.interactable = true;
				break;
		}
	}

	private void HandleAttachError(Exception obj)
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