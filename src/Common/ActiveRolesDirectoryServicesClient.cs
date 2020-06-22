using System;
using System.DirectoryServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace OneIdentity.ARSGJitAccess.Common
{
    public class ActiveRolesDirectoryServicesClient : IActiveRolesClient
    {
        public ActiveRolesDirectoryServicesClient()
        {
            Username = null;
            Password = null; ;
        }
        
        public ActiveRolesDirectoryServicesClient( string username, string password)
        {
            Username = username;
            Password = password;
        }

        public void SetObjectAttribute(string objectDn, string attributeName, object attributeValue)
        {
            try
            {
                if(Username != null && Password != null)
                {

                }
                using (var directoryEntry = GetDirectoryEntry(objectDn))
                {
                    directoryEntry.Properties[attributeName].Value = attributeValue;
                    directoryEntry.CommitChanges();
                }
            }
            catch (COMException e)
            {
                // ErrorCode E_ADS_CANT_CONVERT_DATATYPE means attribute is not in schema, or that value is wrong type
                if ((uint)e.ErrorCode == 0x8000500C) 
                {
                    if(GetAttributeSchemaOmSyntax(attributeName) == 0)
                    {
                        // attribute does not exist
                        throw new ActiveRolesClientException(
                            $"Attribute does not exist in schema.", objectDn, attributeName, attributeValue, e);
                    }

                    // attribute exists, so value must have been the wrong type
                    throw new ActiveRolesClientException(
                        $"Can not set object attribute because the specified value is the wrong type " +
                        "Make sure the type of the value that you're trying to set matches the omSyntax of the attribute",
                        objectDn, attributeName, attributeValue, e);
                }

                throw CreateActiveRolesClientExceptionFromCOMException( objectDn,  attributeName,  attributeValue, e);
            }
        }

        // Return the oMSyntax for the specified attribute.  Returns 0 if the attribute is not in the schema.
        // Boolean - 1
        // Integer - 2, 10, 65
        // String - 22, 23, 24, 64, etc 
        //  https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-adts/7cda533e-d7a4-4aec-a517-91d02ff4a1aa

        public int GetAttributeSchemaOmSyntax(string attributeName)
        {
            try
            {
                var consolidatedSchema = GetDirectoryEntry("CN=Schema,CN=Application Configuration,CN=Configuration");
                using (var directorySearcher = new DirectorySearcher(consolidatedSchema))
                {
                    directorySearcher.Filter = $"(lDAPDisplayName={ attributeName })";
                    directorySearcher.SearchScope = SearchScope.OneLevel;
                    var searchResult = directorySearcher.FindOne();
                    if (searchResult == null)
                    {
                        return 0;
                    }

                    return (int)searchResult.Properties["omSyntax"][0];
                }
            }
            catch (COMException e)
            {
                throw CreateActiveRolesClientExceptionFromCOMException(null,attributeName,null,e);
            }
        }

         DirectoryEntry GetDirectoryEntry(string objectDn)
        {
            if( Username == null)
            {
                return new DirectoryEntry("EDMS://" + objectDn);
            }

            return new DirectoryEntry("EDMS://" + objectDn, Username, Password);
        }

        ActiveRolesClientException CreateActiveRolesClientExceptionFromCOMException( string objectDn, string attributeName, object attributeValue, COMException cOMException)
        {
            // Error code descriptions determined by trial and error testing.
            string errorCodeStr = $"0x{cOMException.ErrorCode:X}";
            if ((uint)cOMException.ErrorCode == 0x80005000) // E_ADS_BAD_PATHNAME
            {
                return new ActiveRolesClientException("Unable to load the Active Roles ADSI Provider. " +
                    "Please locate the Active Roles ADSI Provider on the Active Roles Installation media " +
                    "and install it on this computer.", objectDn, attributeName, attributeValue, cOMException);
            }
            if ((uint)cOMException.ErrorCode == 0x8000500C) // E_ADS_CANT_CONVERT_DATATYPE
            {
                return new ActiveRolesClientException("Attribute does not exist in schema or attempted to "+
                    "set/modify value using wrong value type", objectDn, attributeName, attributeValue, cOMException);
            }
            if ((uint)cOMException.ErrorCode == 0x80070005) // ERROR_ACCESS_DENIED
            {
                return new ActiveRolesClientException("Access Denied.", objectDn, attributeName, attributeValue, cOMException);
            }
            if ((uint)cOMException.ErrorCode == 0x80041069) // Object not found
            {
                return new ActiveRolesClientException("Object not found.", objectDn, attributeName, attributeValue, cOMException);
            }
            if ((uint)cOMException.ErrorCode == 0x80041452) // Username/Password authentication failed
            {
                return new ActiveRolesClientException("Authentication failed.  Invalid username or password.", objectDn, attributeName, attributeValue, cOMException);
            }
          
            return new ActiveRolesClientException("Unknown COM Error", objectDn, attributeName, attributeValue, cOMException);
        }

        string Username { get; }
        string Password { get; }
    }
}
