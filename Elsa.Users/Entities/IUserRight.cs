﻿using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Users.Entities
{
    [Entity]
    public interface IUserRight
    {
        int Id { get; }

        [NVarchar(255, false)]
        string Symbol { get; set; }
    }
}
