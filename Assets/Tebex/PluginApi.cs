using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tebex.Common;
using Tebex.Plugin;

#nullable enable

namespace Tebex
{
    public class PluginApi
    {
        public static readonly string PluginApiBase = "https://plugin.tebex.io/";
        private PluginAdapter Adapter { get; set; }
        private string SecretKey { get; set; }

#pragma warning disable CS8618, CS9264
        private PluginApi() { } // properties assigned in Initialize()
#pragma warning restore CS8618, CS9264

        public static PluginApi Initialize(PluginAdapter pluginAdapter, string secretKey)
        {
            var api = new PluginApi
            {
                Adapter = pluginAdapter,
                SecretKey = secretKey
            };
            return api;
        }

        private Task Send<T>(string endpoint, string requestBody, HttpVerb method, Action<T> onSuccess, Action<PluginApiError>? onPluginApiError, Action<ServerError>? onServerError)
        {
            onServerError ??= Adapter.DefaultServerError;
            onPluginApiError ??= Adapter.DefaultPluginError;

            return Adapter.Send<T>(SecretKey, PluginApiBase + endpoint, requestBody, method, (code, responseBody) =>
            {
                try
                {
                    if (code == 204) // No content
                    {
                        onSuccess.Invoke(default!);
                    }

                    var jsonObj = Json.DeserializeObject<T>(responseBody);
                    if (jsonObj == null)
                    {
                        throw new Exception("Response body expected JSON, but was null");
                    }

                    onSuccess.Invoke(jsonObj);
                }
                catch (Exception e)
                {
                    onServerError.Invoke(new ServerError(code, e.Message + " | " + requestBody));
                }
            }, onPluginApiError, onServerError);
        }

        public Task SendJoinEvents(List<JoinEvent> events, Action<string> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send("events", Json.SerializeObject(events), HttpVerb.POST, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetInformation(Action<Store> storeInfo, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send("information", "", HttpVerb.GET, storeInfo, onPluginApiError, onServerError);
        }

        public Task GetCommandQueue(Action<CommandQueueResponse> onSuccess, Action<PluginApiError> onPluginApiError,
            Action<ServerError> onServerError)
        {
            return Send("queue", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetOfflineCommands(Action<OfflineCommandsResponse> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send("queue/offline-commands", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetOnlineCommands(int playerId, Action<OnlineCommandsResponse> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send($"queue/online-commands/{playerId}", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task DeleteCommands(int[] ids, Action<EmptyResponse> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            var payload = new DeleteCommandsPayload { ids = ids };
            return Send("queue", Json.SerializeObject(payload), HttpVerb.DELETE, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetListings(Action<ListingsResponse> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send("listing", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetAllPackages(bool verbose, Action<List<Package>> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send(verbose ? "packages?verbose=true" : "packages", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetPackage(int packageId, Action<Package> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send($"package/{packageId}", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetAllCommunityGoals(Action<List<CommunityGoal>> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send("community_goals", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetCommunityGoal(int goalId, Action<CommunityGoal> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send($"community_goals/{goalId}", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetAllPayments(int limit, Action<List<PaymentDetails>> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            if (limit <= 0) limit = 1;
            if (limit > 100) limit = 100;
            return Send($"payments?limit={limit}", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetAllPaymentsPaginated(int pageNumber, Action<PaginatedPaymentInfo> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send($"payments?paged={pageNumber}", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetPayment(string transactionId, Action<PaymentDetails> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send($"payments/{transactionId}", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task CreateCheckoutUrl(int packageId, string username, Action<CheckoutResponse> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            var payload = new CreateCheckoutPayload { package_id = packageId, username = username };
            return Send("checkout", Json.SerializeObject(payload), HttpVerb.POST, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetAllGiftCards(Action<WrappedGiftCards> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send("gift-cards", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetGiftCard(int giftCardId, Action<WrappedGiftCard> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send($"gift-cards/{giftCardId}", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task CreateGiftCard(DateTime expiresAt, string note, int amount, Action<GiftCard> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            var payload = new CreateGiftCardPayload { expires_at = expiresAt, note = note, amount = amount };
            return Send("gift-cards", Json.SerializeObject(payload), HttpVerb.POST, onSuccess, onPluginApiError, onServerError);
        }

        public Task VoidGiftCard(int giftCardId, Action<WrappedGiftCard> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send($"gift-cards/{giftCardId}", "", HttpVerb.DELETE, onSuccess, onPluginApiError, onServerError);
        }

        public Task TopUpGiftCard(int giftCardId, double amount, Action<WrappedGiftCard> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            var payload = new TopUpGiftCardPayload { amount = $"{amount}" };
            return Send($"gift-cards/{giftCardId}", Json.SerializeObject(payload), HttpVerb.PUT, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetAllCoupons(Action<CouponPage> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send("coupons", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetCouponById(string couponId, Action<WrappedCoupon> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send($"coupons/{couponId}", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetAllBans(Action<WrappedBans> onSuccess, Action<PluginApiError> onPluginApiError,
            Action<ServerError> onServerError)
        {
            return Send("bans", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task CreateBan(string reason, string ip, string userId, Action<WrappedBan> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            var payload = new CreateBanPayload { reason = reason, ip = ip, user = userId };
            return Send("bans", Json.SerializeObject(payload), HttpVerb.POST, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetAllSales(Action<WrappedSales> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send("sales", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task Lookup(string uuidOrUsername, Action<UserLookupResponse> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send($"user/{uuidOrUsername}", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetActivePackagesForCustomer(string playerId, Action<List<ActivePackage>> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send($"player/{playerId}/packages", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }

        public Task GetActivePackageById(string userId, int packageId, Action<List<ActivePackage>> onSuccess, Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return Send($"player/{userId}/packages?package={packageId}", "", HttpVerb.GET, onSuccess, onPluginApiError, onServerError);
        }
    }
}
