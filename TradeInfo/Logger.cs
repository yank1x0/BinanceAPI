using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace TradeInfo
{
    public class Logger
    {
        public enum LogType {INFO,ERROR }
        private Logger() { }

        private static Logger instance = null;

        private static StreamWriter LogWriter;
        private static FileStream lfs;

        public static Logger getInstance() {
            if (instance == null) instance = new Logger();
            return instance;

        }

        public static void CloseLogger() {
            
            lfs.Close();
        }

        public static void initDebugLogFile()
        {
            if (!Directory.Exists(Globals.LOGS_DIR)) Directory.CreateDirectory(Globals.LOGS_DIR);
        }

        public static void debugMsg(string msg)
        { debugMsg(msg, string.Empty); }

        public static void debugMsg(string msg,string threadName) {
            if (!Globals.TAKE_LOGS) return;
            object lock3=new object();
            bool success;
            int maxAttempts = 100;
            int attempt = 0;
            lock (lock3)
            {
                while (!(success=_WriteMessage(msg,threadName)) && (attempt++)<maxAttempts)
                {
                    Thread.Sleep(500);
                }
            }
        }

        private static bool _WriteMessage(string msg,string threadName)
        {
            try
            {
                string logline = "\n" + msg + "\n";
                string logfile = String.Format("{0}\\{1}_LOG.txt", Globals.LOGS_DIR, threadName);
                
                FileMode writeMode = File.Exists(logfile) ? FileMode.Append : FileMode.Create;
                lfs = File.Open(logfile, writeMode, FileAccess.Write);
                LogWriter = new StreamWriter(lfs);
                LogWriter.Write(DateTime.Now.ToString("MM/dd/yyyy-hh:mm:ss-tt") + " " + logline+"\n");
                LogWriter.Close();
                return true;
            }
            catch (Exception e)
            {
                return false;

            }
        }

        public static void logMsg(string msg, LogType type,bool includeDate) { logMsg(msg, type, "Default", includeDate); }

        public static void logMsg(string msg,LogType type,string name,bool includeDate)
        {
            try
            {

                string logpath = Globals.LOGS_DIR + "\\" + name;
                FileMode writeMode= File.Exists(logpath) ? FileMode.Append:FileMode.Create;
                FileStream logfile = File.Open(logpath, writeMode, FileAccess.Write);
                StreamWriter sw = new StreamWriter(logfile);
                //Globals.consoleDebugMsg(string.Format("date for {0} is {1}", name, DateTime.Now.ToString("MM/dd/yyyy-hh:mm:ss-tt")));
                if (includeDate) sw.Write(DateTime.Now.ToString("MM/dd/yyyy-hh:mm:ss-tt") + " " + msg);
                else sw.Write(msg);
                sw.Close();
                logfile.Close();
            }
            catch(Exception e)
            {
                Globals.consoleMsg("\type:eerror in log: "+e.Message,true);

            }
        }

    }
}
