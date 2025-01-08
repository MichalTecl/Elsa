using Elsa.Common.EntityComments.Impl;
using Robowire;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Common.EntityComments
{
    public class EntityCommentsRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IEntityCommentsRepository>().Use<EntityCommentRepository>();
            setup.For<IEntityCommentsFacade>().Use<EntityCommentsFacade>();
        }
    }
}
