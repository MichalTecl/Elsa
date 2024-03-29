﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public interface ISupplierRepository
    {
        ISupplier GetSupplier(int supplierId);

        ISupplier GetSupplier(string supplierName);

        ISupplier SaveSupplier(ISupplier supplier);

        ISupplier WriteSupplier(int? id, Action<ISupplier> populate);

        void DeleteSupplier(int supplierId);

        IEnumerable<ISupplier> GetSuppliers();
    }
}
