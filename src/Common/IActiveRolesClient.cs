using System;
using System.Collections.Generic;
using System.Text;

namespace OneIdentity.ARSGJitAccess.Common
{
    public interface IActiveRolesClient
    {
        void SetObjectAttribute(string objectDn, string attributeName, object attributeValue);
        int GetAttributeSchemaOmSyntax(string attributeName);
    }
}
