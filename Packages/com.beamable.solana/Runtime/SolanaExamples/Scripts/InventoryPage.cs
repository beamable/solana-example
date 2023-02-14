using System.Collections.Generic;
using System.Text;
using Beamable.Common.Api.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace SolanaExamples.Scripts
{
	/// <summary>
	/// A script that presents how to perform operations related to federated inventory items
	/// </summary>
	public class InventoryPage : TabPage
	{
		[SerializeField] private Button _walletExplorerButton;
		[SerializeField] private Button _getInventoryButton;

		private void Start()
		{
			_walletExplorerButton.onClick.AddListener(OnWalletExplorerClicked);
			_getInventoryButton.onClick.AddListener(OnGetInventoryClicked);
		}

		public override void OnRefresh()
		{
			_walletExplorerButton.interactable = Data.Instance.WalletConnected;
			_getInventoryButton.interactable = !Data.Instance.Working;
		}

		private void OnWalletExplorerClicked()
		{
			var address =
				$"https://explorer.solana.com/address/{Data.Instance.Account.PublicKey}?cluster=devnet";

			Application.OpenURL(address);
		}

		private async void OnGetInventoryClicked()
		{
			Data.Instance.Working = true;
			
			InventoryView view = await Ctx.Api.InventoryService.GetCurrent();

			ParseCurrencies(view.currencies);
			ParseItems(view.items);

			void ParseCurrencies(Dictionary<string, long> currencies)
			{
				StringBuilder builder = new();
				foreach (var (currency, amount) in currencies)
				{
					builder.AppendLine($"Currency: {currency}, amount: {amount}");
				}

				OnLog.Invoke(builder.ToString());
			}

			Data.Instance.Working = false;

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

					OnLog.Invoke(builder.ToString());
				}
			}
		}
	}
}