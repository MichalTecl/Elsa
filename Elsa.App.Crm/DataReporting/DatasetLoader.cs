using Robowire.RobOrm.Core;
using System.Collections.Generic;
using System.Data;

namespace Elsa.App.Crm.DataReporting
{
    public class DatasetLoader
    {
        private readonly IDatabase _database;

        public DatasetLoader(IDatabase database)
        {
            _database = database;
        }

        public DataSet Execute(string procedureName, Dictionary<string, object> parameters) 
        {
            var exec = _database.Sql().Call(procedureName);

            foreach (var par in parameters)
                exec.WithParam(par.Key, par.Value);

            return exec.DataSet();
        }
    }
}
