using System;
using System.Collections.Generic;

using Elsa.Invoicing.Core.Contract;

using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Invoicing.Generation
{
    public class GenerationRunner : IInvoiceFormGenerationJob
    {
        private readonly IServiceLocator m_serviceLocator;

        private readonly Type m_contextGeneratorType;

        private readonly List<Type> m_tasks;

        private readonly IDatabase m_database;
        
        public GenerationRunner(IServiceLocator serviceLocator, string name, Type contextGeneratorType, List<Type> tasks)
        {
            Name = name;
            m_serviceLocator = serviceLocator;
            m_contextGeneratorType = contextGeneratorType;
            m_tasks = tasks;
            m_database = serviceLocator.Get<IDatabase>();
        }

        public string Name { get; }

        public void Start()
        {
            while (true)
            {
                var context = new GenerationContext();
                
                var contextGenerator = m_serviceLocator.InstantiateNow<IContextGenerator>(m_contextGeneratorType);

                if (!contextGenerator.FillNextContext(context))
                {
                    return;
                }

                using (var tx = m_database.OpenTransaction())
                {
                    foreach (var taskType in m_tasks)
                    {
                        var taskInstance = m_serviceLocator.InstantiateNow<IGenerationTask>(taskType);
                        taskInstance.Run(context);
                    }

                    tx.Commit();
                }
            }
        }
    }
}
