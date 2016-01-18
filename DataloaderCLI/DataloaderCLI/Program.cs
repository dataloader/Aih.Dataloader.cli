using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Aih.DataLoader.Tools;

namespace DataloaderCLI
{
    class Program
    {

        /// <summary>
        /// Entrypoint to the program that has the sole purpose of running batch jobs to move data from one place to another.  The data is moved by implementing BaseDataLoader from Aih.DataLoader.Tools.
        /// This program takes in a parameter with a dll name, looks for that dll in the Loaders folder (might be configurable in the future), finds all classes that implement BaseDataLoader in it, creates
        /// one instance of each and calls RunDataLoader() on all of them.
        /// </summary>
        /// <param name="args">
        /// -dll : Name of the assembly that contains the dataloaders.
        /// -n: (optional).  If you only want to create a specific dataloader within a dll pass its name here. If the -n parmeter is empty, an instance is created for all classes that implement BaseDataLoader.
        /// </param>
        static int Main(string[] args)
        {
            //Setup folders
            if (!Directory.Exists("DataLoaders"))
            {
                Directory.CreateDirectory("DataLoaders");
                Directory.CreateDirectory(@"DataLoaders\Refrences");
            }
            if(!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }
            



            Console.WriteLine("Begin Main");
            //Parse arguments 
            Dictionary<string, string> config = CommandLineParser.GetConfig(args);

            IPropertyHandler propertyStore = null;
            IStatusHandler statusHandler = null;

            Console.WriteLine("Setting handlers");
            if (!SetHandlers(ref propertyStore, ref statusHandler))
                return 1;

            Console.WriteLine("Checking for dll name");
            if (HasValidDllName(config))
                return 1;

            Console.WriteLine("Change where the console goes to");
            SetConsole(config);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(ResolveLoaderDependenciesEventHandler);

            if (!LoadDllAndRunDataLoader(config, propertyStore, statusHandler))
                return 1;

            return 0;

        }

        private static void SetConsole(Dictionary<string, string> config)
        {
            //TODO: Make configurable
            string fileName = DateTime.Now.ToString("yyyy-MM-dd") + "  " + config["TYPENAME"] + " - DataLoader.txt";
            ConsoleToFileWriter writer = new ConsoleToFileWriter(@"Logs\" + fileName);
            Console.SetOut(writer);
            Console.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:SS") + "Started Loader");
        }


        private static bool LoadDllAndRunDataLoader(Dictionary<string, string> config, IPropertyHandler propertyHandler, IStatusHandler statusHandler)
        {

            //UGLY CODE BEGINS
            string path = "";
            if (config["PATH"] == "")
            {
                if (Directory.Exists("DataLoaders"))
                {
                    path = Environment.CurrentDirectory + @"\DataLoaders\" + config["DLL"] + ".dll";
                }
                else
                {
                    Console.WriteLine("Folder DataLoaders not found, created folder");
                    Directory.CreateDirectory("DataLoaders");
                    return false;
                }
            }
            else
            {
                path = config["PATH"] + config["DLL"] + ".dll";
            }
            //UGLY CODE ENDS

            //bool oneDataLoader = config["TYPENAME"] != "";
            try
            {
                Assembly plugin = Assembly.LoadFile(path);
                Type[] types = plugin.GetTypes();

                foreach (var type in types)
                {
                    //TODO: Find a cleaner and more readeble solution to this if possible
                    //This terrible implementation is trying to do the following:
                    //If the name parameter in config is specified we only want to create an instance of that class and run the DataLoader
                    //if no name is set then we want to execute RunDataLoader for all classes that implement BaseDataLoader

                    //if (oneDataLoader)
                    //{
                        if (type.Name == config["TYPENAME"])
                        {
                            RunDataLoader(type, propertyHandler, statusHandler);
                        return true;
                        }
                    //}
                    //else
                    //{
                    //    RunDataLoader(type, propertyHandler, statusHandler);
                    //}
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
                path = Environment.CurrentDirectory + @"\DataLoaders\Refrences\" + dllName + ".dll";

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


        private static void RunDataLoader(Type type, IPropertyHandler propertyHandler, IStatusHandler statusHandler)
        {

            if (type.IsSubclassOf(typeof(BaseDataLoader)))
            {
                var loader = (BaseDataLoader)Activator.CreateInstance(type);
                loader.InitializeHandlers(propertyHandler, statusHandler);
                loader.RunDataLoader();
            }
        }

        private static bool HasValidDllName(Dictionary<string, string> config)
        {
            //TODO - Make sure the file exists as well
            return config["DLL"] == "";
        }

        private static bool SetHandlers(ref IPropertyHandler propertyStore, ref IStatusHandler statusHandler)
        {
            string reportStoreType = "";
            string reportStoreConnectionString = "";
            string porpertyStoreType = "";
            string propertyStoreConnectionString = "";

            try
            {
                IniParser ini = new IniParser("conf.ini");
                reportStoreType = ini.GetSetting("STATUS_REPORT", "REPORT_TO");
                reportStoreConnectionString = ini.GetSetting("STATUS_REPORT", "REPORT_CONNECTION");
                porpertyStoreType = ini.GetSetting("PROPERTY_STORE", "TYPE");
                propertyStoreConnectionString = ini.GetSetting("PROPERTY_STORE", "CONNECTION");
            }
            catch (System.IO.FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            //Setup property store
            //Currently we hardcode this to SQL Server, the infrastructure to reflect on it is in place
            propertyStore = new Aih.DataLoader.Tools.PropertyHandlers.SQLServerPropertyHandler(propertyStoreConnectionString);


            //Setup where to report status to
            //Currently we hardcode this to SQL Server, the infrastructure to reflect on it is in place
            statusHandler = new Aih.DataLoader.Tools.StatusHandlers.SQLServerStatusHandler(reportStoreConnectionString);

            return true;
        }
    }




    internal static class CommandLineParser
    {
        public static Dictionary<string, string> GetConfig(string[] args)
        {
            Dictionary<string, string> config = new Dictionary<string, string>();

            string dllPath = GetValue(args, "-dll");
            string typename = GetValue(args, "-n");
            string path = GetValue(args, "-path");

            config.Add("DLL", dllPath);
            config.Add("TYPENAME", typename);
            config.Add("PATH", path);

            return config;
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

