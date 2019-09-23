using System;
using Elsa.Apps.CommonData.ExcelInterop;
using Elsa.Apps.Invoices.Model;

namespace Elsa.Apps.Invoices
{
    public class InvoiceModelFactory
    {
        private readonly ElsaExcelModelFactory m_excelModelFactory;

        public InvoiceModelFactory(ElsaExcelModelFactory excelModelFactory)
        {
            m_excelModelFactory = excelModelFactory;
        }
        
        public InvoiceModel Create()
        {
            var model = m_excelModelFactory.Setup(new InvoiceModel(), m => !m.IsManufactured);
            
            model.Date = DateTime.Now.ToString(ElsaExcelModelBase.ExcelDateFormat);

            for (var i = 0; i < 5; i++)
            {
                model.Items.Add(new InvoiceItem());
            }

            return model;
        }
    }
}
