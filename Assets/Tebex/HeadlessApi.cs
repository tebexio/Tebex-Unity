using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tebex.Common;
using Tebex.Headless;
using UnityEngine;

#nullable enable

namespace Tebex
{
    /// <summary>
    /// Tebex Headless API allows integration of your store directly into your own frontend or in-game.
    /// </summary>
    public class HeadlessApi
    {
        #if UNITY_5_3_OR_NEWER
        private static HeadlessAdapter DefaultAdapter => new UnityHeadlessAdapter();
        #else
        private static HeadlessAdapter DefaultAdapter => new DefaultHeadlessAdapter();
        #endif
        
        private static readonly string HeadlessApiBase = "https://headless.tebex.io/api/";
        internal string PublicToken { get; set; } = string.Empty;
        internal string PrivateKey { get; set; } = string.Empty;
        
        private static HeadlessApi? _instance;
        private HeadlessAdapter _adapter;
        private Webstore? _store;

#pragma warning disable CS8618, CS9264
        private HeadlessApi() {} // non-nullable fields are assigned in Initialize()
#pragma warning restore CS8618, CS9264

        public static HeadlessApi GetInstance(HeadlessAdapter? adapter = null, string publicToken = "")
        {
            if (_instance == null)
            {
                _instance = Create(adapter ?? DefaultAdapter, publicToken);
            }
            return _instance;
        }
        /// <summary>
        /// Initializes a new instance of the HeadlessApi class with the provided adapter and public token.
        /// </summary>
        /// <param name="adapter">Use the DefaultHeadlessAdapter if System libraries will suffice.</param>
        /// <param name="publicToken">The public API token for authentication with the Tebex store.</param>
        /// <returns>A new instance of the HeadlessApi class configured with the provided adapter and public token.</returns>
        private static HeadlessApi Create(HeadlessAdapter adapter, string publicToken)
        {
            var api = new HeadlessApi
            {
                _adapter = adapter
            };
            api.SetStoreToken(publicToken);
            adapter.SetApiInstance(api);
            return api;
        }
        
        /// <summary>
        /// Sets the currently configured public token used for API requests
        /// </summary>
        /// <param name="token"></param>
        public void SetStoreToken(string token)
        {
            PublicToken = token;
        }

        /// <summary>
        /// Sets the current private key used for authenticated API requests.
        /// </summary>
        /// <param name="key"></param>
        public void SetPrivateKey(string key)
        {
            PrivateKey = key;
        }
        
        /// <summary>
        /// Sends an HTTP request to a specified endpoint using the given parameters and invokes appropriate callbacks based on the response.
        /// </summary>
        /// <param name="endpoint">The API endpoint to which the request will be sent.</param>
        /// <param name="body">The request payload in JSON format, if applicable.</param>
        /// <param name="method">The HTTP verb (GET, POST, PUT, etc.) to be used for the request.</param>
        /// <param name="onSuccess">The callback invoked on a successful response, containing the HTTP status code and response body.</param>
        /// <param name="onHeadlessApiError">The callback invoked when a headless API-specific error occurs.</param>
        /// <param name="onServerError">The callback invoked when an unknown/server error occurs.</param>
        /// <param name="authenticated">True if we should use an authenticated call (HTTP Basic, will require public key and private key to be set</param>
        /// <typeparam name="TReturnType">The type of the object to be deserialized from the response.</typeparam>
        /// <returns>A task representing the asynchronous operation of sending the HTTP request.</returns>
        private Task Send<TReturnType>(string endpoint, string body, HttpVerb method, ApiSuccessCallback onSuccess,
            Action<HeadlessApiError> onHeadlessApiError, Action<ServerError> onServerError, bool authenticated = false)
        {
            return _adapter.Send<TReturnType>(HeadlessApiBase + endpoint, body, method, onSuccess, onHeadlessApiError,
                onServerError, authenticated);
        }
        
        public Webstore GetWebstore()
        {
            Webstore? webstore = null;
            GetWebstoreAsync(webstoreData => webstore = webstoreData, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return webstore!;
        }

        /// <summary>
        /// Retrieves a list of all packages available in the webstore.
        /// </summary>
        public List<Package> GetAllPackages()
        {
            List<Package>? packages = null;
            GetAllPackagesAsync(wrappedPackages => packages = wrappedPackages.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return packages!;
        }

        /// <summary>
        /// Retrieves a specific package by its ID from the webstore.
        /// </summary>
        public Package GetPackage(int packageId)
        {
            Package? package = null;
            GetPackageAsync(packageId, wrappedPackage => package = wrappedPackage.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return package!;
        }

        public Package GetPackage(int packageId, string basketIdent)
        {
            Package? package = null;
            GetPackageAsync(packageId, basketIdent, wrappedPackage => package = wrappedPackage.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return package!;
        }
        
        public Package GetPackage(int packageId, string ipAddress, string basketIdent)
        {
            Package? package = null;
            GetPackageAsync(packageId, ipAddress, basketIdent, wrappedPackage => package = wrappedPackage.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return package!;        }

        public List<Category> GetAllCategories()
        {
            List<Category>? categories = null;
            GetAllCategoriesAsync(wrappedCategories => categories = wrappedCategories.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return categories!;
        }
        
        public List<Category> GetAllCategoriesIncludingPackages()
        {
            List<Category> categories = new List<Category>();
            GetAllCategoriesIncludingPackagesAsync(wrappedCategories => categories = wrappedCategories.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return categories;
        }
        
        public Category GetCategory(int categoryId)
        {
            Category? category = null;
            GetCategoryAsync(categoryId, wrappedCategory => category = wrappedCategory.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return category!;
        }
        
        public Category GetCategoryIncludingPackages(int categoryId)
        {
            Category? category = null;
            GetCategoryIncludingPackagesAsync(categoryId, wrappedCategory => category = wrappedCategory.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return category!;
        }

        public List<Category> GetTieredCategories()
        {
            List<Category> categories = GetAllCategories();
            List<Category> tieredCategories = new List<Category>();
            foreach(Category category in categories)
            {
                if (category.tiered)
                {
                    tieredCategories.Add(category);    
                }
            }

            return tieredCategories;
        }
        
        public List<Category> GetTieredCategoriesForUser(long usernameId)
        {
            List<Category> tieredCategories = GetTieredCategories();
            GetTieredCategoriesForUserAsync(usernameId, categories => tieredCategories = categories.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return tieredCategories;
        }

        public bool UpdateTier(int tierId, int packageId)
        {
            var result = false;
            UpdateTierAsync(tierId, packageId, response => result = response.success, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return result;
        }
        
        public Basket GetBasket(string basketIdent)
        {
            Basket? basket = null;
            GetBasketAsync(basketIdent, wrappedBasket => basket = wrappedBasket.data, _adapter.LogApiError, _adapter.LogServerError ).Wait();
            return basket!;
        }
        public Basket CreateBasket(CreateBasketPayload basketPayload)
        {
            Basket? basket = null;
            CreateBasketAsync(basketPayload, wrappedBasket => basket = wrappedBasket.data, _adapter.LogApiError, _adapter.LogServerError ).Wait();
            return basket!;
        }
        public Basket AddPackageToBasket(string basketIdent, AddPackagePayload payload)
        {
            Basket? basket = null;
            AddPackageToBasketAsync(basketIdent, payload, wrappedBasket => basket = wrappedBasket.data, _adapter.LogApiError, _adapter.LogServerError ).Wait();
            return basket!;
        }
        public Basket UpdatePackageQuantity(string basketIdent, int packageId, int newQuantity)
        {
            Basket? basket = null;
            UpdatePackageQuantityAsync(basketIdent, packageId, newQuantity, wrappedBasket => basket = wrappedBasket.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return basket!;
        }
        public List<BasketAuthLink> GetBasketAuthLinks(string basketIdent, string returnUrl)
        {
            List<BasketAuthLink>? basketAuthLinks = null;
            GetBasketAuthLinksAsync(basketIdent, returnUrl, wrappedBasketLinks => basketAuthLinks = wrappedBasketLinks.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return basketAuthLinks!;
        }
        public Basket RemovePackageFromBasket(string basketIdent, int packageId)
        {
            Basket? basket = null;
            RemovePackageFromBasketAsync(basketIdent, packageId, wrappedBasket => basket = wrappedBasket.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return basket!;
        }
        
        public void ApplyCreatorCode(string basketIdent, string creatorCode)
        {
            ApplyCreatorCodeAsync(basketIdent, creatorCode, empty => { }, _adapter.LogApiError, _adapter.LogServerError).Wait();
        }
        
        public void RemoveCreatorCode(string basketIdent)
        {
            RemoveCreatorCodeAsync(basketIdent, empty => { }, _adapter.LogApiError, _adapter.LogServerError).Wait();
        }
        
        public Basket ApplyGiftCard(string basketIdent, string giftCardCode)
        {
            Basket? basket = null;
            ApplyGiftCardAsync(basketIdent, giftCardCode, wrappedBasket => basket = wrappedBasket.data, _adapter.LogApiError, _adapter.LogServerError).Wait();
            return basket!;
        }
        public void RemoveGiftCard(string basketIdent, string giftCardCode)
        {
            RemoveGiftCardAsync(basketIdent, giftCardCode, empty => {}, _adapter.LogApiError, _adapter.LogServerError).Wait();
        }
        
        public void ApplyCoupon(string basketIdent, string couponCode)
        {
            ApplyCouponAsync(basketIdent, couponCode, empty => { }, _adapter.LogApiError, _adapter.LogServerError).Wait();            
        }
        
        public void RemoveCoupon(string basketIdent, string couponCode)
        {
            RemoveCouponAsync(basketIdent, couponCode, empty => { }, _adapter.LogApiError, _adapter.LogServerError);
        }
        
        public Task GetBasketAsync(string basketIdent, Action<WrappedBasket> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetRequestAsync("accounts/" + PublicToken + "/baskets/" + basketIdent, onSuccess, onApiError, onServerError);
        }
        public Task CreateBasketAsync(CreateBasketPayload basketPayload, Action<WrappedBasket> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return PostRequestAsync("accounts/" + PublicToken + "/baskets", Json.SerializeObject(basketPayload), onSuccess, onApiError, onServerError);
        }
        public Task AddPackageToBasketAsync(string basketIdent, AddPackagePayload payload, Action<WrappedBasket> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return PostRequestAsync("baskets/" + basketIdent + "/packages", Json.SerializeObject(payload), onSuccess, onApiError, onServerError);            
        }
        public Task UpdatePackageQuantityAsync(string basketIdent, int packageId, int newQuantity, Action<WrappedBasket> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            var payload = new PackageQuantityPayload(newQuantity);
            return PutRequestAsync("baskets/" + basketIdent + "/packages/" + packageId, Json.SerializeObject(payload), onSuccess, onApiError, onServerError);
        }
        public Task GetBasketAuthLinksAsync(string basketIdent, string returnUrl, Action<WrappedBasketLinks> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetRequestAsync("accounts/" + PublicToken + "/baskets/" + basketIdent + "/auth?returnUrl=" + returnUrl, onSuccess, onApiError, onServerError);
        }
        public Task RemovePackageFromBasketAsync(string basketIdent, int packageId, Action<WrappedBasket> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            var payload = new PackageRemovePayload(packageId);
            return PostRequestAsync("baskets/" + basketIdent + "/packages/remove", Json.SerializeObject(payload), onSuccess, onApiError, onServerError);            
        }
        public Task ApplyCreatorCodeAsync(string basketIdent, string creatorCode, Action<EmptyResponse> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            var payload = new CreatorCode(creatorCode);
            return PostRequestAsync("accounts/" + PublicToken + "/baskets/" + basketIdent + "/creator-codes", Json.SerializeObject(payload), onSuccess, onApiError, onServerError);
        }
        public Task RemoveCreatorCodeAsync(string basketIdent, Action<EmptyResponse> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return PostRequestAsync("accounts/" + PublicToken + "/baskets/" + basketIdent + "/creator-codes/remove", "", onSuccess, onApiError, onServerError);
        }
        public Task ApplyGiftCardAsync(string basketIdent, string giftCardCode, Action<WrappedBasket> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            var giftCard = new GiftCard(giftCardCode);
            return PostRequestAsync("accounts/" + PublicToken + "/baskets/" + basketIdent + "/giftcards", Json.SerializeObject(giftCard), onSuccess, onApiError, onServerError);
        }
        public Task RemoveGiftCardAsync(string basketIdent, string giftCardCode, Action<EmptyResponse> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            var giftCard = new GiftCard(giftCardCode);
            return PostRequestAsync("accounts/" + PublicToken + "/baskets/" + basketIdent + "/giftcards/remove", Json.SerializeObject(giftCard), onSuccess, onApiError, onServerError);
        }
        public Task ApplyCouponAsync(string basketIdent, string couponCode, Action<EmptyResponse> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            var coupon = new BasketCoupon(couponCode);
            return PostRequestAsync("accounts/" + PublicToken + "/baskets/" + basketIdent + "/coupons", Json.SerializeObject(coupon), onSuccess, onApiError, onServerError);
        }
        public Task RemoveCouponAsync(string basketIdent, string couponCode, Action<EmptyResponse> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            var coupon = new BasketCoupon(couponCode);
            return PostRequestAsync("accounts/" + PublicToken + "/baskets/" + basketIdent + "/coupons/remove", Json.SerializeObject(coupon), onSuccess, onApiError, onServerError);
        }
        /// <summary>
        /// Retrieves the webstore associated with the provided public token.
        /// </summary>
        /// <param name="onSuccess">The action to execute when the webstore is successfully retrieved. Provides an instance of <see cref="Webstore"/>.</param>
        /// <param name="onApiError">The action to execute when an API error occurs. Provides an instance of <see cref="HeadlessApiError"/>.</param>
        /// <param name="onServerError">The action to execute when a server error occurs. Provides an instance of <see cref="ServerError"/>.</param>
        /// <returns>A task representing the asynchronous operation of retrieving the webstore.</returns>
        public Task GetWebstoreAsync(Action<Webstore> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetRequestAsync("accounts/" + PublicToken, onSuccess, onApiError, onServerError);
        }

        /// <summary>
        /// Retrieves a list of all packages available in the webstore.
        /// </summary>
        /// <param name="onSuccess">The action to execute on success. Provides an instance of <see cref="WrappedPackages"/>.</param>
        /// <param name="onApiError">The action to execute when an API error occurs. Provides an instance of <see cref="HeadlessApiError"/>.</param>
        /// <param name="onServerError">The action to execute when a server error occurs. Provides an instance of <see cref="ServerError"/>.</param>
        /// <returns>An asynchronous operation representing the process of retrieving all packages.</returns>
        public Task GetAllPackagesAsync(Action<WrappedPackages> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetRequestAsync("accounts/" + PublicToken + "/packages", onSuccess, onApiError, onServerError);
        }

        /// <summary>
        /// Retrieves a specific package by its ID from the webstore.
        /// </summary>
        /// <param name="packageId">The unique identifier of the package to retrieve.</param>
        /// <param name="onSuccess">The action to execute on successProvides an instance of <see cref="WrappedPackage"/>.</param>
        /// <param name="onApiError">The action to execute when an API error occurs. Provides an instance of <see cref="HeadlessApiError"/>.</param>
        /// <param name="onServerError">The action to execute when a server error occurs. Provides an instance of <see cref="ServerError"/>.</param>
        /// <returns>A Task representing the asynchronous operation of retrieving the package information.</returns>
        public Task GetPackageAsync(int packageId, Action<WrappedPackage> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetRequestAsync("accounts/" + PublicToken + "/packages/" + packageId, onSuccess, onApiError, onServerError);
        }

        public Task GetPackageAsync(int packageId, string basketIdent, Action<WrappedPackage> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetRequestAsync("accounts/" + PublicToken + "/packages/" + packageId + "?basketIdent=" + basketIdent, onSuccess, onApiError, onServerError);
        }
        
        public Task GetPackageAsync(int packageId, string ipAddress, string basketIdent, Action<WrappedPackage> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetRequestAsync("accounts/" + PublicToken + "/packages/" + packageId + "?basketIdent=" + basketIdent + "&ipAddress=" + ipAddress, onSuccess, onApiError, onServerError);
        }

        public Task GetAllCategoriesAsync(Action<WrappedCategories> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetRequestAsync("accounts/" + PublicToken + "/categories", onSuccess, onApiError, onServerError);
        }
        
        public Task GetAllCategoriesIncludingPackagesAsync(Action<WrappedCategories> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetRequestAsync("accounts/" + PublicToken + "/categories?includePackages=1", onSuccess, onApiError, onServerError);
        }
        
        public Task GetCategoryAsync(int categoryId, Action<WrappedCategory> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetRequestAsync("accounts/" + PublicToken + "/categories/" + categoryId, onSuccess, onApiError, onServerError);
        }
        
        public Task GetCategoryIncludingPackagesAsync(int categoryId, Action<WrappedCategory> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetRequestAsync("accounts/" + PublicToken + "/categories/" + categoryId + "?includePackages=1", onSuccess, onApiError, onServerError);
        }

        public Task GetTieredCategoriesAsync(Action<List<Category>> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetAllCategoriesAsync(categories =>
            {
                List<Category> tieredCategories = new List<Category>();
                foreach(Category category in categories.data)
                {
                    if (category.tiered)
                    {
                        tieredCategories.Add(category);    
                    }
                }
                onSuccess.Invoke(tieredCategories);
            }, onApiError, onServerError);
        }
        
        public Task GetTieredCategoriesForUserAsync(long usernameId, Action<WrappedCategories> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            return GetRequestAsyncAuthenticated("accounts/" + PublicToken + "/categories?usernameId=" + usernameId, onSuccess, onApiError, onServerError);
        }

        public Task UpdateTierAsync(int tierId, int packageId, Action<TierUpgradeResponse> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            var payload = new UpdateTierPayload(packageId);
            return PatchRequestAsyncAuthenticated("accounts/" + PublicToken + "/tiers/" + tierId, Json.SerializeObject(payload), onSuccess, onApiError, onServerError);
        }
        private Task GetRequestAsync<T>(string endpoint, Action<T> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            _adapter.LogDebug("-> GET " + endpoint);
            return Send<T>(endpoint, "", HttpVerb.GET, (code, body) =>
            {
                HandleResponse(endpoint, body, onSuccess, onApiError, onServerError, code);
            }, err =>
            {
                _adapter.LogDebug("api error");
                _adapter.LogDebug(err.title);
            }, err =>
            {
                _adapter.LogDebug("server error");
                _adapter.LogDebug(err.Body);
            });
        }
        private Task GetRequestAsyncAuthenticated<T>(string endpoint, Action<T> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            _adapter.LogDebug("-> GET " + endpoint);
            return Send<T>(endpoint, "", HttpVerb.GET, (code, body) =>
            {
                HandleResponse(endpoint, body, onSuccess, onApiError, onServerError, code);
            }, onApiError.Invoke, onServerError.Invoke, true);
        }
        private Task PostRequestAsync<T>(string endpoint, string data, Action<T> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            _adapter.LogDebug("-> POST " + endpoint + " | " + data);
            return Send<T>(endpoint, data, HttpVerb.POST, (code, body) =>
            {
                HandleResponse(endpoint, body, onSuccess, onApiError, onServerError, code);
            }, onApiError.Invoke, onServerError.Invoke);
        }
        private Task PatchRequestAsyncAuthenticated<T>(string endpoint, string data, Action<T> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            _adapter.LogDebug("-> PATCH " + endpoint + " | " + data);
            return Send<T>(endpoint, data, HttpVerb.PATCH, (code, body) =>
            {
                HandleResponse(endpoint, body, onSuccess, onApiError, onServerError, code);
            }, onApiError.Invoke, onServerError.Invoke, true);
        }
        private Task PutRequestAsync<TReturnType>(string endpoint, string data, Action<TReturnType> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError)
        {
            _adapter.LogDebug("-> PUT " + endpoint + " | " + data);
            return Send<TReturnType>(endpoint, data, HttpVerb.PUT, (code, body) =>
            {
                HandleResponse(endpoint, body, onSuccess, onApiError, onServerError, code);
            }, onApiError.Invoke, onServerError.Invoke);
        }
        
        private void HandleResponse<TReturnType>(string url, string body, Action<TReturnType> onSuccess, Action<HeadlessApiError> onApiError, Action<ServerError> onServerError, int statusCode)
        {
            try
            {
                _adapter.LogDebug($"<- {statusCode} {url} | {body}");
                
                // baskets/{ident}/auth will return an array containing an empty array "[[]]" instead of an empty object, handle that here
                if (typeof(TReturnType) == typeof(WrappedBasketLinks) && body.StartsWith("["))
                {
                    var emptyBasketLinks = Activator.CreateInstance(typeof(WrappedBasketLinks));
                    onSuccess.Invoke((TReturnType)emptyBasketLinks);
                    return;
                }

                // A basket without links is improperly returned as an empty array instead of an object, handle explicitly
                if ((typeof(TReturnType) == typeof(WrappedBasket) || typeof(TReturnType) == typeof(Basket)) && body.Contains("\"links\":[]"))
                {
                    body = body.Replace("\"links\":[]", "\"links\":{}");
                    var basket = Json.DeserializeObject<TReturnType>(body);
                    if (basket == null)
                    {
                        throw new Exception("Response body was null, expected JSON");
                    }
                    
                    onSuccess.Invoke(basket);
                    return;
                }
                
                var response = Json.DeserializeObject<TReturnType>(body);
                if (response == null)
                {
                    throw new Exception("Response body was null, expected JSON");
                }
                onSuccess.Invoke(response);
            }
            catch (Exception e)
            {
                onServerError.Invoke(new ServerError(statusCode, e.Message + " | " + body));
            }
        }
    }
}