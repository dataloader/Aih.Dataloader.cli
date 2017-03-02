using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DataloaderCLI.Services
{
    class ConsoleToFileWriter : TextWriter
    {
        private string _fileName;

        public ConsoleToFileWriter(string fileName)
        {
            _fileName = fileName;
        }

        public override void WriteLine(string line)
        {
            line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + line + Environment.NewLine;

            base.WriteLine(line);
            File.AppendAllText(_fileName, line);
        }

        public override Encoding Encoding
        {
            get
            {
                return System.Text.Encoding.UTF8;
            }
        }
    }
}
