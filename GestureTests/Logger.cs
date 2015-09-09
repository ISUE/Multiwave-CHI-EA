using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureTests
{
    public class Logger
    {
        /// <summary>
        /// Target Location for user logs.
        /// </summary>
        public static string LogPath = @"..\..\..\log\";

        StreamWriter sw;
        
        public Logger(string suffix)
        {
            sw = new StreamWriter(LogPath + DateTime.Now.ToString("ddhhmmss") + suffix + ".txt");
            sw.WriteLine("Started " + suffix + " at " + DateTime.Now.ToLongTimeString());

        }

        public void Log(String gesture, String expected, List<List<int>> hist)
        {
            sw.WriteLine("Detected: " + gesture);
            sw.WriteLine("Expected: " + expected);
            sw.WriteLine("Channels: " + hist.Count);
            sw.WriteLine("Length: " + hist[0].Count);
            sw.WriteLine("Data:");
            foreach (List<int> sublist in hist)
            {
                for (int i = 0; i < sublist.Count; i++)
                {
                    sw.Write(sublist[i] + " ");
                }
                sw.WriteLine();
                sw.WriteLine();
            }
            sw.Flush();
        }

        public void Log(String keypress)
        {
            sw.WriteLine("Keypress: " + keypress);
            sw.WriteLine();
            sw.Flush();
        }
    }
}
