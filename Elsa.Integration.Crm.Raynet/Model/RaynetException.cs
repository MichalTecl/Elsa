﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.Crm.Raynet.Model
{
    public class RaynetException : Exception
    {
        public RaynetException(string message) : base(message) { }
    }
}
