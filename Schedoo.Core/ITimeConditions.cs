using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schedoo.Core
{
    public interface ITimeConditions
    {
        bool NowIsBetween(int minHour, int maxHour);
    }
}
