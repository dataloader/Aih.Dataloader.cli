using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Aih.DataLoader;
using Aih.DataLoader.Interfaces;
using DataloaderCLI.Models;
using Newtonsoft.Json;
using DataloaderCLI.Services;

namespace DataloaderCLI
{
    class Program
    {
        private static string DATLOADERS_ROOT_FOLDER = "DataLoaders";
        private static string LOGFOLDER = "Logs";
        private static string REPORT_FOLDER = "Reports";

        private static string ISO_DATE_FORMAT = "yyyy-MM-dd";
        private static string ISO_DATETIME_FORMAT = "yyyy-MM-dd HH:mm:SS";


        /// <summary>
        /// Entrypoint to the program that has the sole purpose of running batch jobs to move data from one place to another.  The data is moved by implementing BaseDataLoader from Aih.DataLoader.Tools.
        /// This program takes in a parameter with a dll name, looks for that dll in the Loaders folder (might be configurable in the future), finds all classes that implement BaseDataLoader in it, creates
        /// one instance of each and calls RunDataLoader() on all of them.
        /// </summary>
        /// <param name="args">
        /// -dll : Name of the assembly that contains the dataloaders.
        /// -n: Name of the dataloader(s) that you want to run. Example: -n DemoDataLoader , to run two (or more) dataloaders, simply put a 
        /// </param>
        static int Main(string[] args)
        {
            Config conf = CommandLineParser.GetConfig(args);

            if (conf.Report)
                DLReport();
            else
                DLRun(conf);

            return 0;
        }



        private static int DLRun(Config conf)
        {
            //Setup folders
            if (!Directory.Exists(DATLOADERS_ROOT_FOLDER))
            {
                Directory.CreateDirectory(DATLOADERS_ROOT_FOLDER);
            }
            if(!Directory.Exists(LOGFOLDER))
            {
                Directory.CreateDirectory(LOGFOLDER);
            }

            SetConsole(LOGFOLDER);

            ILoaderConfigHandler configStore = null;
            IStatusHandler statusHandler = null;

            if (!SetHandlers(ref configStore, ref statusHandler))
            {
                Console.WriteLine("Problem with setting handlers, exiting with error");
                return 1;
            }

            if (HasInvalidDllName(conf))
            {
                Console.WriteLine("Invalid dll name, exiting with error");
                return 1;
            }

            AppDomain currentDomain = AppDomain.CurrentDomain;

            foreach (var dl in conf.TypeName)
            {
                SetConsole(dl);

                if (!LoadDllAndRunDataLoader(conf.DllName, dl, configStore, statusHandler))
                {
                    SetConsole(LOGFOLDER);
                    Console.WriteLine("Problem with loading and running dll, exiting with error");
                    return 1;
                }
            }

            return 0;

        }

        private static bool HasInvalidDllName(Config conf)
        {
            return (conf.DllName == "");
        }




        private static void DLReport()
        {
            SetConsole(REPORT_FOLDER);
            DLReporter reporter = new DLReporter();
            List<DataLoaderInfo> infos = reporter.ReportDLs();

            string json = JsonConvert.SerializeObject(infos, Formatting.Indented);
            Console.WriteLine(json);
            
        }


        private static void SetConsole(string logname)
        {

            string dllLogFolder = LOGFOLDER + @"\" + logname; 

            if (!Directory.Exists(dllLogFolder))
            {
                Directory.CreateDirectory(dllLogFolder);
            }

            string fileName = $"{DateTime.Now.ToString(ISO_DATE_FORMAT)} {logname}.txt";

            ConsoleToFileWriter writer = new ConsoleToFileWriter($@"{dllLogFolder}\{fileName}");
            Console.SetOut(writer);
            Console.Write(DateTime.Now.ToString(ISO_DATETIME_FORMAT) + "Started Loader");
        }




        private static bool LoadDllAndRunDataLoader(string dllName, string dataloaderName, ILoaderConfigHandler configHandler, IStatusHandler statusHandler)
        {

            string path = "";
            
            if (Directory.Exists(DATLOADERS_ROOT_FOLDER))
            {
                path = Environment.CurrentDirectory + $@"\{DATLOADERS_ROOT_FOLDER}\{dllName}\{dllName}.dll";
            }
            else
            {
                Directory.CreateDirectory(DATLOADERS_ROOT_FOLDER);
                return false;
            }
           
            try
            {
                Assembly plugin = Assembly.LoadFile(path);
                Type[] types = plugin.GetTypes();

                foreach (var type in types)
                {
                    if (type.Name == dataloaderName)
                    {
                        RunDataLoader(type, configHandler, statusHandler);
                        return true;
                    }
                }

                return false;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"File: {path}  -> Exception came up: {ex.Message}");
                return false;
            }
        }


        private static void RunDataLoader(Type type, ILoaderConfigHandler configHandler, IStatusHandler statusHandler)
        {

            if (type.IsSubclassOf(typeof(BaseDataLoader)))
            {
                var loader = (BaseDataLoader)Activator.CreateInstance(type);
                loader.InitializeHandlers(configHandler, statusHandler);
                loader.RunDataLoader();
            }
        }


        private static bool SetHandlers(ref ILoaderConfigHandler configStore, ref IStatusHandler statusHandler)
        {
            string reportStoreType = "";
            string reportStoreConnectionString = "";
            string configStoreName = "";
            string configStoreConnectionString = "";

            try
            {
                IniParser ini = new IniParser("conf.ini");
                reportStoreType = ini.GetSetting("STATUS_REPORT", "REPORT_TO");
                reportStoreConnectionString = ini.GetSetting("STATUS_REPORT", "REPORT_CONNECTION");
                configStoreName = ini.GetSetting("CONFIG_STORE", "TYPE");
                configStoreConnectionString = ini.GetSetting("CONFIG_STORE", "CONNECTION");
            }
            catch (System.IO.FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            //Setup property store
            //Currently we hardcode this to SQL Server, the infrastructure to reflect on it is in place
            configStore = new Aih.DataLoader.ConfigHandlers.SQLServerLoaderConfigHandler(configStoreConnectionString);


            //Setup where to report status to
            //Currently we hardcode this to SQL Server, the infrastructure to reflect on it is in place
            statusHandler = new Aih.DataLoader.StatusHandlers.SQLServerStatusHandler(reportStoreConnectionString);

            return true;
        }
    }




    
}

