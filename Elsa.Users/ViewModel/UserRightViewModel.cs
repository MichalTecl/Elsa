using System;
using System.Collections.Generic;

namespace Elsa.Users.ViewModel
{
    public class UserRightViewModel
    {
        public UserRightViewModel(int id, string symbol, string parentRightSymbol)
        {
            Id = id;
            Symbol = symbol;
            ParentRightSymbol = parentRightSymbol;
        }

        public int Id { get; }

        public string Symbol { get; }

        public string ParentRightSymbol { get; }

        public string Description { get; set;  }

        public List<UserRightViewModel> ChildRights { get; } = new List<UserRightViewModel>();

        public bool Assigned { get; set; }

        public void Visit(Action<UserRightViewModel> visitor)
        {
            visitor(this);

            Visit(ChildRights, visitor);
        }

        public static void Visit(IEnumerable<UserRightViewModel> rights, Action<UserRightViewModel> visitor)
        {
            foreach (var ch in rights)
            {
                ch.Visit(visitor);
            }
        }
    }
}
