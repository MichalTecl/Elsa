using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elsa.Integration.PaymentSystems.Fio.Internal
{
    public class TransactionModel : Dictionary<string, TransactionPropertyModel>
    {
        private static readonly Dictionary<Type, Func<string, object>> s_casts = new Dictionary<Type, Func<string, object>>();

        static TransactionModel()
        {
            s_casts.Add(typeof(string), s => s);
            s_casts.Add(typeof(decimal), s => decimal.Parse(s, System.Globalization.NumberStyles.Any));
            s_casts.Add(typeof(DateTime), s => DateTime.Parse(s));
        }

        private T GetValue<T>(TransactionPropertyModel property)
        {
            Func<string, object> converter;
            if(!s_casts.TryGetValue(typeof(T), out converter))
            {
                throw new Exception($"Chyba cteni vlastnosti transacke FIO. {property.Name} = {property.Value} nelze prevest na {typeof(T).Name}");
            }

            try
            {
                return (T)converter(property.Value);
            }
            catch(Exception e)
            {
                throw new Exception($"Chyba \"{e.Message}\" pri prevodu {property.Name}={property.Value} na {typeof(T).Name}. Zaznam transakce: {this}", e);
            }
        }

        public T GetValue<T>(string propertyName)
        {
            var property = Values.FirstOrDefault(v => v.Name == propertyName);

            if (property == null)
            {
                throw new Exception($"Transakce FIO neobsahuje vlastnost s nazvem \"{propertyName}\"");
            }

            return GetValue<T>(property);
        }   

        public T GetValue<T>(int id, bool useDefault = false, T defaultValue = default(T))
        {
            var property = Values.Where(i => i != null).FirstOrDefault(i => i.Id == id);

            if (property == null)
            {
                if (useDefault)
                {
                    return defaultValue;
                }

                throw new Exception($"Transakce FIO neobsahuje vlastnost s id = {id}");
            }

            return GetValue<T>(property);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach(var kv in this.Where(k => k.Value?.Value != null))
            {
                sb.Append(kv.Value.Name).Append(":").Append(kv.Value.Value).Append("|");
            }

            return sb.ToString();
        }
    }
}
