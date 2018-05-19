using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.IO;

namespace TradeInfo
{
    class Test
    {

        public static void Main()
        {
            Globals.Coin btc = new Globals.Coin("Bitcoin", "btc");
            Globals.Coin ltc = new Globals.Coin("Litecoin", "ltc");
            Globals.Coin usdt = new Globals.Coin("USDT", "usdt");

            BinanceAPIClient binanceAPI = new BinanceAPIClient
            {
                FiatCoin = usdt,
                ProfitRealizationCoin = btc,
                markets ={
                        new Globals.Coin(Globals.COIN_SYMBOL_TO_NAME[Globals.ltc],Globals.ltc),
                        new Globals.Coin(Globals.COIN_SYMBOL_TO_NAME[Globals.usdt],Globals.usdt),
                        new Globals.Coin(Globals.COIN_SYMBOL_TO_NAME[Globals.btc],Globals.btc),
                    },

                minPRCtradeAmount = 0.002,
                pricePrecision = new CustomDict<string, int>{
                        {string.Format("{0}{1}",Globals.ltc,Globals.btc),6 },
                        {string.Format("{0}{1}",Globals.btc,Globals.usdt),2},
                       },
                amountPrecision = new CustomDict<string, int>{
                        {string.Format("{0}{1}",Globals.ltc,Globals.btc),2 },
                        {string.Format("{0}{1}",Globals.btc,Globals.usdt),6 },
                       },
                minIncreaseForBuying = new CustomDict<string, double>{
                        {string.Format("{0}{1}",Globals.ltc,Globals.btc),0.003 },
                        {string.Format("{0}{1}",Globals.btc,Globals.usdt),0.003 },
                       },
            };

            double ltcToSell = 3.5;
            double ltcToBtc;
            Globals.consoleMsg("<type>i</type>curr. free balance of btc=<type>d</type>" + binanceAPI.GetBalance(btc).free+"\n");
            Globals.consoleMsg("<type>i</type>curr. in-trade balance of btc=<type>d</type>" + binanceAPI.GetBalance(btc).inTrade + "\n");
            Globals.consoleMsg("<type>i</type>curr. trade price of ltc in btc=<type>d</type>" + (ltcToBtc=binanceAPI.GetRatio(ltc.marketSymbol,btc.marketSymbol))+"\n");
            Globals.consoleMsg(string.Format("<type>i</type>selling {0} ltc for {1} btc",ltcToSell,ltcToBtc));
            Transaction t=binanceAPI.PerformTrade(ltc, btc, ltcToSell, ltcToSell * ltcToBtc, 1 / (Math.Pow(10, binanceAPI.amountPrecision[binanceAPI.reformCoinPairLower(ltc.marketSymbol, btc.marketSymbol)] - 1)));
            Globals.consoleMsg("<type>i</type>resulting transaction: <type>d</type>" + t.ToString() + "\n");


        }

    }
}
