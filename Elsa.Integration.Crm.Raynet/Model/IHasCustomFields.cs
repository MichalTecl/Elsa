using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Integration.Crm.Raynet.Model
{
    public interface IHasCustomFields
    {
        Dictionary<string, object> CustomFields { get; }

        string CustomFieldsCategory { get; }
    }    
}
