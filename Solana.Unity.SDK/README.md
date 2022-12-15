<div align="center">

  <a href="https://solana.unity-sdk.gg/"><img height="170x" src="https://i.imgur.com/UvulxS0.png" /></a>
  

  <h1>Solana.Unity SDK</h1>

  <p>
    <strong>Solana.Unity integration Framework</strong>
  </p>

  <p>
    <a href="https://developers.garbles.fun"><img alt="Tutorials" src="https://img.shields.io/badge/docs-tutorials-blueviolet" /></a>
    <a href="https://github.com/garbles-labs/Solana.Unity-SDK/issues"><img alt="Issues" src="https://img.shields.io/github/issues/garbles-labs/Solana.Unity-SDK?color=blueviolet" /></a>
    <a href="https://discord.com/invite/MBkdC3gxcv"><img alt="Discord Chat" src="https://img.shields.io/discord/943797222162726962?color=blueviolet" /></a>
    <a href="https://opensource.org/licenses/MIT"><img alt="License" src="https://img.shields.io/github/license/garbles-labs/Solana.Unity-SDK?color=blueviolet" /></a>
  </p>
</div>
  
Solana.Unity-SDK is the interface to access [Solana.Unity-Core](https://github.com/garbles-labs/Solana.Unity-Core), Solnet implementation in .NET Standard 2.0 (Unity compatible).
The SDK support an In-game wallet with Web3auth authentication, phantom and SMS (upcoming). A set of convenience methods allows Unity developers to access all the methods implemented in Solana.Unity-Core, inluding MPL, native dex transations and more...

Solana.Unity-SDK started as a fork of [unity-solana-wallet](https://github.com/allartprotocol/unity-solana-wallet), but it has been detached due to the several changes we have made and upcoming pipeline of integrations. 

## 📝 [Documentation](http://developers.garbles.fun/)

## 🚀 [Live Demo](https://garbles-labs.github.io/Solana.Unity-SDK/)

## ✨ Features
- Full JSON RPC API coverage
- Wallet and accounts: Set up of a non-custodial Solana wallet in Unity (sollet and solana-keygen compatible)
- Phantom and Web3auth support (non-custodial signup/login through social accounts)
- Transaction decoding from base64 and wire format and encoding back into wire format
- Message decoding from base64 and wire format and encoding back into wire format
- Instruction decompilation 
- TokenWallet object to send and receive SPL tokens and JIT provisioning of Associated Token Accounts 
- Basic UI examples 
- NFTs

## 🚩 Upcoming
- Native DEX operations (Orca coming soon...)
- Raindrops integration, see the [DAO proposal](https://app.realms.today/dao/DTP/proposal/AyEMvQTicTBZJjfVkrhMRYTGEWczwHrdXpPuV74VpRt9) 
- Backpack
- Wallet adapter and support for SMS (Solana Mobile Stack)
- Methods to trigger / register custom events to easily integrate custom logics (e.g.: server checks/updates or caching)

## 📌 Dependencies
- Solana.Unity.Wallet
- Solana.Unity.Rpc
- Soalana.Unity.KeyStore
- Soalana.Unity.Programs
- Newtonsoft.Json
- Chaos.NaCl.Standard
- Portable.BouncyCastle
- Zxing

## ➕ Installation

* Open [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html) window.
* Click the add **+** button in the status bar.
* The options for adding packages appear.
* Select Add package from git URL from the add menu. A text box and an Add button appear.
* Enter the `https://github.com/garbles-labs/Solana.Unity-SDK.git` Git URL in the text box and click Add.
* Once the package is installed, in the Package Manager inspector you will have Samples. Click on Import
* You may also install a specific package version by using the URL with the specified version.
  * `https://github.com/garbles-labs/Solana.Unity-SDK.git#X.Y.X`
  * Please note that the version `X.Y.Z` stated here is to be replaced with the version you would like to get.
  * You can find all the available releases [here](https://github.com/garbles-labs/Solana.Unity-SDK/releases).
  * The latest available release version is [![Last Release](https://img.shields.io/github/v/release/garbles-labs/Solana.Unity-SDK)](https://github.com/Sgarbles-labs/Solana.Unity-SDK/releases/latest)
* You will find a sample scene with a configured wallet in `Samples/Solana SDK/0.0.1/Simple Wallet/Solana Wallet/1.0.0/Simple Wallet/scenes/wallet_scene.unity`

## 👷 Step-by-step instructions
1. If you have an older version of Unity that doesn't have imported Newtonsoft.Json just import it.
2. Create a new scene.
3. Import WalletController prefab into your scene.
4. Set RPC Cluster (Mainnet/Testnet/Devnet/Custom uri) on SimpleWallet script in WalletController prefab.
5. If you use custom URI be careful to use WS/WSS instead of HTTP/HTTPS because WebSocket does not work with HTTP / HTTPS.
6. Create new Canvas
7. Import WalletHolder prefab into the Canvas or if you want your design just import wallet prefab and customize the scene.


## 💚 Open Source
Open Source is at the heart of what we do at Garbles Labs. We believe building software in the open, with thriving communities, helps leave the world a little better than we found it.

## ✨ Contributors & Community

Thanks go to these wonderful people:

<a href="https://github.com/garbles-labs/Solana.Unity-SDK/graphs/contributors"><img width="100%" src="https://garbles-labs.github.io/Solana.Unity-SDK/metrics.repository.svg"></a>

<a href="https://github.com/garbles-labs/Solana.Unity-SDK/stargazers"><img width="100%" src="https://garbles-labs.github.io/Solana.Unity-SDK/people.repository.svg"></a>
