using System;
using System.IO;
using Java.Lang;
using Environment = Android.OS.Environment;
using Exception = System.Exception;

namespace WB.Core.GenericSubdomains.Logging.AndroidLogger
{
    internal class FileLogger : ILogger
    {
        private static readonly string LogFilename = Path.Combine(GetLogDirectory(), "WBCapi.log.txt");
        
        private const string Tag = "Android.WBCapi";
        private const string CapiFolderName = "CAPI";
        private const string LogFolderName = "Logs";

        private static string GetLogDirectory()
        {
            var storageDirectory = Directory.Exists(Environment.ExternalStorageDirectory.AbsolutePath)
                             ? Environment.ExternalStorageDirectory.AbsolutePath
                             : System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

            storageDirectory = Path.Combine(storageDirectory, CapiFolderName);

            if (!Directory.Exists(storageDirectory))
            {
                Directory.CreateDirectory(storageDirectory);
            }

            storageDirectory = Path.Combine(storageDirectory, LogFolderName);

            if (!Directory.Exists(storageDirectory))
            {
                Directory.CreateDirectory(storageDirectory);
            }
           
            return storageDirectory;
        }

        public FileLogger()
        {
#if DEBUG
            this.IsDebugEnabled = true;
            this.IsInfoEnabled = true;
            this.IsWarnEnabled = true;
#else
            this.IsDebugEnabled = false;
            this.IsInfoEnabled = false;
            this.IsWarnEnabled = false;
#endif
            this.IsErrorEnabled = true;
            this.IsFatalEnabled = true;
        }

        public void Debug(string message, Exception exception = null)
        {
            if (!this.IsDebugEnabled) return;
            if (exception == null)
            {
                this.WriteLogMessage(Tag, LogMessageType.Debug, message);
            }
            else
            {
                this.WriteLogMessage(Tag, LogMessageType.Debug, exception.ToThrowable(), message);    
            }
            
        }
       
        public void Info(string message, Exception exception = null)
        {
            if (!this.IsInfoEnabled) return;
            if (exception == null)
            {
                this.WriteLogMessage(Tag, LogMessageType.Info, message);
            }
            else
            {
                this.WriteLogMessage(Tag, LogMessageType.Info, exception.ToThrowable(), message);    
            }
            
        }
        
        public void Warn(string message, Exception exception = null)
        {
            if (!this.IsWarnEnabled) return;
            if (exception == null)
            {
                this.WriteLogMessage(Tag, LogMessageType.Warning, message);
            }
            else
            {
                this.WriteLogMessage(Tag, LogMessageType.Warning, exception.ToThrowable(), message);    
            }
            
        }

        public void WarnFormat(string format, params object[] args)
        {
            if (this.IsWarnEnabled)
                this.WriteLogMessage(Tag, LogMessageType.Warning, format, args);
        }

        public void Error(string message, Exception exception = null)
        {
            if (exception==null)
            {
                this.WriteLogMessage(Tag, LogMessageType.Error, message);
            }
            else
            {
                this.WriteLogMessage(Tag, LogMessageType.Error, exception.ToThrowable(), message);    
            }
        }

        public void Fatal(string message, Exception exception = null)
        {
            if (!this.IsFatalEnabled) return;
            if (exception == null)
            {
                this.WriteLogMessage(Tag, LogMessageType.Fatal, message);
            }
            else
            {
                this.WriteLogMessage(Tag, LogMessageType.Fatal, exception.ToThrowable(), message);    
            }
        }

        public bool IsTraceEnabled { get; private set; }
        public bool IsDebugEnabled { get; private set; }
        public bool IsInfoEnabled { get; private set; }
        public bool IsWarnEnabled { get; private set; }
        public bool IsErrorEnabled { get; private set; }
        public bool IsFatalEnabled { get; private set; }

        private void WriteLogMessage(string tag, string type,  string message)
        {
            using (var s = File.AppendText(LogFilename))
            {
                s.WriteLine(string.Format("{0} {1} {2} {3}", DateTime.UtcNow, type, tag, message));
            }
        }

        private void WriteLogMessage(string tag, string type, Throwable exc, string message)
        {
            using (var s = File.AppendText(LogFilename))
            {
                s.WriteLine(string.Format("{0} {1} {2} {3} {4}", DateTime.UtcNow, type, tag, message, exc));
            }
        }

        private void WriteLogMessage(string tag, string type ,string format, params object[] args)
        {
            using (var s = File.AppendText(LogFilename))
            {
                s.WriteLine(string.Format("{0} {1} {2} {3}", DateTime.UtcNow, type , tag, string.Format(format, args)));    
            }
        }

    }

    public static class LogMessageType
    {
        public const string Debug = "Debug";
        public const string Error = "Error";
        public const string Fatal = "Fatal";
        public const string Info = "Info";
        public const string Warning = "Warning";

    }
}