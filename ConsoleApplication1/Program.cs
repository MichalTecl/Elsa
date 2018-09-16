using Elsa.Core.Entities.Commerce;

using Robowire;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new Container();
            ElsaDbInstaller.Initialize(container);
        }
    }
}
