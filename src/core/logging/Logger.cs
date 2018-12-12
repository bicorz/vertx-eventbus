using log4net;
using log4net.Config;
using log4net.Repository;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace io.vertx.core.logging
{

    public class Logger
    {

        public static string LoggerRepositoryName { get; set; }

        static Logger()
        {
            ILoggerRepository repo = LogManager.GetRepository(Assembly.GetCallingAssembly());
            LoggerRepositoryName = repo.Name;
        }

        public static ILog GetLogger(Type type)
        {
            return LogManager.GetLogger(LoggerRepositoryName, type);
        }

        public static ILog GetLogger(string type)
        {
            return LogManager.GetLogger(LoggerRepositoryName, type);
        }

    }

}
