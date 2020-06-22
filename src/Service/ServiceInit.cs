using System;
using System.Linq;
using System.Security.Principal;

using OneIdentity.ARSGJitAccess.Common;
using OneIdentity.SafeguardDotNet;
using Topshelf;
using Topshelf.Logging;

namespace OneIdentity.ARSGJitAccess.Service
{
    partial class Service
    {
        public bool InitActiveRolesClient()
        {
            
            Log.Info("Initializing Active Roles Client");
            if (Config.ARSGJitAccessAttribute == null)
            {
                Log.Error("Bad configuration. ARSGJitProvisioningAttribute must be set");
                return false;
            }
            Log.Info("Using ARSGJitProvisioningAttribute: " + Config.ARSGJitAccessAttribute);

            if (Config.ActiveRolesUsername == null)
            {
                Log.Info("Authenticating to Active Roles using current windows identity: " + WindowsIdentity.GetCurrent().Name);
                ActiveRolesClient = new ActiveRolesDirectoryServicesClient();
            }
            else
            {
                Log.Info("Authenticating to Active Roles using username/password for: " + Config.ActiveRolesUsername);
                ActiveRolesClient = new ActiveRolesDirectoryServicesClient(Config.ActiveRolesUsername, Config.ActiveRolesPassword);
            }

            try
            {
                ActiveRolesClient.GetAttributeSchemaOmSyntax(Config.ARSGJitAccessAttribute);
            }
            catch (ActiveRolesClientException e)
            {
                Log.Error(e.Message);
                return false;
            }

            return true;
        }

        bool InitSafeguardClient()
        {
            Log.Info("Initializing Safeguard Client");

            if (Config.SafeguardAppliance == null)
            {
                Log.Error("Config value for SafeguardAppliance must be set hostname or IP address");
                return false;
            }

            ISafeguardConnection safeguardConnection = null;
            try
            {
                if (Config.SafeguardCertificateThumbprint != null)
                {
                    safeguardConnection = Safeguard.Connect(Config.SafeguardAppliance, Config.SafeguardCertificateThumbprint, 3, true);
                }
                else if (Config.SafeguardCertificateFile != null)
                {
                    safeguardConnection = Safeguard.Connect(Config.SafeguardAppliance, Config.SafeguardCertificateFile, Config.SafeguardCertificatePassword, 3, true);
                }
                else if (Config.SafeguardUsername != null && Config.SafeguardPassword != null)
                {
                    safeguardConnection = Safeguard.Connect(Config.SafeguardAppliance, "Local", Config.SafeguardUsername, Config.SafeguardPassword, 3, true);
                }
                else
                {
                    Log.Error("Missing Safeguard credentials.  Please edit configuration to set one of: SafeguardCertificateFile, SafeguardCertificateThumbprint or SafeguardUsername");
                    return false;
                }
                               
                SafeguardClient = new SafeguardRestApiClient(safeguardConnection);
                var user = SafeguardClient.GetCurrentUser();
                Log.Info("Connected to Safeguard as: " + user.Username);

                if (!user.AdminRoles.Contains("Auditor"))
                {
                    Log.Error($"{user.Username} does not have AdminRole: \"Auditor\"");
                    return false;
                }

                if (user.AdminRoles.Count() > 1)
                {
                    Log.Warn($"{user.Username} has additional Admin Roles. Only \"Auditor\" is required.");
                }

                var eventSubscriptions = SafeguardClient.GetEventSubscriptionsForUser(user);
                if (eventSubscriptions.Where(e => e.Description == "ARSJITAccess").Count() == 0)
                {
                    var eventSubscription = new SafeguardEventSubscription()
                    {
                        UserId = user.Id,
                        Description = "ARSJITAccess",
                        Type = "Signalr",
                        Events = Events
                    };

                    SafeguardClient.CreateEventSubscription(eventSubscription);
                }

                return true;
            }
            catch (SafeguardDotNetException e)
            {
                Log.Error(e.Message);                
            }           
            return false;
        }
    }
}
