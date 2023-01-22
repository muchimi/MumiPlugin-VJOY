namespace Loupedeck.MumiPlugin
{

    using System;
    using System.IO;

    using log4net;

    /// <summary>
    /// implements log4Net logger
    /// </summary>
    public static class MumiLog
    {

        public static ILog Log { get; private set; }


        public static bool Config(String pluginDataDirectory)
        {

            var configFileDirectory = Path.Combine(pluginDataDirectory, "logconfig.xml");
            var logFile = Path.Combine(pluginDataDirectory, "log.txt");
            var info = new FileInfo(configFileDirectory);
            if (info.Exists)
            {
                log4net.Config.XmlConfigurator.ConfigureAndWatch(info);

                var appender = (log4net.Appender.FileAppender)LogManager.GetRepository().GetAppenders()[0];
                appender.File = logFile;
                appender.ActivateOptions();
                MumiLog.Log = log4net.LogManager.GetLogger("log4netFileLogger");
                return true;
            }

            return false;
        }

        public static void Info(object message)
        {
            if (MumiLog.Log != null)
                MumiLog.Log.Info(message);
        }

        public static void InfoFormat(String format, params object[] args)
        {
            if (MumiLog.Log != null)
                MumiLog.Log.InfoFormat(format, args);
        }

        public static void Error(object message)
        {
            if (MumiLog.Log != null)
                MumiLog.Log.Error(message);
        }


        public static void ErrorFormat(string format, params object[] args)
        {
            if (MumiLog.Log != null)
                MumiLog.Log.ErrorFormat(format, args);
        }

        public static void Warn(object message)
        {
            if (MumiLog.Log != null)
                MumiLog.Log.Warn(message);
        }


        public static void WarnFormat(string format, params object[] args)
        {
            if (MumiLog.Log != null)
                MumiLog.Log.WarnFormat(format, args);
        }

        public static void Fatal(object message)
        {
            if (MumiLog.Log != null)
                MumiLog.Log.Fatal(message);
        }

        public static void FatalFormat(string format, params object[] args)
        {
            if (MumiLog.Log != null)
                MumiLog.Log.FatalFormat(format, args);
        }
    }
}