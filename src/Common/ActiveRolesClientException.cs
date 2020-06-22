using System;
using System.Collections.Generic;
using System.Text;

namespace OneIdentity.ARSGJitAccess.Common
{
    public class ActiveRolesClientException : Exception
    {
        public ActiveRolesClientException(string message, string objectDn, string attributeName, object attributeValue)
            : base(message + $" objectDn: {objectDn}, attributeName: {attributeName}, attributeValue: {attributeValue}")
        {
        }

        public ActiveRolesClientException(string message, string objectDn, string attributeName, object attributeValue, Exception inner)
            : base(message + $" objectDn: {objectDn}, attributeName: {attributeName}, attributeValue: {attributeValue}", inner)
        {
        }
    }
}
