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

        private static string logFolder = "Logs";

        static int Main(string[] args)
        {
            Config conf = CommandLineParser.GetConfig(args);

            if (conf.Report)
                DLReport();
            else
                DLRun(conf);

            return 0;
        }


        /// <summary>
        /// Entrypoint to the program that has the sole purpose of running batch jobs to move data from one place to another.  The data is moved by implementing BaseDataLoader from Aih.DataLoader.Tools.
        /// This program takes in a parameter with a dll name, looks for that dll in the Loaders folder (might be configurable in the future), finds all classes that implement BaseDataLoader in it, creates
        /// one instance of each and calls RunDataLoader() on all of them.
        /// </summary>
        /// <param name="args">
        /// -dll : Name of the assembly that contains the dataloaders.
        /// -n: (optional).  If you only want to create a specific dataloader within a dll pass its name here. If the -n parmeter is empty, an instance is created for all classes that implement BaseDataLoader.
        /// </param>
        static int DLRun(Config conf)
        {
            //Setup folders
            if (!Directory.Exists("DataLoaders"))
            {
                Directory.CreateDirectory("DataLoaders");
            }
            if(!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }


            ILoaderConfigHandler configStore = null;
            IStatusHandler statusHandler = null;

            if (!SetHandlers(ref configStore, ref statusHandler))
                return 1;

            //TODO: Test the result of the HasInvalidDllName(conf) function below thoroughly
            if (HasInvalidDllName(conf))
                return 1;

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(ResolveLoaderDependenciesEventHandler);  //Since I assume that all the dataloaders are contained in the same dll I can do this.

            foreach (var dl in conf.TypeName)
            {
                SetConsole(dl);

                if (!LoadDllAndRunDataLoader(conf.DllName, dl, configStore, statusHandler))
                    return 1;

                //return 0;
            }

            return 0;

        }

        private static bool HasInvalidDllName(Config conf)
        {
            return (conf.DllName == "");
        }




        static void DLReport()
        {
            SetConsole("report");
            DLReporter reporter = new DLReporter();
            List<DataLoaderInfo> infos = reporter.ReportDLs();

            string json = JsonConvert.SerializeObject(infos, Formatting.Indented);
            Console.WriteLine(json);
            
        }


        private static void SetConsole(string logname)
        {
            //TODO: Make configurable
            string dllLogFolder = logFolder + @"\" + logname; 

            if (!Directory.Exists(dllLogFolder))
            {
                Directory.CreateDirectory(dllLogFolder);
            }

            string fileName = DateTime.Now.ToString("yyyy-MM-dd") + "  " + logname + " - DataLoader.txt";
            ConsoleToFileWriter writer = new ConsoleToFileWriter(dllLogFolder + @"\" + fileName);
            Console.SetOut(writer);
            Console.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:SS") + "Started Loader");
        }




        private static bool LoadDllAndRunDataLoader(string dllName, string dataloaderName, ILoaderConfigHandler configHandler, IStatusHandler statusHandler)
        {

            string path = "";
            
            if (Directory.Exists("DataLoaders"))
            {
                path = Environment.CurrentDirectory + @"\DataLoaders\" + dllName + @"\" + dllName + ".dll";
            }
            else
            {
                Console.WriteLine("Folder DataLoaders not found, created folder");
                Directory.CreateDirectory("DataLoaders");
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
                Console.WriteLine("File: " + path + " exception came up: " + ex.Message);
                return false;
            }
        }





        



        private static Assembly ResolveLoaderDependenciesEventHandler(object sender, ResolveEventArgs args)
        {
            string path = "";

            try
            {
                string dllName = args.Name.Substring(0, args.Name.IndexOf(','));
                Assembly ass = null;

                Assembly requestingAssembly = args.RequestingAssembly;
                path = Environment.CurrentDirectory + @"\DataLoaders\" + dllName + ".dll";

                ass = Assembly.LoadFrom(path);
                return ass;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem resolving assembly: " + path);
                Console.WriteLine(ex.Message);
                return null;
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

        //private static bool HasValidDllName(Dictionary<string, string> config)
        //{
        //    //TODO - Make sure the file exists as well
        //    return config["DLL"] == "";
        //}




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

