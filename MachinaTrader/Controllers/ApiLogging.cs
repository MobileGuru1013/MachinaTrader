using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MachinaTrader.Globals;
using MachinaTrader.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MachinaTrader.Controllers
{
    [Authorize, Route("api/logging/")]
    public class ApiLogging : Controller
    {
        [HttpGet]
        [Route("logs")]
        public IActionResult Logs()
        {
            var log = Log.ReadTail(Global.DataPath + "/Logs/MachinaTrader-" + DateTime.Now.ToString("yyyyMMdd") + ".log", 500);
            return new JsonResult(log);
        }
    }

    public class Log
    {
        /// <summary>
        /// Reading log file
        /// </summary>
        /// <param name="filename">filename as string</param>
        /// <param name="lines">Get number of lines which should be red</param>
        /// <returns></returns>
        public static List<LogEntry> ReadTail(string filename, int lines = 10)
        {
            var logEntries = new List<LogEntry>();
            string[] entries;
            using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                TextReader tr = new StreamReader(fs);
                entries =  Tail(tr, lines);
            }

            // Split each line to model for searchable table
            foreach (var entry in entries)
            {
                var dateExctracted = entry.Substring(0, 30);
                var dateValue = DateTimeOffset.Parse(dateExctracted, CultureInfo.InvariantCulture);
                logEntries.Add(new LogEntry()
                {
                    Date = dateValue.UtcDateTime,
                    LogState = entry.Split('[', ']')[1],
                    Msg = entry.Split("]")[1],
                });
            }
            return logEntries;
        }


        ///<summary>Returns the end of a text reader.</summary>
        ///<param name="reader">The reader to read from.</param>
        ///<param name="lineCount">The number of lines to return.</param>
        ///<returns>The last lneCount lines from the reader.</returns>
        public static string[] Tail(TextReader reader, int lineCount)
        {
            var buffer = new List<string>(lineCount);
            string line;
            for (var i = 0; i < lineCount; i++)
            {
                line = reader.ReadLine();
                if (line == null) return buffer.ToArray();
                if (!line.StartsWith("2")) i--;
                if (line.StartsWith("2")) buffer.Add(line);
            }

            //The index of the last line read from the buffer.  Everything > this index was read earlier than everything <= this indes
            int lastLine = lineCount - 1;

            while (null != (line = reader.ReadLine()))
            {
                if (line.StartsWith("2"))
                {
                    lastLine++;
                    if (lastLine == lineCount) lastLine = 0;
                    buffer[lastLine] = line;
                };
            }

            if (lastLine == lineCount - 1) return buffer.ToArray();
            var retVal = new string[lineCount];
            buffer.CopyTo(lastLine + 1, retVal, 0, lineCount - lastLine - 1);
            buffer.CopyTo(0, retVal, lineCount - lastLine - 1, lastLine + 1);
            return retVal;
        }
    }
}
