using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators.PremanufacturedMixtures;
using Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators.PurchasedMaterial;
using Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators.SellableProducts;
using Elsa.Invoicing.Core.Contract;

using Robowire;

namespace Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration
{
    public class InvoiceFormGeneratorFactory : IInvoiceFormGeneratorFactory
    {
        private readonly IServiceLocator m_serviceLocator;

        public InvoiceFormGeneratorFactory(IServiceLocator serviceLocator)
        {
            m_serviceLocator = serviceLocator;
        }

        public IInvoiceFormGenerator Get(string name)
        {
            name = name.ToUpperInvariant().Trim();

            switch (name)
            {
                case "PURCHASED":
                    return m_serviceLocator.InstantiateNow<PurchasedMaterialInvFrmGenerator>();
                case "MIXTURES":
                    return m_serviceLocator.InstantiateNow<PremanufacturedMixturesInvFrmGenerator>();
                case "PRODUCTS":
                    return m_serviceLocator.InstantiateNow<FinalProductRecInvFormGenerator>();
                default:
                    throw new InvalidOperationException($"Invoice form generator '{name}' does not exist");
            }
        }
    }
}
