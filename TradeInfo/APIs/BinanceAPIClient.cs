using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Globalization;


namespace TradeInfo
{
    public enum TransactionStatus { PASSED, PENDING, COMPLETE, UNDEFINED, PARTIALLY_FILLED, CANCELLED }
    public enum BalanceType { FREE, IN_TRADE, ALL }
    public enum ExchangeType { Crypto, Fiat }
    public enum PriceDetermineMethod { GLOBAL,ACTUAL}
    public class BinanceAPIClient
    {
        private readonly WebClient Client = new WebClient();
        private readonly string apiKey = "your-public-key";
        private readonly string apiSecret = "your-private-key";
        public string apiUrl = "https://api.binance.com";
        private readonly Encoding U8 = Encoding.UTF8;
        public double minPRCtradeAmount { get; set; } = 0.000001;
        public string Name = "Binance";
        public CustomDict<string, double> minIncreaseForBuying { get; set; }
        public CustomDict<string, int> pricePrecision { get; set; }
        public CustomDict<string, int> amountPrecision { get; set; }
        public List<Globals.Coin> markets = new List<Globals.Coin>();

        public double transactionFee { get; set; } = 0.001;
        public Globals.Coin ProfitRealizationCoin { get; set; }
        public Globals.Coin FiatCoin { get; set; } = new Globals.Coin(Globals.COIN_SYMBOL_TO_NAME[Globals.NA], Globals.NA);
        public string baseGlobalDataUrl = "https://min-api.cryptocompare.com";

        public string getPublicKey()
        {
            return apiKey;
        }

        public string getSecretKey()
        {
            return apiSecret;
        }

        public double GetMinTradeAmount(Globals.Coin from, Globals.Coin to)
        {
            if (from == ProfitRealizationCoin) return minPRCtradeAmount;
            else return minPRCtradeAmount/GetRatio(from.marketSymbol, to.marketSymbol);
        }

        public double GetRatio(string from, string to)
        {
            return GetRatio(from, to, 0);
        }

        public double getPairVolume(Globals.Coin coinFrom,Globals.Coin coinTo)
        {
            string pair = reformCoinPair(coinFrom.marketSymbol, coinTo.marketSymbol);
            string resp=ExchangeAuxilaries.SendWebRequest(
                apiUrl + "/api/v1/ticker/24hr",
                apiSecret,
                "GET",
                new CustomDict<string, string>
                {
                    { "X-MBX-APIKEY",apiKey}
                },
                new CustomDict<string, string>
                {
                    { "symbol",pair.ToUpper()}
                }, null
            );
            if (resp == null) throw new Exception("null response for getPairVolume");
            JObject assets = JObject.Parse(resp);
            return double.Parse(assets["quoteVolume"].ToString());
        }

        public double getGlobalAssetPrice(string _asset,string _convertTo)
        {
            double result = 0.0d;
            bool hasFiat = false;
            string asset=_asset, convertTo=_convertTo;
            if (asset.Equals(convertTo)) return 1.0d;
            if (asset.ToLower().Equals(FiatCoin.marketSymbol))
            {
                asset = _convertTo;
                convertTo = _asset;
                hasFiat = true;
            }
            //Globals.consoleDebugMsg("_asset=" + _asset+ " _convertTo="+ _convertTo+" hasFiat="+hasFiat+ "asset=" + asset + " convertTo="+convertTo);
            string pricesHistoryUrl = string.Format("{0}/data/price",baseGlobalDataUrl);
            if (asset.Equals("bcc")) asset = "bch";
            string responseStr = ExchangeAuxilaries.SendWebRequest(
                    pricesHistoryUrl,
                    null,
                    "GET",
                    new CustomDict<string, string>(),
                    new CustomDict<string, string>()
                    {
                        {"fsym",asset.ToUpper() }
                        ,{"tsyms", convertTo.ToUpper()},
                        { "e",Name}
                    },
                    null
                );
            if (responseStr == null) throw new Exception("null response for getGlobalAssetPrice");
            //Globals.consoleDebugMsg("response="+responseStr);
            if(hasFiat) result = 1 / double.Parse(JObject.Parse(responseStr)[FiatCoin.marketSymbol.ToUpper()].ToString());
            else result = double.Parse(JObject.Parse(responseStr)[ProfitRealizationCoin.marketSymbol.ToUpper()].ToString());
            return result;

        }

        public void testApi()
        {
			/*api tests*/
        }

        public Globals.Balance GetBalance(string coinSymbol)
        {
            return GetBalance(new Globals.Coin(Globals.COIN_SYMBOL_TO_NAME[coinSymbol.ToLower()], coinSymbol));
        }


        public Globals.Balance GetBalance(Globals.Coin coin)
        {
            bool contains = false;
            foreach (Globals.Coin c in markets) { /*Logger.debugMsg(string.Format("comparing {0} {1}", c.ToString(), coin.ToString()));*/ if (c == coin) { contains = true; break; } }
            if (!contains) throw new Exception(string.Format("'{0}' coin not found in exchange",coin.ToString()));
            string parsedCoin;
            JArray arr = _getBalancesJson();
            if (arr == null) throw new Exception("_getBalancesJson() returned null");
            foreach (JObject obj in arr)
            {
                parsedCoin = obj["asset"].ToString();
                if (parsedCoin.Equals(coin.marketSymbol.ToUpper()))
                {

                        return new Globals.Balance(double.Parse(obj["free"].ToString()), double.Parse(obj["locked"].ToString()));
                }
            }
            throw new Exception(string.Format("'{0}' coin not found in query response",coin.marketSymbol));
        }


        public double GetTotBalance(Globals.Coin coinToConvertTo, PriceDetermineMethod pricingMethod)
        {
            double result = 0;
            JToken jtok; 
            double price;
            foreach (Globals.Coin market in markets)
            {
                jtok = _getBalancesJson(market.marketSymbol);
                //Globals.consoleDebugMsg(jtok.ToString());
                price = pricingMethod == PriceDetermineMethod.GLOBAL ? getGlobalAssetPrice(market.marketSymbol, coinToConvertTo.marketSymbol) : GetRatio(market.marketSymbol, coinToConvertTo.marketSymbol);
                result += (double.Parse(jtok["free"].ToString()) + double.Parse(jtok["locked"].ToString())) * price;
            }
            return result;

        }

        private JArray _getBalancesJson()
        {
            string assetsStr = ExchangeAuxilaries.SendWebRequest(
                apiUrl + "/api/v3/account",
                apiSecret,
                "GET",
                new CustomDict<string, string>()
                {
                    {"X-MBX-APIKEY", apiKey }
                },
                new CustomDict<string, string>()
                {
                    {"timestamp",ExchangeAuxilaries.timestamp },
                    { "recvWindow","10000000"}
                },
                "signature"
                );
            if (assetsStr == null) return null;
            JObject assets = JObject.Parse(assetsStr);
            return JArray.Parse(assets["balances"].ToString());
        }

        private JToken _getBalancesJson(string coinName)
        {
            string assetsStr = ExchangeAuxilaries.SendWebRequest(
                apiUrl + "/api/v3/account",
                apiSecret,
                "GET",
                new CustomDict<string, string>()
                {
                    {"X-MBX-APIKEY", apiKey }
                },
                new CustomDict<string, string>()
                {
                    {"timestamp",ExchangeAuxilaries.timestamp },
                    { "recvWindow","10000000"}
                },
                "signature"
                );
            if (assetsStr == null) return null;
            JObject assets = JObject.Parse(assetsStr);
            return JArray.Parse(assets["balances"].ToString()).Where(x=>x["asset"].ToString().ToUpper().Equals(coinName.ToUpper())).First();
        }

        private double GetRatio(string from, string to, int attempts)
        {
            try
            {
                if (from.Equals(ProfitRealizationCoin.marketSymbol) && to.Equals(ProfitRealizationCoin.marketSymbol)) return 1.0d;
                BuyOrder order = GetBestTradeOrder(from, to);
                return order.price;
            }
            catch (System.Net.WebException e)
            {
                Logger.debugMsg("web error: \n" + Name + "\n" + e.Message + "\n" + e.StackTrace);
                if (attempts > Globals.HTTP_ATTEMPTS)
                {
                    throw new Exception("GetRatio() out of attempts");
                }
                System.Threading.Thread.Sleep(1000 * Globals.HTTP_MAX_RESPONSE_TIME);
                return (GetRatio(from, to, attempts + 1));
            }

        }

        public string reformCoinPairLower(string c1, string c2)
        {
            return reformCoinPair(c1, c2).ToLower();
        }

        public string reformCoinPair(string sellCoin, string buyCoin)
        {
            if(sellCoin.Equals(FiatCoin.marketSymbol, StringComparison.OrdinalIgnoreCase) || buyCoin.Equals(FiatCoin.marketSymbol, StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(buyCoin.Equals(FiatCoin.marketSymbol, StringComparison.OrdinalIgnoreCase) ? "{0}{1}" : "{1}{0}", sellCoin.ToUpper(), buyCoin.ToUpper());
            }
            else
                return string.Format(buyCoin.Equals(ProfitRealizationCoin.marketSymbol,StringComparison.OrdinalIgnoreCase) ? "{0}{1}" : "{1}{0}", sellCoin.ToUpper(), buyCoin.ToUpper());
        }

        public List<Transaction> getAllOpenOrders()
        {
            List<Transaction> result = new List<Transaction>();
            string assetsStr = ExchangeAuxilaries.SendWebRequest(
                apiUrl + "/api/v3/openOrders",
                apiSecret,
                "GET",
                new CustomDict<string, string>()
                {
                    {"X-MBX-APIKEY", apiKey }
                },
                new CustomDict<string, string>()
                {
                    {"timestamp",ExchangeAuxilaries.timestamp },
                    { "recvWindow","10000000"}
                },
                "signature"
                );

            if (assetsStr == null)
            {
                Logger.debugMsg("failed getting assetsStr in getAllOpenOrders()");
                return null;
            }
            JArray jarr=JArray.Parse(assetsStr);

            foreach (JObject jobj in jarr)
            {
                TradeType orderType = jobj["side"].ToString().Equals("BUY") ? TradeType.BUY : TradeType.SELL;
                string from = orderType == TradeType.SELL ? jobj["symbol"].ToString().Substring(0, 3).ToLower() : jobj["symbol"].ToString().Substring(3).ToLower();
                string to = orderType == TradeType.BUY ? jobj["symbol"].ToString().Substring(0, 3).ToLower() : jobj["symbol"].ToString().Substring(3).ToLower();
                double origQty = double.Parse(jobj["origQty"].ToString());
                double price= double.Parse(jobj["price"].ToString());
                result.Add(TransactionGenerator.NewTransaction(
                    uint.Parse(jobj["orderId"].ToString()),
                    from, to,
                    orderType == TradeType.BUY ? origQty * price : origQty,
                    orderType == TradeType.SELL ? origQty * price : origQty
                    ));
            }
            return result;
        }

        public Globals.Coin[] getCoinsInExchangeOrder(Transaction t)
        {
            Globals.Coin[] result = new Globals.Coin[2];

            if (t.coinBought == FiatCoin) { result[0] = t.coinSold; result[1] = t.coinBought; }
            else if (t.coinSold == FiatCoin) {result[0] = t.coinBought; result[1] = t.coinSold; }
            else if (t.coinBought == ProfitRealizationCoin) { result[0] = t.coinSold; result[1] = t.coinBought; }
            else { result[0] = t.coinBought; result[1] = t.coinSold; }
            return result;
        }

        public BuyOrder GetBestTradeOrder(string from, string to)
        {

            string url = apiUrl + "/api/v3/ticker/bookTicker";

            double sellPrice, buyPrice, buyQty,dealPrice;

            
            string resp1 = ExchangeAuxilaries.SendWebRequest(
                        url,
                        apiKey,
                        "GET",
                        new CustomDict<string, string>{
                                {"X-MBX-APIKEY", apiKey }
                        },
                        new CustomDict<string, string>{
                                {"symbol", reformCoinPair(from,to) }
                        },
                        null
                    );

            JObject jResult1 = JObject.Parse(resp1);

            if (from.Equals(FiatCoin.marketSymbol) || to.Equals(FiatCoin.marketSymbol))
            {
                sellPrice = from.Equals(FiatCoin.marketSymbol) ? 1.0f : double.Parse(jResult1["bidPrice"].ToString());
                buyPrice = to.Equals(FiatCoin.marketSymbol) ? 1.0f : double.Parse(jResult1["askPrice"].ToString());
                dealPrice = sellPrice / buyPrice;
                buyQty = to.Equals(FiatCoin.marketSymbol) ? double.Parse(jResult1["bidQty"].ToString()) * dealPrice : double.Parse(jResult1["askQty"].ToString());
            }
            else
            {
                sellPrice = from.Equals(ProfitRealizationCoin.marketSymbol) ? 1.0f : double.Parse(jResult1["bidPrice"].ToString());
                buyPrice = to.Equals(ProfitRealizationCoin.marketSymbol) ? 1.0f : double.Parse(jResult1["askPrice"].ToString());
                dealPrice = sellPrice / buyPrice;
                buyQty = to.Equals(ProfitRealizationCoin.marketSymbol) ? double.Parse(jResult1["bidQty"].ToString()) * dealPrice : double.Parse(jResult1["askQty"].ToString());
            }
            return new BuyOrder(dealPrice, buyQty);

        }

        public TransactionStatus TradeStatus(Transaction t) {
             return TradeStatus(t.coinSold.marketSymbol, t.coinBought.marketSymbol, t.id);
        }

        public double getTradeStatusData(Transaction t,string param)
        {
            return getTradeStatusData(t.coinSold.marketSymbol, t.coinBought.marketSymbol, t.id,param);
        }

        public double getTradeStatusData(string coinSold, string coinBought, uint id,string param)
        {
            string symbolBought = coinBought.ToUpper();
            string symbolSold = coinSold.ToUpper();
            string pair = coinSold == ProfitRealizationCoin.marketSymbol ? string.Format("{0}{1}", symbolBought, symbolSold) :
                string.Format("{1}{0}", symbolBought, symbolSold);

            string resStr = ExchangeAuxilaries.SendWebRequest(
                apiUrl + "/api/v3/order",
                apiSecret,
                "GET",
                new CustomDict<string, string>()
                {
                    {"X-MBX-APIKEY", apiKey }
                },
                new CustomDict<string, string>()
                {
                    {"symbol",pair },
                    {"orderId",id.ToString() },
                    {"recvWindow", "10000000"},
                    {"timestamp",ExchangeAuxilaries.timestamp }
                },
                "signature"
                );

            if (resStr == null) throw new Exception("resStr==null at getTradeExecutedQty");
            JObject jResult = JObject.Parse(resStr);
            return double.Parse(jResult[param].Value<string>());
        }

        public double getTradePrice(Transaction trade)
        {
            string symbolBought = trade.coinBought.marketSymbol.ToUpper();
            string symbolSold = trade.coinSold.marketSymbol.ToUpper();
            string pair = reformCoinPair(trade.coinBought.marketSymbol, trade.coinSold.marketSymbol);

            string resStr = ExchangeAuxilaries.SendWebRequest(
                apiUrl + "/api/v3/order",
                apiSecret,
                "GET",
                new CustomDict<string, string>()
                {
                    {"X-MBX-APIKEY", apiKey }
                },
                new CustomDict<string, string>()
                {
                    {"symbol",pair.ToUpper() },
                    {"orderId",trade.id.ToString() },
                    {"recvWindow", "10000000"},
                    {"timestamp",ExchangeAuxilaries.timestamp }
                },
                "signature"
                );
            double result= double.Parse(JObject.Parse(resStr)["price"].ToString());
            if (trade.coinBought == FiatCoin || trade.coinSold == FiatCoin) result = 1 / result;
            return result;

        }

        public TransactionStatus TradeStatus(string coinSold,string coinBought,uint id)
        {
            string symbolBought = coinBought.ToUpper();
            string symbolSold = coinSold.ToUpper();
            string pair = reformCoinPair(coinSold, coinBought);

            string resStr = ExchangeAuxilaries.SendWebRequest(
                apiUrl+ "/api/v3/order",
                apiSecret,
                "GET",
                new CustomDict<string, string>()
                {
                    {"X-MBX-APIKEY", apiKey }
                },
                new CustomDict<string, string>()
                {
                    {"symbol",pair },
                    {"orderId",id.ToString() },
                    {"recvWindow", "10000000"},
                    {"timestamp",ExchangeAuxilaries.timestamp }
                },
                "signature"
                );

            if (resStr == null) return TransactionStatus.UNDEFINED;
            JObject jResult = JObject.Parse(resStr);
            switch (jResult["status"].Value<string>()) {
                case "NEW":
                    return TransactionStatus.PENDING;
                case "FILLED":
                    return TransactionStatus.COMPLETE;
                case "PARTIALLY_FILLED":
                    return TransactionStatus.PARTIALLY_FILLED;
                case "CANCELED":
                    return TransactionStatus.CANCELLED;
                default:
                    Logger.debugMsg("unrecognized status : "+ jResult["status"].Value<string>(),Name);
                    return TransactionStatus.UNDEFINED;
            }
        }


        public bool CancelTrade(Transaction t)
        {
            return CancelTrade(t.coinSold.marketSymbol, t.coinBought.marketSymbol, t.id);
        }

        public bool CancelTrade(string coinSold,string coinBought,uint id)
        {
            string symbolBought = coinBought.ToUpper();
            string symbolSold = coinSold.ToUpper();
            string pair = reformCoinPair(coinBought, coinSold);

            string resStr = ExchangeAuxilaries.SendWebRequest(
                apiUrl+ "/api/v3/order",
                apiSecret,
                "DELETE",
                new CustomDict<string, string>()
                {
                    {"X-MBX-APIKEY", apiKey }
                },
                new CustomDict<string, string>()
                {
                    {"symbol",pair },
                    {"orderId",id.ToString() },
                    { "recvWindow","10000000"},
                    {"timestamp",ExchangeAuxilaries.timestamp }
                },
                "signature"
                );
            return (resStr != null && !resStr.Contains("error"));
        }

        private string _SendTradeRequest(string currencyPair,string tradeAction,string qty,string price) {
            return
                ExchangeAuxilaries.SendWebRequest(
                    apiUrl + "/api/v3/order",
                    apiSecret,
                    "POST",
                    new CustomDict<string, string>()
                    {
                        {"X-MBX-APIKEY",apiKey }
                    },
                    new CustomDict<string, string>(){
                        {"newOrderRespType","RESULT" },
                        { "symbol",currencyPair},
                        {"side", tradeAction},
                        { "type","LIMIT"},
                        {"timeInForce","GTC"},
                        { "quantity", qty},//Math.Round(otherAmount,amountPrecision[currencyPair.ToLower()]).ToString()},
                        { "price",price },//Math.Round(((decimal)(btcAmount / otherAmount)),pricePrecision[currencyPair.ToLower()]).ToString()},
                        { "recvWindow","10000000"},
                        { "timestamp",ExchangeAuxilaries.timestamp}
                    }, "signature"
                );
        }

        public Transaction PerformTrade(Globals.Coin from, Globals.Coin to, double _amountToSell, double _amountToBuy,double maxAmountCorrection)
        {
            string method = "POST";
            bool toPRC = to==ProfitRealizationCoin; // the api only accepts {COIN}BTC pair name or {COIN}USDT
            bool hasFiat = (from==FiatCoin || to==FiatCoin);
            bool toFiat= to==FiatCoin;
            Transaction result = null;

            string tradeAction;

            if ((!hasFiat) && toPRC) tradeAction = "SELL";
            else if (hasFiat && toFiat) tradeAction = "SELL";
            else tradeAction = "BUY";

            string currencyPair = reformCoinPair(from.marketSymbol, to.marketSymbol);

            double mainCurrencyAmount;
            double otherAmount;

            if (!hasFiat) otherAmount = toPRC ? _amountToSell : _amountToBuy;
            else otherAmount = toFiat ? _amountToSell : _amountToBuy;

            mainCurrencyAmount = (otherAmount == _amountToSell) ? _amountToBuy : _amountToSell;

            decimal price=(decimal)(mainCurrencyAmount / otherAmount);

            string resString = _PerformTrade(currencyPair, tradeAction, otherAmount, price, maxAmountCorrection, 1/Math.Pow(10,amountPrecision[currencyPair.ToLower()]),pricePrecision[currencyPair.ToLower()], 0, 50);


            if (resString == null) return null;


            JObject jObj = JObject.Parse(resString);
            string pendingID = jObj["orderId"].ToString();
            double executedQty= double.Parse(jObj["executedQty"].ToString());
            double origQty = double.Parse(jObj["origQty"].ToString()),
                roundedTot = Math.Round(
                    origQty * (double)price,
                    pricePrecision[reformCoinPairLower(from.marketSymbol, to.marketSymbol)]);

            double tradeQtyFrom,tradeQtyTo;

            if (!hasFiat) {
                tradeQtyFrom = toPRC ? origQty : roundedTot;
                tradeQtyTo = toPRC ? roundedTot : origQty;
            }
            else {
                tradeQtyFrom = toFiat ? origQty : roundedTot;
                tradeQtyTo = toFiat ? roundedTot : origQty;
            }

            result = new Transaction(
                uint.Parse(pendingID),
                //TransactionGenerator.CreateNewTransactionID(),
                DateTime.ParseExact(DateTime.Now.ToString("MM/dd/yyyy-HH:mm:ss"), "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture),
                from,
                tradeQtyFrom,
                to,
                tradeQtyTo
                );

           // Logger.debugMsg("result:\n" + resString, Name);

            return result;
        }

        public struct BuyOrder
        {
            public double price, amount;
            public BuyOrder(double price, double amount) { this.price = price; this.amount = amount; }
        }

        //auxillary method to try and solve common issues with the order if exist
        private string _PerformTrade(string currencyPair,string tradeAction,double otherAmount,decimal price, double maxAmountCorrection,double currCorrection,int newPricePrecision, int tradeAttempt, int maxTradeAttempts)
        {
            //ExchangeDebugMsg(string.Format("running _PerformTrade(currencyPair={0},tradeAction={1},otherAmount={2},price={3},maxAmountCorrection={4},currCorrection={5},tradeAttempt={6},maxTradeAttempts={7}", currencyPair, tradeAction, otherAmount, price, maxAmountCorrection, currCorrection,tradeAttempt, maxTradeAttempts));
            if (tradeAttempt >= maxTradeAttempts) return null;

            string resString = _SendTradeRequest(currencyPair, tradeAction, Math.Round(otherAmount, amountPrecision[currencyPair.ToLower()]).ToString()
                , Math.Round(price, newPricePrecision).ToString());

            bool hasErrors = resString.Contains("\"code\":");

            if (!hasErrors) return resString;

            while (resString.Contains("Account has insufficient balance for requested action") && currCorrection < maxAmountCorrection)
                {
                    ExchangeDebugMsg("correcting : correction=" + currCorrection + " amount of " + currencyPair + " to " + tradeAction + " is " + otherAmount+" corrected amount : "+ (otherAmount - currCorrection).ToString());
                double newAmount = otherAmount - currCorrection;
                    return _PerformTrade(currencyPair, tradeAction, Math.Round(newAmount, amountPrecision[currencyPair.ToLower()])
                        , price,maxAmountCorrection,currCorrection+1/Math.Pow(10,amountPrecision[currencyPair.ToLower()]), newPricePrecision,tradeAttempt + 1,maxTradeAttempts);
                }

            while (resString.Contains("Filter failure: PRICE_FILTER") && newPricePrecision>0)
            {
                ExchangeDebugMsg("encountered price filter, rounding to precision "+ pricePrecision[currencyPair.ToLower()] + "-1");
                return _PerformTrade(currencyPair, tradeAction, Math.Round(otherAmount , amountPrecision[currencyPair.ToLower()])
                        , price, maxAmountCorrection, currCorrection, newPricePrecision - 1, tradeAttempt + 1, maxTradeAttempts);
            }


            return null;
        }

        protected void ExchangeDebugMsg(string msg) { Logger.debugMsg(msg, Name); }

    }
}
