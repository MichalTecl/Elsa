﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.EditorBuilder.Internal
{
    internal interface ICanRender
    {
        void Render(StringBuilder target);
    }
}
