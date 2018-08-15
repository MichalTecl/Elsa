using System;

using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core.Impl
{
    public class OrderStatusTranslator : IOrderStatusTranslator
    {
        private static readonly string[] s_translations = new[]
                                                              {
                                                                  "Nová", "Čeká na platbu", "Zaplacena", "Zabalena",
                                                                  "Odeslána", "Vrácena", "Zrušena", "Chyba"
                                                              };


        public string Translate(int statusId)
        {
            statusId--;
            if (statusId < 0 || statusId >= s_translations.Length)
            {
                throw new InvalidOperationException("Invalid OrderStatus Id");
            }

            return s_translations[statusId];
        }

        public string Translate(IOrderStatus status)
        {
            return Translate(status.Id);
        }
    }
}
