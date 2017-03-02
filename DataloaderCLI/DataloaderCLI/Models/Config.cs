using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataloaderCLI.Models
{
    class Config
    {
        public Config()
        {
            DllName = "";
            TypeName = new List<string>();
            //Path = "";
            Report = false;
        }

        public string DllName { get; set; }     //Container name
        public List<string> TypeName { get; set; }
        //public string Path { get; set; }
        public bool Report { get; set; }
    }
}
