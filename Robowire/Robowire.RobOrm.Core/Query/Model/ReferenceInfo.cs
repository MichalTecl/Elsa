using System;

namespace Robowire.RobOrm.Core.Query.Model
{
    public sealed class ReferenceInfo
    {
        public readonly string LeftModelPropertyName;
        public readonly Type LeftEntityType;
        public readonly string LeftKeyColumnName;

        public readonly string RightModelPropertyName;
        public readonly Type RightEntityType;
        public readonly string RightKeyColumnName;

        public ReferenceInfo(string leftModelPropertyName, 
                             Type leftEntityType, 
                             string leftKeyColumnName, 
                             string rightModelPropertyName, 
                             Type rightEntityType, 
                             string rightKeyColumnName)
        { 
            LeftModelPropertyName = leftModelPropertyName;
            LeftEntityType = leftEntityType;
            LeftKeyColumnName = leftKeyColumnName;
            RightModelPropertyName = rightModelPropertyName;
            RightEntityType = rightEntityType;
            RightKeyColumnName = rightKeyColumnName;
        }
    }
}
