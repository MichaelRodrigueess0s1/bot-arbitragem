using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;


class Program
{
	public static string location = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\";


	static void sleep(int i)
	{
		Logger.log("Wait " + i + "ms...");
		System.Threading.Thread.Sleep(i);
	}

	public static decimal FindDifference(decimal x, decimal y) { return Math.Abs(x - y); }

	static void Main(string[] args)
	{
		Logger.log("Começouuuu...");
		String jsonConfig = System.IO.File.ReadAllText(location + "key.txt");
		JContainer jConfig = (JContainer)JsonConvert.DeserializeObject(jsonConfig, (typeof(JContainer)));

		Key.keyA = jConfig["key"].ToString();
		Key.secretA = jConfig["secret"].ToString();

		decimal BUY = decimal.Parse(jConfig["buy"].ToString(), System.Globalization.NumberStyles.Float);
		decimal SELL = decimal.Parse(jConfig["sell"].ToString(), System.Globalization.NumberStyles.Float);
		//decimal TAXVALUE = decimal.Parse(jConfig["taxValue"].ToString(), System.Globalization.NumberStyles.Float);
		//decimal TAXPERCENT = decimal.Parse("0.001498959", System.Globalization.NumberStyles.Float);
		decimal priceToSell = 0;
		decimal pricetoBuy = 0;
		bool selledLasPrice = true;
		bool buyLastPrice = true;
		decimal percentToNegociate = 0.020m;
		ExchangeBinance binance = new ExchangeBinance();
		while (true)
		{
			try
			{
				String jsonOpenOrders = binance.getAllOrders();
				if (jsonOpenOrders.Length > 10)
				{
					JContainer jContainerOpenOrders = (JContainer)JsonConvert.DeserializeObject(jsonOpenOrders);
					Logger.log(binance.cancelOrder(jContainerOpenOrders[0]["symbol"].ToString(), jContainerOpenOrders[0]["orderId"].ToString()));
				}
				Logger.log("==================================");
				Logger.log("Balances");

				string json = binance.getBalancesJSON();
				decimal BUSDBalance = binance.getBalances("BUSD", json);
				decimal BTCBalance = binance.getBalances("BTC", json);
				//decimal USDC = binance.getBalances("USDC", json);
				//decimal USDS = binance.getBalances("USDS", json);
				//decimal PAX = binance.getBalances("PAX", json);

				Logger.log("Balance TUSD: " + BUSDBalance);
				Logger.log("Balance USDT: " + BTCBalance);
				//Logger.log("USDC: " + USDC);
				//Logger.log("USDS: " + USDS);
				//Logger.log("PAX: " + PAX);
				Logger.log("==================================");

				string cache = Http.get("https://api.binance.com/api/v1/ticker/24hr");

				decimal percentBTC = ExchangeBinance.getPriceChangePercentCache("BTCBUSD", cache);
				decimal BUSDBTC = binance.getLastPriceCACHE("BTCBUSD", cache);
				Logger.log("PERCENT BTC: " + percentBTC);
				Logger.log("BUSDBTC: " + BUSDBTC);


				Logger.log("----------------------------");
				Logger.log("BUY PRICE " + pricetoBuy);
				Logger.log("SELL PRICE: " + priceToSell);
				Logger.log("----------------------------");



				if (buyLastPrice)
				{
					buyLastPrice = false;
					pricetoBuy = BUSDBTC - (BUSDBTC * percentToNegociate);
				}
			
				if (selledLasPrice)
				{
					selledLasPrice = false;
					priceToSell = BUSDBTC + (BUSDBTC * percentToNegociate); ;
				}
			
				string jsonOrder = string.Empty;
				if (BUSDBalance > 10)
				{
					var price = (BUSDBTC + 0.00001m);
					var amount = BUSDBalance / (BUSDBTC);
					if (BUSDBTC <= pricetoBuy)
					{
						Logger.log($"ORDER PRICE: {price}");
						Logger.log($"ORDER QUANTIDY: {amount}");
						jsonOrder = binance.order("buy", "BTCBUSD", amount, price);
						buyLastPrice = true;
					}
				}

				if (BTCBalance > (0.000285m))
				{
					var price = (BUSDBTC);
					var aumont = BTCBalance;

					if (BUSDBTC >= priceToSell && BTCBalance > 0)
					{
						Logger.log($"ORDER PRICE: {price}");
						Logger.log($"ORDER AUMONT: {aumont}");
						jsonOrder = binance.order("sell", "BTCBUSD", aumont, price);
						selledLasPrice = true;
					}
				}


				if (jsonOrder != string.Empty)
				{
					JContainer jContainer = (JContainer)JsonConvert.DeserializeObject(jsonOrder);
					String jsonStatusOrder = binance.getDetailOrder(jContainer["symbol"].ToString(), jContainer["orderId"].ToString());
					for (int i = 0; i < 100; i++)
					{
						jsonStatusOrder = binance.getDetailOrder(jContainer["symbol"].ToString(), jContainer["orderId"].ToString());
						JContainer jContainerOrder = (Newtonsoft.Json.Linq.JContainer)JsonConvert.DeserializeObject(jsonStatusOrder);
						if (jContainerOrder["status"].ToString().Trim().ToUpper() == "FILLED")
							break;
						Console.WriteLine("Wait 3s...");
						sleep(3000);
					}

					binance.cancelOrder(jContainer["symbol"].ToString(), jContainer["orderId"].ToString());
				}
			}
			catch (Exception ex)
			{
				Logger.log("ERROR ::: " + ex.Message + ex.StackTrace);
			}

			Logger.log("====================================================");
			sleep(6000);
		}

	}
}

