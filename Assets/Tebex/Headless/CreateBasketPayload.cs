using System;
using System.Collections.Generic;

#nullable enable

namespace Tebex.Headless
{
    /// <summary>
    /// Request data for creating a basket.
    /// </summary>
    [Serializable]
    public class CreateBasketPayload
    {
        public string? email;
        public string? username;
        public string? cancel_url;
        public string? complete_url;
        public Dictionary<string, string> custom = new Dictionary<string, string>();
        public bool complete_auto_redirect;

        public CreateBasketPayload(string? email = null, string? username = null, string? cancelUrl = null,
            string? completeUrl = null, Dictionary<string, string>? custom = null, bool completeAutoRedirect = true)
        {
            this.email = email;
            this.username = username;
            cancel_url = cancelUrl;
            complete_url = completeUrl;
            this.custom = custom ?? new Dictionary<string, string>();
            complete_auto_redirect = completeAutoRedirect;
        }
    }
}
