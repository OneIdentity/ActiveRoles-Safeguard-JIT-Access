using System.Collections.Generic;
using Newtonsoft.Json;
using Topshelf;
using Topshelf.Logging;

using OneIdentity.ARSGJitAccess.Common;
using OneIdentity.SafeguardDotNet.Event;
using System;

namespace OneIdentity.ARSGJitAccess.Service
{
    partial class Service : ServiceControl
    {
        public Service(bool isTest)
        {
            Log = HostLogger.Get(typeof(HostFactory));
            IsTest = isTest;            
        }

        public static List<SafeguardEvent> Events
        {
            get
            {
                return new List<SafeguardEvent>()
                {
                    new SafeguardEvent() { name = "AccessRequestAvailable"},
                    new SafeguardEvent() { name = "AccessRequestCancelled"},
                    new SafeguardEvent() { name = "AccessRequestCheckedIn"},
                    new SafeguardEvent() { name = "AccessRequestClosed"},
                    new SafeguardEvent() { name = "AccessRequestExpired"},
                    new SafeguardEvent() { name = "AccessRequestRevoked"},
                };
            }
        }
        public bool IsTest { get; }

        public bool Start(HostControl hostControl)
        {
            if(ActiveRolesClient == null)
            {
                if (!InitActiveRolesClient())
                {
                    Log.Fatal("Failed to create ActiveRolesClient");
                    return false;
                }
            }

            if (SafeguardClient == null)
            {
                if (!InitSafeguardClient())
                {
                    Log.Fatal("Failed to create SafeguardClient");
                    return false;
                }
            }

            if (IsTest)
            { 
                Log.Info("Test mode enabled.  Stopping service before listening.");
                hostControl.Stop();
                return true;
            }

            // Start listener
            try
            {
                if (Listener == null)
                {
                    Listener = SafeguardClient.GetEventListener();
                }
                foreach (var e in Events)
                {
                    Listener.RegisterEventHandler(e.name, HandleAccessRequestEvent);
                }
                Listener.Start();

                Log.Info("Service Started");
                return true;
            }
            catch(Exception e)
            {
                Log.Fatal(e.Message);
            }

            return false;
        }

        public bool Stop(HostControl hostControl)
        {
            if (IsTest)
            {
                return true;
            }

            if (Listener != null)
            {
                Listener.Stop();
                Log.Info("Service Stopped");
            }

            return true;
        }

        void HandleAccessRequestEvent(string eventName, string eventBody)
        {
            var accessRequestEvent = JsonConvert.DeserializeObject<AccessRequestEvent>(eventBody);
            
            Log.Debug($"Recieved event: {eventName}, AssetId: {accessRequestEvent.AssetId}, AccountId: {accessRequestEvent.AccountId}");
            
            var assetAccount = SafeguardClient.GetAssetAccount(accessRequestEvent.AccountId);
            if (assetAccount.PlatformType == "MicrosoftAD")
            {
                if (eventName == "AccessRequestAvailable")
                {
                    ActiveRolesClient.SetObjectAttribute(assetAccount.DistinguishedName, Config.ARSGJitAccessAttribute, "true");
                    Log.Info($"Grant access for: {assetAccount.DistinguishedName}. Set {Config.ARSGJitAccessAttribute} = true.");
                }
                else
                {
                    ActiveRolesClient.SetObjectAttribute(assetAccount.DistinguishedName, Config.ARSGJitAccessAttribute, "false");
                    Log.Info($"Revoke access for: {assetAccount.DistinguishedName}. Set {Config.ARSGJitAccessAttribute} = false.");
                }
            }
            else
            {
                Log.Debug($"Ignored event for {assetAccount.Name}, because PlatformType is: {assetAccount.PlatformType}");
            }
        }   

        LogWriter Log { get; }
        IActiveRolesClient ActiveRolesClient { get; set; }
        ISafeguardClient SafeguardClient { get; set; }
        ISafeguardEventListener Listener { get; set; }
    }

    class AccessRequestEvent
    {
        public string EventName { get; set; }
        public string AccountId { get; set; }
        public string AssetId { get; set; }
    }
}
