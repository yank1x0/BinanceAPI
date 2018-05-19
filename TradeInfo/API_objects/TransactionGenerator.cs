using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Globalization;

namespace TradeInfo
{
    public class TransactionGenerator
    {

        public static UInt32 CreateNewTransactionID()
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            var byteArray = new byte[4];
            provider.GetBytes(byteArray);
            return BitConverter.ToUInt32(byteArray, 0);
        }

        public static DateTime GetCurrentDate()
        {
            return DateTime.ParseExact(DateTime.Now.ToString(), "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture);
        }

        public static Transaction ParseTransaction(string transactionStr)
        {
            try
            {
                string[] split = transactionStr.Split();
                Transaction t = new Transaction(
                        UInt32.Parse(split[0]),
                        DateTime.ParseExact(split[1], "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture),
                        new Globals.Coin(Globals.COIN_SYMBOL_TO_NAME[split[2]], split[2]),
                        double.Parse(split[3]),
                        new Globals.Coin(Globals.COIN_SYMBOL_TO_NAME[split[4]], split[4]),
                        double.Parse(split[5])
                    );
                return t;
            }
            catch (Exception e)
            {
                Globals.consoleMsg("<type>e</type>" + e.Message + "\n" + e.StackTrace);
                Logger.debugMsg(e.Message + "\n" + e.StackTrace);
                return null;
            }

        }

        public static Transaction NewTransaction(string from, string to, double amountFrom, double amountTo)
        {
            return new Transaction(CreateNewTransactionID(), DateTime.Now,
                new Globals.Coin(Globals.COIN_SYMBOL_TO_NAME[from], from),
                amountFrom,
                new Globals.Coin(Globals.COIN_SYMBOL_TO_NAME[to], to),
                amountTo);
        }

        public static Transaction NewTransaction(UInt32 id, string from, string to, double amountFrom, double amountTo)
        {
            return new Transaction(id, DateTime.Now,
                new Globals.Coin(Globals.COIN_SYMBOL_TO_NAME[from], from),
                amountFrom,
                new Globals.Coin(Globals.COIN_SYMBOL_TO_NAME[to], to),
                amountTo);

        }

    }
}
