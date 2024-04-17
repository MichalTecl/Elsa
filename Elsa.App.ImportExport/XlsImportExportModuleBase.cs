using OfficeOpenXml;
using Robowire.RobOrm.Core;
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
        private readonly IDatabase m_database;

        protected XlsImportExportModuleBase(IDatabase database)
        {
            m_database = database;
        }

        public abstract string Title { get; }

        public abstract string Description { get; }

        public string Uid => this.GetType().FullName;

        public byte[] Export(out string exportFileName)
        {
            var export = ExportData(out exportFileName, m_database);
            return XlsxSerializer.Instance.Serialize(export);
        }

        public string Import(Stream inputStream)
        {
            using (var ep = new ExcelPackage(inputStream))
            {
                var data = XlsxSerializer.Instance.Deserialize<List<TModel>>(ep);

                string res = null;

                using (var tx = m_database.OpenTransaction())
                {
                    res = ImportDataInTransaction(data, m_database, tx);
                    tx.Commit();
                }

                return res;
            }            
        }

        protected abstract List<TModel> ExportData(out string exportFileName, IDatabase db);
        protected abstract string ImportDataInTransaction(List<TModel> data, IDatabase db, ITransaction tx);
    }
}
