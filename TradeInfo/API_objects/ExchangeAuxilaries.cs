using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TradeInfo
{
    public enum TradeType { BUY,SELL}
    public class ExchangeAuxilaries
    {
        private static object _requestLock=new object();
        private static TimeSpan _serverOffset= TimeSpan.FromMilliseconds(1000);
        //private   TimeSpan _timestampOffset = TimeSpan.FromMilliseconds(7200000)-_serverOffset;
        private static TimeSpan _timestampOffset = TimeSpan.FromMilliseconds(-1000);
        public static string timestamp { get { return ConvertToUnixTime(DateTime.UtcNow.AddMilliseconds(_timestampOffset.TotalMilliseconds)).ToString(); } }
        private static readonly int[] ERRORS_TO_RETURN_ON = new int[] { -2010,-1013 };

        public static long ConvertToUnixTime(DateTime datetime)
        {
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return (long)(datetime - sTime).TotalMilliseconds;
        }

        public static int ConvertToUnixTimeSecs(DateTime datetime)
        {
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return (int)(datetime - sTime).TotalSeconds;
        }

        public static string pricesToString(CustomDict<string, double> prices, Globals.Coin displayPricesIn)
        {
            string result = string.Empty;
            foreach (KeyValuePair<string, double> pair in prices) { result += pair.Key + "=" + pair.Value + " " + displayPricesIn.marketSymbol + "\n"; }
            return result;
        }

        public static Transaction reverseTransaction(Transaction origTrans, CustomDict<string, double> initialPrices)
        {
            return reverseTransaction(origTrans, initialPrices, -1.0d);
        }

        public static Transaction reverseTransaction(Transaction origTrans, CustomDict<string, double> initialPrices,double newToAmount)
        {
            double _newToAmount = newToAmount;
            if (_newToAmount < 0) _newToAmount = origTrans.amountSold;
           return TransactionGenerator.NewTransaction(
                            origTrans.coinSold.marketSymbol,
                            origTrans.coinBought.marketSymbol,
                            newToAmount *ConvertPrices(initialPrices, origTrans.coinSold.marketSymbol)[origTrans.coinBought.marketSymbol],
                            newToAmount);

        }


        public static string GetSign(string message, string secret)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var hash = new HMACSHA256(keyBytes);
            var computedHash = hash.ComputeHash(messageBytes);
            return BitConverter.ToString(computedHash).Replace("-", "").ToLower();
        }

        public static TradeType getTransType(Exchange ex,Transaction t) { return t.coinBought == ex.ProfitRealizationCoin ? TradeType.SELL : TradeType.BUY; }

        public static string SendWebRequest(string url, string privateKey, string method, CustomDict<string, string> headers, CustomDict<string, string> parameters, string signatureParamName)
        {
            lock (_requestLock)
            {
                return _SendWebRequest(url, privateKey, method, headers, parameters, signatureParamName, 0);
            }
        }

		public static string ComputeHash(string secret, string message)
        {
            var key = Encoding.UTF8.GetBytes(secret.ToUpper());
            string hashString;

            using (var hmac = new HMACSHA256(key))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                hashString = Convert.ToBase64String(hash);
            }

            return hashString;
        }

        private static string _SendWebRequest(string url, string privateKey,string method, CustomDict<string, string> headers, CustomDict<string, string> parameters,string signatureParamName,int attemptNo)
        {
            
            string paramsStr = string.Empty;
            string finalUrl = url;
            string headersStr = "headers:\n";

            if (parameters != null && parameters.Count > 0)
            {
                foreach (KeyValuePair<string, string> p in parameters)
                {
                    paramsStr += paramsStr.Equals(string.Empty) ? string.Format("{0}={1}", p.Key, p.Value) : string.Format("&{0}={1}", p.Key, p.Value);
                }
                if (signatureParamName != null)
                {
                    paramsStr += "&signature=" + GetSign(paramsStr, privateKey);
                }
                finalUrl += "?" + paramsStr;
            }

            var request = WebRequest.Create(new Uri(finalUrl)) as HttpWebRequest;
            if (request == null)
                throw new Exception("Non HTTP WebRequest");

            request.Method = method;
            request.Timeout = 15000;
            // request.ContentType = "application/x-www-form-urlencoded";
            request.ContentType = "application/json; charset=utf-8";

            foreach (KeyValuePair<string, string> h in headers)
            {
                headersStr += h.Key + ":" + h.Value + "\n";
                request.Headers.Add(h.Key, h.Value);
            }

            try
            {
                //Logger.debugMsg("\nSENDING WEB REQUEST [attemp no. "+attemptNo+"/"+Globals.WEBREQUEST_MAX_ATTEMPTS+"] with following data:\n" +
                //    headersStr + "\nrequest body:\n" + paramsStr + "\nto URL:\n" + finalUrl+"...");
                var response = request.GetResponse();
                var resStream = response.GetResponseStream();
                var resStreamReader = new StreamReader(resStream);
                var resString = resStreamReader.ReadToEnd();
                 //Logger.debugMsg("succeeded.");
                return resString;
            }
            catch (WebException ex)
            {
                Logger.debugMsg("caught web exception");
                var messageFromServer = "didn't get response";
                var response = ex.Response;
               // bool stop = false;
                if (response != null)
                {
                    var respStream=response.GetResponseStream();
                    var respStr = new StreamReader(respStream).ReadToEnd();

                    //dynamic obj = JsonConvert.DeserializeObject(resp);
                    messageFromServer = respStr.ToString();
                }
               // if (!messageFromServer.Contains("Unknown")) stop = true;
                Logger.debugMsg("\nWEB REQUEST [attemp no. " + attemptNo + "/" + Globals.WEBREQUEST_MAX_ATTEMPTS + "] with following data:\n" +
                    headersStr + "\nrequest body:\n" + paramsStr + "\nto URL:\n" + finalUrl + "\n" +
                    "\nRESPONSE:\n" + messageFromServer + "\n" +
                    "failed with following exception:\n\n" +
                ex.GetType().FullName + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Data + "\n" + ex.GetBaseException().ToString());

                bool stopOnError = false;

                foreach (int errCode in ERRORS_TO_RETURN_ON) {
                    //Logger.debugMsg("checking error code " + errCode + " in " + messageFromServer);
                    if (messageFromServer.Contains(errCode.ToString()))
                    {
                        stopOnError = true;
                        break;
                    }
                }

                if (!stopOnError && attemptNo < Globals.WEBREQUEST_MAX_ATTEMPTS )
                {
                    Thread.Sleep(Globals.MINS_BETWEEN_ATTEMPTS * 1000);
                    return _SendWebRequest(url, privateKey, method, headers, parameters, signatureParamName, attemptNo + 1);
                }
                return messageFromServer;
            }
            catch (Exception e)
            {
                Logger.debugMsg("\nWEB REQUEST attempt no. " + attemptNo + "/" + Globals.WEBREQUEST_MAX_ATTEMPTS + " with following data:\n" +
                    headersStr + "\nrequest body:\n" + paramsStr + "\nto URL:\n" + finalUrl + "\n" +
                    "failed with following exception:\n\n" +
                e.GetType().FullName + "\n" + e.Message + "\n" + e.StackTrace + "\n" + e.Data + "\n" + e.GetBaseException().ToString());
                if (attemptNo < Globals.WEBREQUEST_MAX_ATTEMPTS)
                {
                    Thread.Sleep(Globals.MINS_BETWEEN_ATTEMPTS * 1000);
                    return _SendWebRequest(url, privateKey, method, headers, parameters, signatureParamName, attemptNo + 1);
                }
                return null;
            }
        }


        public static double ConvertPrice(CustomDict<string, double> prices, string from, string to)
        {
            double result = 0.0f;

            try
            {
                result = prices[from] / prices[to];
            }
            catch (Exception e)
            {
                Logger.debugMsg($"couldn't convert {from} - {to}\n" + e.Message + "\n" + e.StackTrace);
                return -1.0d;
            }
            return result;
        }

        public static CustomDict<string, double> ConvertPrices(CustomDict<string, double> prices, string convertToCoin)
        {
            //get the ratio of the coin we convert the ratios to
            double convertToValue = prices.Where<KeyValuePair<string, double>>(x => x.Key.Equals(convertToCoin)).First().Value;

            CustomDict<string, double> result = new CustomDict<string, double>();
            foreach (KeyValuePair<string, double> pair in prices)
            {
                result.Add(pair.Key, pair.Value / convertToValue);
            }
            return result;
        }
    }
}
