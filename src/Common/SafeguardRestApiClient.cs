using System;
using System.Net;
using System.Collections.Generic;

using Newtonsoft.Json;
using OneIdentity.SafeguardDotNet;
using OneIdentity.SafeguardDotNet.Event;
using Newtonsoft.Json.Linq;
using Serilog;

namespace OneIdentity.ARSGJitAccess.Common
{
    public class SafeguardRestApiClient : ISafeguardClient
    {
        public SafeguardRestApiClient(ISafeguardConnection connection)
        {
            Connection = connection;
        }

        public SafeguardAssetAccount GetAssetAccount(string assetAccountId)
        {
            var response = InvokeMethod(Service.Core, Method.Get, $"AssetAccounts/{assetAccountId}");
            return JsonConvert.DeserializeObject<SafeguardAssetAccount>(response);
        }

        public SafeguardUser GetCurrentUser()
        {
            var response = InvokeMethod(Service.Core, Method.Get, "Me");
            return JsonConvert.DeserializeObject<SafeguardUser>(response);
        }

        public List<SafeguardEventSubscription> GetEventSubscriptionsForUser(SafeguardUser user)
        {
            var parameters = new Dictionary<string, string>()
            {
                {"filter",$"UserId eq {user.Id}"}
            };
            var response = InvokeMethod(Service.Core, Method.Get, "EventSubscribers", null, parameters);
            return JsonConvert.DeserializeObject<List<SafeguardEventSubscription>>(response);
        }

        public void CreateEventSubscription(SafeguardEventSubscription eventSubscription)
        {
            InvokeMethod(Service.Core, Method.Post, "EventSubscribers", JsonConvert.SerializeObject(eventSubscription));
        }

        public ISafeguardEventListener GetEventListener()
        {
            return Connection.GetPersistentEventListener();
        }
    
        public static bool ValidateSafeguardApplianceAddress(string addr, out JObject response)
        {
            response = null;

            if (!IPAddress.TryParse(addr, out var IpAddr) && !Uri.IsWellFormedUriString(addr, UriKind.Absolute))
                return false;
            try
            {
                var conn = Safeguard.Connect(addr, 3, true);
                var resp = conn.InvokeMethod(SafeguardDotNet.Service.Appliance, Method.Get, "ApplianceStatus");
                var jResp = JObject.Parse(resp);
                response = jResp;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to validate Safeguard Appliance Address {addr}: {ex.Message}");
            }

            return false;
        }

        private string InvokeMethod(Service service, Method method, string endpoint, string body = null, IDictionary<string, string> parameters = null, IDictionary<string, string> additionalHeaders = null, TimeSpan? timeout=null)
        {
            if (Connection.GetAccessTokenLifetimeRemaining() <= 0)
            {
                Log.Information("Access Token Expired. Re-authenticating to Safeguard.");
                Connection.RefreshAccessToken();
            }
            return Connection.InvokeMethod(service, method, endpoint, body, parameters, additionalHeaders, timeout);            
        }

        ISafeguardConnection Connection
        {
            get;
        }
    }    
}
