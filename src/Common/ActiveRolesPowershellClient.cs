using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace OneIdentity.ARSGJitProvisioning.Common
{
    internal class ActiveRolesPowershellClient : IActiveRolesClient
    {
        public void SetObjectAttribute(string objectDn, string attributeName, string attributeValue)
        {
            string command = "set-QADObject";
            var parameters = new List<string> 
            {
                $"-identity {objectDn}",
                $"-objectattributes @{{{attributeName}='{attributeValue}'}}"
            };

            ExecuteCommand(command, parameters);       
        }

        public void ExecuteCommand(string command, List<string> parameters)
        {
            PowerShell ps = PowerShell.Create();
            ps.AddCommand(command);
            ps.AddParameters(parameters);
            try
            {
                var res = ps.Invoke();
                if (ps.HadErrors)
                {
                    throw new Exception("Fail");
                }                 
            }
            catch (Exception ex)
            {
                throw new Exception("Failed Harder", ex);
            }


        }
    }
}
