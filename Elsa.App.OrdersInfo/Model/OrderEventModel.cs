using System;

namespace Elsa.App.OrdersInfo.Model
{
    public class OrderEventModel
    {
        public int Id { get; set; }
        public DateTime Dt { get; set; }
        public string Text { get; set; }
        public string User { get; set; }
    }
}