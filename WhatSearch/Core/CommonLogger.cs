using System;

namespace WhatSearch.Core
{
    public interface ICommonLog
    {
        void Log(string message);
    }

    public class CommonLogger : ICommonLog
    {
        //private ILog logger = LogManager.GetLogger("default");
        public void Log(string message)
        {
            //logger.Info(message);
            Console.WriteLine(message);
        }
    }
}
