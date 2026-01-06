using Elsa.Core.Entities.Commerce.Commerce;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Shipment
{
    public interface IShipmentRequestDocumentGenerator
    {
        string Symbol { get; }
        void Generate(List<IPurchaseOrder> orderList, StreamWriter streamWriter, out string fileName);
    }
}
