using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;
using Newtonsoft.Json;
using WhatSearch.Utility;

namespace WhatSearch.Core
{
    public class CommonAppender : AppenderSkeleton
    {

        public string Folder { get; set; }

        Dictionary<string, StreamWriter> writers = new Dictionary<string, StreamWriter>();

        private DateTime TheDate;

        protected override void Append(LoggingEvent log)
        {
            if (TheDate != DateTime.Today)
            {
                TheDate = DateTime.Today;
            }

            string msg = RenderLoggingEvent(log);

            StreamWriter writer;
            if (writers.TryGetValue(log.LoggerName, out writer) == false)
            {
                string logPath = Helper.GetRelativePath(Folder, log.LoggerName,
                    TheDate.ToString("yyyyMMdd-") + Environment.MachineName) + ".log";
                FileInfo fiLog = new FileInfo(logPath);
                if (fiLog.Directory.Exists == false)
                {
                    fiLog.Directory.Create();
                }
                writer = new StreamWriter(fiLog.FullName, true, Encoding.UTF8);
                writers[log.LoggerName] = writer;
            }

            WriteLog(writer, msg);
        }

        private void WriteLog(StreamWriter writer, string msg)
        {
            lock (writer)
            {
                writer.Write(msg);
                writer.Flush();
            }
        }

        protected override void OnClose()
        {
            foreach (var pair in writers)
            {
                try
                {
                    pair.Value.Close();
                }
                catch
                {
                }
            }
            base.OnClose();
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            base.Append(loggingEvents);
        }
    }
}
