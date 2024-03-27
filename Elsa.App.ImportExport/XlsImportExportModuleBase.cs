using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XlsSerializer.Core;

namespace Elsa.App.ImportExport
{
    public abstract class XlsImportExportModuleBase<TModel> : IImportExportModule 
        where TModel : new()
    {
        public abstract string Title { get; }

        public abstract string Description { get; }

        public string Uid => this.GetType().FullName;

        public byte[] Export(out string exportFileName)
        {
            var export = ExportData(out exportFileName);
            return XlsxSerializer.Instance.Serialize(export);
        }

        public string Import(Stream inputStream)
        {
            var data = XlsxSerializer.Instance.Deserialize<List<TModel>>(inputStream);
            return ImportData(data);
        }

        protected abstract IEnumerable<TModel> ExportData(out string exportFileName);
        protected abstract string ImportData(List<TModel> data);
    }
}
