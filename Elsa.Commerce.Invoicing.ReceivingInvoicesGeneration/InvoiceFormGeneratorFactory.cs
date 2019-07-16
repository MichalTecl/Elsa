using System;
using System.Collections.Generic;

using Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators.PremanufacturedMixtures;
using Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators.PurchasedMaterial;
using Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators.SellableProducts;
using Elsa.Commerce.Invoicing.ReleasingFormsGeneration.Generators;
using Elsa.Invoicing.Core.Contract;

using Robowire;

namespace Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration
{
    public class InvoiceFormGeneratorFactory : IInvoiceFormGeneratorFactory
    {
        private static readonly Dictionary<string, Type> s_types = new Dictionary<string, Type>();
        private readonly IServiceLocator m_serviceLocator;

        static InvoiceFormGeneratorFactory()
        {
            s_types["PURCHASED"] = typeof(PurchasedMaterialInvFrmGenerator);
            s_types["MIXTURES"] = typeof(PremanufacturedMixturesInvFrmGenerator);
            s_types["PRODUCTS"] = typeof(FinalProductRecInvFormGenerator);

            s_types["COMPOSITIONS"] = typeof(BatchCompositionReleaseFormsGenerator);
        }

        public InvoiceFormGeneratorFactory(IServiceLocator serviceLocator)
        {
            m_serviceLocator = serviceLocator;
        }

        public IInvoiceFormGenerator Get(string name)
        {
            name = name.ToUpperInvariant().Trim();

            if (s_types.TryGetValue(name, out var generatorType))
            {
                return m_serviceLocator.InstantiateNow<IInvoiceFormGenerator>(generatorType);
            }

            throw new InvalidOperationException($"Invoice form generator '{name}' does not exist");
        }
    }
}
