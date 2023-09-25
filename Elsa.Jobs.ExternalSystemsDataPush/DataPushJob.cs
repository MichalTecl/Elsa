using System;
using Elsa.Core.Entities.Commerce.Crm;
using Elsa.Jobs.Common;
using Elsa.Jobs.Common.EntityChangeProcessing;
using Elsa.Jobs.ExternalSystemsDataPush.ChangeProcessors;
using Robowire;
using Elsa.Jobs.ExternalSystemsDataPush.Model;

namespace Elsa.Jobs.ExternalSystemsDataPush
{
    public class DataPushJob : IExecutableJob
    {
        private readonly IChangeProcessorHostFactory _changeProcessorHostFactory;
        private readonly IServiceLocator _services;

        public DataPushJob(IChangeProcessorHostFactory changeProcessorHostFactory, IServiceLocator services)
        {
            _changeProcessorHostFactory = changeProcessorHostFactory;
            _services = services;
        }

        public void Run(string customDataJson)
        {
            Run<ICustomer, RayNetDistributorsPush>();
            //Run<OrderExportModel, RayNetOrdersPush>();
        }

        private void Run<TEntity, TProcessor>() where TProcessor : IEntityChangeProcessor<TEntity>
        {
            var prc = _services.InstantiateNow<TProcessor>();
            var host = _changeProcessorHostFactory.Get<TEntity>();
            host.Execute(prc);
        }
    }
}
