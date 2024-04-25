using Elsa.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Elsa.App.PublicFiles.Impl
{
    public class PublicFiles : IPublicFilesHelper
    {
        public FileResult GetFile(string cid, string ftype)
        {
            if (!ValidateDirectory(cid) || !ValidateDirectory(ftype))
            {
                return new FileResult("error.txt", Encoding.ASCII.GetBytes("404"));
            }

            var path = $"C:\\Elsa\\PublicFiles\\{cid}\\{ftype}";

            var file = System.IO.Directory.GetFiles(path, "*.public").FirstOrDefault();
            if (file == null)
                return new FileResult("error.txt", Encoding.ASCII.GetBytes("404"));

            return new FileResult(Path.GetFileNameWithoutExtension(file), File.ReadAllBytes(file));
        }

        public void Write(string customerName, string fileType, string fileName, Action<StreamWriter> generate)
        {
            if (!ValidateDirectory(customerName))
                throw new ArgumentException("Invalid customer name");

            if(!ValidateDirectory(fileType))
                throw new ArgumentException("Invalid file type");

            var directory = $"C:\\Elsa\\PublicFiles\\{customerName}\\{fileType}";
            Directory.CreateDirectory(directory);

            var tempFile = Path.Combine(directory, $"{fileName}.temp");
            InRetryLoop(() => { 
                if(File.Exists(tempFile))
                    File.Delete(tempFile);
            });

            using(var writer = new StreamWriter(File.OpenWrite(tempFile), Encoding.UTF8))
            {
                generate(writer);
            }

            var bakFile = Path.Combine(directory, $"{fileName}.bak");
            InRetryLoop(() =>
            {
                if(File.Exists(bakFile))
                    File.Delete(bakFile);
            });

            InRetryLoop(() =>
            {
                if(File.Exists(Path.Combine(directory, $"{fileName}.public")))
                    File.Move(Path.Combine(directory, $"{fileName}.public"), bakFile);
            });

            InRetryLoop(() =>
            {
                File.Move(tempFile, Path.Combine(directory, $"{fileName}.public"));
            });
        }

        private bool ValidateDirectory(string inp)
        {
            if (string.IsNullOrWhiteSpace(inp) || inp.Length > 100)
                return false;

            return inp.ToLower().All(x => "abcdefghijklmnopqrstuvwxyz0123456789".Contains(x));
        }

        private void InRetryLoop(Action action)
        {
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException)
                {
                    if (i == 4)
                    {
                        throw;
                    }

                    Thread.Sleep(100 * i);
                }
            }
        }
    }
}
