using System.IO;

namespace Elsa.App.ImportExport
{
    public interface IImportExportModule
    {
        string Title { get; }
        string Description { get; }
        string Uid { get; }
        string Import(Stream inputStream);
        byte[] Export(out string exportFileName);
    }
}
