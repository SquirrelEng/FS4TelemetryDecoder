using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS4TelemetryDecoder
{
    public class Logger
    {
        private const string LogFileName = "FS4Decoder.log";

        
        public static void Write(string fmt, params object[] args)
        {
            string Msg = string.Format(fmt, args);
            Write(Msg);
        }

        public static void Write(string Msg)
        {
            try
            {
                using (StreamWriter w = File.AppendText(LogFileName))
                {
                    string line = string.Format("{0:yyyy-MM-dd HH:mm:ss} UTC - {1}", DateTime.UtcNow, Msg);
                    w.WriteLine(line);
                }
            }
            catch (Exception ex) { }
        }
    }
}
