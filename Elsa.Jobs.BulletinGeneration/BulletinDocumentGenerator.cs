using System;
using DocGen;
using Robowire;

namespace Elsa.Jobs.BulletinGeneration
{
    public sealed class BulletinDocumentGenerator : DocumentGeneratorBase
    {
        private readonly IServiceLocator _serviceLocator;

        public BulletinDocumentGenerator(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        protected override string Title => "Bulletin — přehled podniku";
        protected override Type[] Chapters => new[] { typeof(ExampleRevenueChapter) };
        protected override object CreateInstance(Type t) => _serviceLocator.InstantiateNow(t);
    }
}
