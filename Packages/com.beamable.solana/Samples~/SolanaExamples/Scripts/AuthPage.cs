﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beamable;
using Beamable.Api.Auth;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Content;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolanaExamples.Scripts
{
    /// <summary>
    /// A script that presents how to perform basic operations like connecting to a wallet, attach or detach external identity
    /// or sign a message with connected wallet
    /// </summary>
    public class AuthPage : TabPage
    {
        [SerializeField] private Button _connectWalletButton;
        [SerializeField] private Button _attachIdentityButton;
        [SerializeField] private Button _detachIdentityButton;
        [SerializeField] private Button _getExternalIdentitiesButton;
        [SerializeField] private Button _signMessageButton;

        [SerializeField] private TextMeshProUGUI _beamId;
        [SerializeField] private TextMeshProUGUI _walletId;

        private IAuthService _authService;

        private async void Start()
        {
            _connectWalletButton.onClick.AddListener(OnConnectClicked);
            _attachIdentityButton.onClick.AddListener(OnAttachClicked);
            _detachIdentityButton.onClick.AddListener(OnDetachClicked);
            _getExternalIdentitiesButton.onClick.AddListener(OnGetExternalClicked);
            _signMessageButton.onClick.AddListener(OnSignClicked);

            _authService = Ctx.Api.AuthService;

            await BeamContext.Default.OnReady;
            await BeamContext.Default.Accounts.OnReady;

            _beamId.text = $"<b>Beam ID</b> {Ctx.Accounts.Current.GamerTag.ToString()}";
        }

        public override void OnRefresh()
        {
            _connectWalletButton.interactable = !Data.Instance.Working && !Data.Instance.WalletConnected;
            _attachIdentityButton.interactable = !Data.Instance.Working && Data.Instance.WalletConnected &&
                                                 !Data.Instance.WalletAttached;
            _detachIdentityButton.interactable = !Data.Instance.Working && Data.Instance.WalletConnected &&
                                                 Data.Instance.WalletAttached;
            _getExternalIdentitiesButton.interactable = !Data.Instance.Working;
            _signMessageButton.interactable = !Data.Instance.Working && Data.Instance.WalletConnected;

            _walletId.text = Data.Instance.WalletConnected
                ? $"<b>Wallet Id</b> {Data.Instance.Account.PublicKey.Key}"
                : String.Empty;
        }

        private async void OnConnectClicked()
        {
            Data.Instance.Working = true;

            OnLog("Connecting to a wallet...");
            await Login();

            if (Data.Instance.WalletConnected)
            {
                Data.Instance.WalletAttached = await CheckIfWalletHasAttachedIdentity();
            }

            Data.Instance.Working = false;
        }

        private async void OnAttachClicked()
        {
            Data.Instance.Working = true;

            Federation federation = Data.Instance.Federation;

            if (!VerifyFederation())
            {
                return;
            }

            OnLog("Attaching wallet...");
            await SendAttachRequest();
            Data.Instance.WalletAttached = await CheckIfWalletHasAttachedIdentity();

            async Promise SendAttachRequest(ChallengeSolution challengeSolution = null)
            {
                Data.Instance.Working = true;
                StringBuilder builder = new();
                builder.AppendLine("Sending a request with:");
                builder.AppendLine($"Public key: {Data.Instance.Account.PublicKey.Key}");
                builder.AppendLine($"Provider service: {federation.Service}");
                if (challengeSolution != null)
                {
                    builder.AppendLine($"Signed solution: {challengeSolution.solution}");
                }

                OnLog(builder.ToString());

                AttachExternalIdentityResponse response = await Ctx.Api.AuthService.AttachIdentity(
                    Data.Instance.Wallet.Account.PublicKey, federation.Service,
                    federation.Namespace, challengeSolution);

                switch (response.result)
                {
                    case "challenge":
                        if (response.challenge_token == null)
                        {
                            throw new InvalidOperationException(
                                "A challenge was requested, but no challenge handler was provided.");
                        }

                        var solvedChallenge = await SolveChallenge(response.challenge_token);

                        ChallengeSolution solution = new ChallengeSolution
                        {
                            challenge_token = response.challenge_token,
                            solution = solvedChallenge
                        };

                        await SendAttachRequest(solution);
                        break;
                    case "ok":
                        Ctx.Api.AuthService.GetUser();
                        OnLog("Succesfully attached an external identity...");
                        break;
                }

                Data.Instance.Working = false;
            }
        }

        private async void OnDetachClicked()
        {
            Data.Instance.Working = true;
            OnLog("Detaching wallet...");
            
            User user = await Ctx.Api.AuthService.GetUser();

            if (user == null)
                return;

            ExternalIdentity externalIdentity = user.external.Find(identity =>
                identity.providerNamespace == Data.Instance.Federation.Namespace);

            if (externalIdentity != null)
            {
                DetachExternalIdentityResponse response = await Ctx.Api.AuthService.DetachIdentity(
                    externalIdentity.providerService, externalIdentity.userId,
                    externalIdentity.providerNamespace);

                if (response.result == "ok")
                {
                    Data.Instance.WalletAttached = await CheckIfWalletHasAttachedIdentity();

                    if (!Data.Instance.WalletAttached)
                    {
                        OnLog("Succesfully detached an external identity...");
                    }
                }
                else
                {
                    OnLog($"Detaching result: {response.result}");
                }
            }

            Data.Instance.Working = false;
        }

        /// <summary>
        /// Method that shows how to sign a message with connected wallet 
        /// </summary>
        private async void OnSignClicked()
        {
            Data.Instance.Working = true;
            OnLog("Signing a message...");

            string message = "Sample message to sign";

            // Currently connected wallet is responsible for signing passed challenge. InGameWallet (in editor) is 
            // handling this automatically. PhantomWallet (mobile and WebGL) connects either with mobile app or browser
            // extension.
            byte[] signatureBytes = await Data.Instance.Wallet.SignMessage(Encoding.UTF8.GetBytes(message));

            // Signature signed by a wallet should be converted back to Base64String as that's the format that server is
            // waiting for
            string signedSignature = Convert.ToBase64String(signatureBytes);
            OnLog($"Signed signature: {signedSignature}");
            Data.Instance.Working = false;
        }

        /// <summary>
        /// Method that renders currently connected to account external identities where Service is a microservice responsible
        /// for handling custom server side logic, Namespace shows which namespace will be handled (namespaces can be implemented
        /// by deriving IThirdPartyCloudIdentity interface and Public Key is a wallet address that has been connected to an
        /// account
        /// </summary>
        private async void OnGetExternalClicked()
        {
            OnLog("Gettting external identities info...");

            User user = await Ctx.Api.AuthService.GetUser();

            if (user == null)
                return;

            if (user.external.Count != 0)
            {
                StringBuilder builder = new();
                foreach (ExternalIdentity identity in user.external)
                {
                    builder.AppendLine(
                        $"Service: {identity.providerService}, namespace: {identity.providerNamespace}, public key: {identity.userId}");
                }

                OnLog(builder.ToString());
            }
            else
            {
                OnLog("No external identities found...");
            }
        }

        /// <summary>
        /// Method that shows a way to solve a challenge received from a server. It needs to be done to proof that we
        /// are true owners of a wallet. After sending it back to a server it verifies it an decides wheter solution was
        /// correct or not. Challenge token we are receving from server is a three-part, dot separated string and has
        /// following format: {challenge}.{validUntilEpoch}.{signature} where:
        ///		{challenge}			- Base64 encoded string
        ///		{validUntilEpoch}	- valid until epoch time in milliseconds, Int64 value
        ///		{signature}			- Base64 encoded token signature
        /// </summary>
        /// <param name="challengeToken"></param>
        /// <returns></returns>
        private async Promise<string> SolveChallenge(string challengeToken)
        {
            OnLog($"Signing a challenge: {challengeToken}");

            // Parsing received challenge token to a 3 part struct
            ChallengeToken parsedToken = _authService.ParseChallengeToken(challengeToken);

            // Challenge we received to solve is Base64String 
            byte[] challengeBytes = Convert.FromBase64String(parsedToken.challenge);

            // Currently connected wallet is responsible for signing passed challenge. InGameWallet (in editor) is 
            // handling this automatically. PhantomWallet (mobile and WebGL) connects either with mobile app or browser
            // extension.
            byte[] signatureBytes = await Data.Instance.Wallet.SignMessage(challengeBytes);

            // Signature signed by a wallet should be converted back to Base64String as that's the format that server is
            // waiting for
            string signedSignature = Convert.ToBase64String(signatureBytes);

            OnLog($"Signed signature: {signedSignature}");

            return signedSignature;
        }

        private async Promise<bool> CheckIfWalletHasAttachedIdentity()
        {
            User user = await Ctx.Api.AuthService.GetUser();

            if (user == null)
                return false;

            if (user.external.Count == 0)
                return false;

            Federation federation = Data.Instance.Federation;

            if (!VerifyFederation())
            {
                return false;
            }

            ExternalIdentity externalIdentity = user.external.Find(i =>
                i.providerNamespace == federation.Namespace && i.providerService == federation.Service &&
                i.userId == Data.Instance.Account.PublicKey);

            return externalIdentity != null;
        }

        private bool VerifyFederation()
        {
            bool verified = !string.IsNullOrEmpty(Data.Instance.Federation.Service) &&
                            !string.IsNullOrEmpty(Data.Instance.Federation.Namespace);

            if (verified) return true;

            OnLog("You need to set Federation in SolanaAuthExample gameobject...");
            return false;
        }

        public async Task Login()
        {
#if UNITY_EDITOR
            Data.Instance.Account = await LoginInGame();
#elif (UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL)
		Account = await LoginPhantom();
#endif

            OnLog(Data.Instance.Account != null
                ? $"Wallet connected with PublicKey: {Data.Instance.Account.PublicKey.Key}"
                : "Something gone wrong while connecting with a wallet");
        }

        private async Task<Account> LoginInGame()
        {
            // InGameWallet class is used for editor operations. It automatically approves all messages and transactions.
            Data.Instance.Wallet = new InGameWallet(RpcCluster.DevNet, null, true);

            // We are retrieving local wallet or creating a new one if none was found
            return await Data.Instance.Wallet.Login(Data.Instance.WalletPassword) ??
                   await Data.Instance.Wallet.CreateAccount(new Mnemonic(WordList.English, WordCount.Twelve).ToString(),
                       Data.Instance.WalletPassword);
        }

        private async Task<Account> LoginPhantom()
        {
            // PhantomWallet class is used for clients built for Android, iOS and WebGL. It handles communication with
            // Phantom Wallet app installed on mobile device and Phantom Wallet browser extensions installed on desktop.
            Data.Instance.Wallet =
                new PhantomWallet(Data.Instance.WalletOptions, RpcCluster.DevNet, string.Empty, true);
            return await Data.Instance.Wallet.Login();
        }
    }
}