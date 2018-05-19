using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TradeInfo
{
    public enum MessageTypes { INFO, ERROR, DEFAULT, WARNING,DEBUG,SUCCESS}
    public enum Markets {Ethereum,Bitcoin,Litecoin }
    public enum RunMode { FullSimulation,Real,HalfSimulation,TestApi}

    public static class Globals
    {
        //CONSTANTS
        public static readonly string TYPE_INLINE_ID_START = "<type>", TYPE_INLINE_ID_END="</type>";
        public static readonly string SEPARATOR = "<|>";
        public static readonly ConsoleColor DEFAULT_COLOR = ConsoleColor.Gray;
        public static readonly ConsoleColor GOOD_COLOR = ConsoleColor.Green;
        public static readonly ConsoleColor ERROR_COLOR = ConsoleColor.Red;
        public static readonly ConsoleColor WARNING_COLOR = ConsoleColor.Yellow;
        public static readonly ConsoleColor DEBUG_COLOR = ConsoleColor.DarkYellow;
        public static readonly ConsoleColor INFO_COLOR = ConsoleColor.Cyan;
        public static readonly ConsoleColor DEFAULT_BGCOLOR = ConsoleColor.Black;
        public static readonly int TIMEOUT = 300;//secs
        public static readonly bool TAKE_LOGS = true;
        public static RunMode RUN_MODE=RunMode.Real;
        public static List<string> loggerOpenWindows = new List<string>();
        public static readonly string loggerTitleTemplate = "TradeBox.*?log";

        public static int focusTimer = 5000;

        public static int secsForBlackedAsset = 1800;

        public static int mainProcessID { get; set; } = 0;

        public static readonly string APP_LOCATION=getParentDir(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Substring(8).Replace('/','\\'));
        public static readonly string loadingMessage = "Loading...";

        public static readonly string lastTransFilename = "_lastTrans",sumsFilename="_sums",transactionsFilename="_transactions",profitsHistoryFilename="_profitsHistory", lastProfitFilename = "_profit";
        public static readonly string simuPriceslFilename = "_SimulationPrices", simulAmountsFilename = "_SimulationAmounts",blackListFile="_blackList";
        public static string lastTransPricesFilename(UInt32 id) { return "_transPricesHistory_ID_"+id; }
        public static string statisticsFilename = "_statistics";


        public static readonly CustomDict<string, string> COIN_SYMBOL_TO_NAME = new CustomDict<string, string>(){
                {"btc","Bitcoin" },
                {"eth","Ethereum"},
                {"xrp","Ripple" },
                {"ltc","LiteCoin" },
                {"bch","Bitcoin Cash" },
                {"bcc","Bitcoin Cash Old" },
                { "btg","Bitcoin Gold"},
                { "nis","NIS"},
                { "bnb","Binance Coin"},
                { "xmr","Monero"},
                { "xlm","Stellar"},
                { "xvg","Verge"},
                { "ont","Ontology"},
                { "poe","Po.et"},
                { "mod","Modum"},
                { "tnt","Tierion"},
                { "usdt","USD Tether"},
                { "neo","NEO"},
                { "trx","Tronix"},
                { "dash","DigitalCash"},
                { "undefined","undefined"}
            };

        public static string btc { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Bitcoin")).Key; } }
        public static string eth { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Ethereum")).Key; } }
        public static string nis { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("NIS")).Key; } }
        public static string bch { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Bitcoin Cash")).Key; } }
        public static string bcc { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Bitcoin Cash Old")).Key; } }
        public static string btg { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Bitcoin Gold")).Key; } }
        public static string ltc { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("LiteCoin")).Key; } }
        public static string xrp { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Ripple")).Key; } }
        public static string bnb { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Binance Coin")).Key; } }
        public static string NA { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("undefined")).Key; } }
        public static string xmr { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Monero")).Key; } }
        public static string xlm { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Stellar")).Key; } }
        public static string xvg { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Verge")).Key; } }
        public static string ont { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Ontology")).Key; } }
        public static string poe { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Po.et")).Key; } }
        public static string mod { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Modum")).Key; } }
        public static string tnt { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Tierion")).Key; } }
        public static string usdt { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("USD Tether")).Key; } }
        public static string neo { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("NEO")).Key; } }
        public static string trx { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("Tronix")).Key; } }
        public static string dash { get { return COIN_SYMBOL_TO_NAME.FirstOrDefault(x => x.Value.Equals("DigitalCash")).Key; } }

        private static int maxPrevMessageHeight = 5;

        public readonly static int MAX_ATTEMPTS= 10;
        public readonly static int MINS_BETWEEN_ATTEMPTS = 5;
        public static readonly int DATA_TIME_MIN = 5;
        public static readonly int TRADE_COMPETION_TIMEOUT = 5; //secs
        public static readonly int DATA_TIME_SEC = 60* DATA_TIME_MIN;
        public static readonly int WEBREQUEST_MAX_ATTEMPTS = 20;

        public static readonly int HTTP_ATTEMPTS = 5;

        //public static readonly string CRYPTOCOMPARE_API_URL = "https://min-api.cryptocompare.com/data/price?fsym=%COIN%&tsyms=USD";//BTC&tsyms=USD"
        public static readonly string BIT2C_PRICE_API_URL = "https://www.bit2c.co.il/Exchanges/%COINFROM%%COINTO%/ticker.json";


        public static readonly int DISP_HEIGHT = 150;
        public static readonly int DISP_WIDTH = Console.WindowWidth-1;
        public static readonly string LOGS_DIR = APP_LOCATION + "\\logs";

        public static readonly int HTTP_MAX_RESPONSE_TIME = 1;//secs

        public static int max(int int1, int int2) { return int1 > int2 ? int1 : int2; }
        public static int min(int int1, int int2) { return int1 < int2 ? int1 : int2; }


        public static MessageTypes charToType(char c)
        {
            switch (c)
            {
                case 'd':
                    return MessageTypes.DEBUG;
                case 'i':
                    return MessageTypes.INFO;
                case 'e':
                    return MessageTypes.ERROR;
                case 'w':
                    return MessageTypes.WARNING;
                case 'n':
                    return MessageTypes.DEFAULT;
                case 'g':
                    return MessageTypes.SUCCESS;
                default:
                    return MessageTypes.DEFAULT;
            }
        }


        public static string getParentDir(string path)
        {
            return string.Join("\\", path.Split('\\'), 0, (path.Split('\\')).Length - 1);
        }

        public static void consoleDebugMsg(string msg, [CallerLineNumber] int lineNumber = 0,
                [CallerMemberName] string caller = null,[CallerFilePath] string file=null)
        {
            object lock4 = new object();
            lock(lock4){
                consoleMsg("<type>d</type>file:" + file.Split('\\')[file.Split('\\').Length - 1] + " line:" + lineNumber + " at: " + caller + "\n" + msg + "\n", false);
            }
        }

        public static void consoleLineMsg(string msg)
        {
            consoleMsg(msg + "\n", false);
        }

        public static void consoleLineMsg(string msg, bool clear)
        {
            consoleMsg(msg + "\n", clear);
        }

        public static string ToHexString(byte[] array)
        {
            StringBuilder hex = new StringBuilder(array.Length * 2);
            foreach (byte b in array)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        public static void consoleMsg(string msg, bool clear)
        {
           // int initCurCol = Console.CursorLeft;
           // int initCurLine = Console.CursorTop;
            string totMsg = "<type>i</type>---------------\n<type>i</type>TradeInfo\n<type>i</type>---------------\n" + msg;
            string[] splitMsg = totMsg.Split(new string[] { "\n" }, StringSplitOptions.None);

            if (clear)
            {
                Console.Clear();
                Console.SetCursorPosition(0, 0);
                int i = 0, j = 0;
                int row = Console.CursorTop, col = Console.CursorLeft;
                //overwrite the lines
                foreach (string line in splitMsg)
                {

                    Console.SetCursorPosition(0, i++);

                    consoleMsg(line);

                    row = Console.CursorTop;
                    col = Console.CursorLeft;
                    //for (j = 0; j < DISP_WIDTH - line.Length; j++) Console.Write(" ");
                }
                //clear the rest of the display lines
                //for (; i < maxPrevMessageHeight+5; i++)
                //{
                //    Console.BackgroundColor = DEFAULT_BGCOLOR;
                //    for (j = 0; j < DISP_WIDTH; j++) consoleMsg(" ");
                //    Console.Write("\n");
                //}
                Console.SetCursorPosition(col, row);
                maxPrevMessageHeight = splitMsg.Length+1;
            }

            else
            {
                consoleMsg(msg);
                maxPrevMessageHeight += splitMsg.Length+1;
            }
            //Console.SetCursorPosition(initCurCol, initCurLine);
        }

        public static double Min(double n1, double n2) { return n1 > n2 ? n2 : n1; }

        public static void consoleMsg(string msg)
        {
           // int initCurCol = Console.CursorLeft;
            //int initCurLine = Console.CursorTop;
            //split the lines in a way that 1st letter is the type
            string[] splitMsg = msg.Split(new string[] { TYPE_INLINE_ID_START, TYPE_INLINE_ID_END ,SEPARATOR}, StringSplitOptions.None);
            if (splitMsg.Length <3)
            {
                Console.ForegroundColor = Globals.DEFAULT_COLOR;
                Console.BackgroundColor = Globals.DEFAULT_BGCOLOR;
                Console.Write(msg);
                return;
            }

            for (int i = 0; i < splitMsg.Length-2; i+=2)
            {
                string splitTypes = splitMsg[i+1];
                string txt = splitMsg[i+2];
                bool foreground = true;

                foreach (string type in splitTypes.Split(','))
                {
                    if (type == "f") continue;
                    if (type == "b") { foreground = false; continue; }
                    switch (charToType(type.ToCharArray()[0]))
                    {
                        case MessageTypes.DEBUG:
                            SetTextColor(DEBUG_COLOR, foreground);
                            break;
                        case MessageTypes.ERROR:
                            SetTextColor(ERROR_COLOR, foreground);
                            break;
                        case MessageTypes.INFO:
                            SetTextColor(INFO_COLOR, foreground);
                            break;
                        case MessageTypes.WARNING:
                            SetTextColor(WARNING_COLOR, foreground);
                            break;
                        case MessageTypes.SUCCESS:
                            SetTextColor(GOOD_COLOR, foreground);
                            break;
                        default:
                            SetTextColor(DEFAULT_COLOR, foreground);
                            break;
                    }
                }
                Console.Write(txt);
                Console.ForegroundColor = DEFAULT_COLOR;
                Console.BackgroundColor = DEFAULT_BGCOLOR;
                Console.CursorVisible = false;
            }
            //Console.SetCursorPosition(initCurCol, initCurLine);
        }

        private static void SetTextColor(ConsoleColor col,bool foreground)
        {
            if (foreground)
            {
                Console.ForegroundColor = col;
                Console.BackgroundColor = Globals.DEFAULT_BGCOLOR;
            }
            else
            {
                Console.ForegroundColor = Globals.DEFAULT_BGCOLOR;
                Console.BackgroundColor = col;
            }
        }

    [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

    [DllImport("USER32.DLL")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

    public static void setMainProcessID()
        {
            var activatedHandle = GetForegroundWindow();
            int procId;
            GetWindowThreadProcessId(activatedHandle, out procId);
            mainProcessID = procId;
        }

    public static void bringToFront(string title) {
        // Get a handle to the Calculator application.
        IntPtr handle = FindWindow(null, title);
            // Verify that Calculator is a running process.
            if (handle == IntPtr.Zero) {
                Logger.debugMsg("title=" + title+" not found");
                return;
        }

        // Make Calculator the foreground application
        SetForegroundWindow(handle);
    }

    public static void killProcess(int id)
        {
            Process p = Process.GetProcessById(id);
            p.Kill();
        }

    public static List<string> getAllLoggerWindowsTitles()
    {
        Process[] processlist = Process.GetProcesses();
            List<string> result = new List<string>();
        foreach (Process process in processlist)
        {
            if (!String.IsNullOrEmpty(process.MainWindowTitle))
            {
                    //Logger.debugMsg(string.Format("Process: {0} ID: {1} Window title: {2}", process.ProcessName, process.Id, process.MainWindowTitle));
                    if (Regex.Match(process.MainWindowTitle, loggerTitleTemplate).Success)
                    {
                        result.Add(process.MainWindowTitle);
                    }
            }
        }
        return result;
    }

        public static List<int> getAllLoggerWindowsIds()
        {
            Process[] processlist = Process.GetProcesses();
            List<int> result = new List<int>();
            foreach (Process process in processlist)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    //Logger.debugMsg(string.Format("Process: {0} ID: {1} Window title: {2}", process.ProcessName, process.Id, process.MainWindowTitle));
                    if (Regex.Match(process.MainWindowTitle, loggerTitleTemplate).Success)
                    {
                        result.Add(process.Id);
                    }
                }
            }
            return result;
        }


        public static void displayLoading()
        {
            Console.Clear();
            Console.SetCursorPosition(1, 5);
            Console.Write(loadingMessage);
            Console.SetCursorPosition(0,0);
        }

        public static bool ApplicationIsActivated()
    {
        var activatedHandle = GetForegroundWindow();
        if (activatedHandle == IntPtr.Zero)
        {
            return false;       // No window is currently activated
        }

        var procId = Process.GetCurrentProcess().Id;
        int activeProcId;
        GetWindowThreadProcessId(activatedHandle, out activeProcId);
        return activeProcId == mainProcessID;
    }

        public struct ConsoleLine{
        string text;
        MessageTypes type;
            public ConsoleLine(string txt,MessageTypes type)
        {
            this.text = txt;
            this.type = type;
        }

        }

        public static double DegreesToRads(float deg) { return (Math.PI / 180) * deg; }
        public static double RadsToDegrees(float rad) { return (180/Math.PI) * rad; }

        public struct Balance
        {
            public double free;
            public double inTrade;
            public double all;
            public Balance(double _free, double _inTrade) { free = _free;inTrade = _inTrade; all = free + inTrade; }
        }

        public static T[] subArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public struct Coin
        {
            public string marketName;
            public string marketSymbol;

            public Coin(string name, string symbol)
            {
                marketName = name;
                marketSymbol = symbol;
            }
            public override string ToString()
            {
                return marketSymbol;
            }

            public bool isEmpty() { return marketSymbol.Equals(Globals.NA); }
            public static bool operator ==(Globals.Coin c1, Globals.Coin c2)
            {
                return c1.marketName.ToLower().Equals(c2.marketName.ToLower()) && c1.marketSymbol.ToLower().Equals(c2.marketSymbol.ToLower());
                }
            public static bool operator !=(Globals.Coin c1, Globals.Coin c2)
            {
                return !(c1.marketName.Equals(c2.marketName) && c1.marketSymbol.Equals(c2.marketSymbol));
            }
        }
    }
}
