using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ErrorCategorizationTool
{
    public class Constants
    {
        static Constants()
        {
            string jenkinsPath = Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), @"Jenkins\Builds"));
            if (!Directory.Exists(jenkinsPath))
            {
                Directory.CreateDirectory(jenkinsPath);
            }
        }
        public static string buildPath = Path.Combine(Directory.GetCurrentDirectory(), @"Jenkins\Builds");
        public static string exportFilePath = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "Jenkins"), "Jenkins_Failures.csv");
        public static string errorFilePath = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "Jenkins"), "ErrorLevel.csv");
        public static string Failure = "FAILED:";
        public static string Passed = "PASSED:";
        public static string ErrorMessage = "ERROR MESSAGE:";
        public static string StackTrace = "Stack Trace:";
        public static string StandardOutputMessages = "Standard Output Messages:";
    }
}
