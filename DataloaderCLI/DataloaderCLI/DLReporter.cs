using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Aih.DataLoader;
using Aih.DataLoader.Interfaces;
using DataloaderCLI.Models;

namespace DataloaderCLI
{
    class DLReporter
    {
        public List<DataLoaderInfo> ReportDLs()
        {
            List<DataLoaderInfo> infos = new List<DataLoaderInfo>();

            //TODO: Remove hardcoding
            string rootPath = Environment.CurrentDirectory + @"\DataLoaders\";

            string[] subfolders = Directory.GetDirectories(rootPath);

            foreach(var folder in subfolders)
            {
                var dlls = Directory.EnumerateFiles(folder, "*.dll");

                foreach (var dll in dlls)
                {
                    Assembly plugin = Assembly.LoadFile(dll);
                    Type[] types = plugin.GetTypes();

                    foreach (var type in types)
                    {
                        if (type.IsSubclassOf(typeof(BaseDataLoader)))
                        {
                            DataLoaderInfo inf = new DataLoaderInfo();
                            inf.ContainerName = plugin.GetName().Name;
                            inf.ContainerFileFullPath = dll;
                            inf.Name = type.Name;
                            inf.Version = plugin.GetName().Version.ToString();
                            infos.Add(inf);
                        }
                    }

                }
            }
            return infos;
        }
    }
}
