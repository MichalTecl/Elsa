using Elsa.Common;
using System;
using System.IO;

namespace Elsa.App.PublicFiles
{
    public interface IPublicFilesHelper
    {
        void Write(string customerName, string fileType, string fileName, Action<StreamWriter> generate);

        FileResult GetFile(string customerName, string fileType);
    }
}
