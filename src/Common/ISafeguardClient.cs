using Newtonsoft.Json;
using OneIdentity.SafeguardDotNet.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace OneIdentity.ARSGJitAccess.Common
{
    public interface ISafeguardClient
    {
        SafeguardAssetAccount GetAssetAccount(string assetAccountId);
        SafeguardUser GetCurrentUser();
        List<SafeguardEventSubscription> GetEventSubscriptionsForUser(SafeguardUser user);
        void CreateEventSubscription(SafeguardEventSubscription eventSubscription);
        ISafeguardEventListener GetEventListener();
    }

    public class SafeguardAssetAccount
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DomainName { get; set; }
        public string DistinguishedName { get; set; }
        public string PlatformType { get; set; }
    }

    public class SafeguardUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public List<string> AdminRoles { get; set; }
    }

    public class SafeguardEventSubscription
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public int UserId { get; set; }
        [JsonProperty("Subscriptions")]
        public List<SafeguardEvent> Events { get; set; }
    }

    public class SafeguardEvent
    {
        public string name { get; set; }
    }
}
