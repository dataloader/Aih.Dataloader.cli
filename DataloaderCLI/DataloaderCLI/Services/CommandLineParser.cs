using DataloaderCLI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataloaderCLI.Services
{
    internal static class CommandLineParser
    {

        public static Config GetConfig(string[] args)
        {
            Config conf = new Config();

            //if there are no arguments I want to run the reporter of if I request the reporter directly
            if (Empty(args) || (RequestReporter(args)))
            {
                conf.Report = true;
                return conf;
            }

            conf.Report = false;
            conf.DllName = GetValue(args, "-dll");
            conf.TypeName = GetValues(args, "-n");

            return conf;
        }

        private static bool RequestReporter(string[] args)
        {
            return GetValue(args, "-report") != "";
        }

        private static bool Empty(string[] args)
        {
            return (args.Length == 0);
        }


        private static List<string> GetValues(string[] args, string parameter)
        {
            List<string> values = new List<string>();
            bool captureValues = false;  //The first element in the array should never be captured, as it is supposed to be a control character

            if (!Empty(args))
            {
                for (int i = 0; i < args.Length; i++)  //loop over all the args array
                {
                    if(captureValues)
                    {
                        values.Add(args[i]);
                        if(i+1 < args.Length)
                        {
                            if(args[i+1].IndexOf("-") != -1)
                            {
                                captureValues = false;
                            }
                        }
                    }

                    if (!captureValues)
                    {
                        captureValues = args[i].ToLower() == parameter;
                    }
                }
            }

            return values;
        }



        private static string GetValue(string[] args, string parameter)
        {
            if ((args != null) && (args.Length > 0))
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower() == parameter)
                    {
                        return args[i + 1];
                    }
                }
            }

            return "";
        }
    }
}
