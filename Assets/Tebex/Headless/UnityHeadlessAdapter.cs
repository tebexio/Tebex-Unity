using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tebex.Common;
using UnityEngine;

namespace Tebex.Headless
{
    public class UnityHeadlessAdapter : HeadlessAdapter
    {
        public override async Task Send<T>(string url, string body, HttpVerb verb, ApiSuccessCallback onSuccess, Action<HeadlessApiError> onHeadlessApiError, Action<ServerError> onServerError, bool authenticated = false)
        {
            var headers = new Dictionary<string, string>();

            if (authenticated)
            {
                var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_apiInstance.PublicToken}:{_apiInstance.PrivateKey}"));
                headers["Authorization"] = $"Basic {credentials}";
            }

            try
            {
                var (code, resultBody) = await Http.Send(url, body, verb, headers);
                LogDebug("<- " + code + " | " + resultBody);

                if (code >= 200 && code < 300)
                {
                    onSuccess.Invoke(code, resultBody);
                    return;
                }

                if (code >= 400 && code < 500)
                {
                    try
                    {
                        var error = Json.DeserializeObject<HeadlessApiError>(resultBody);
                        if (error == null)
                        {
                            throw new Exception("Error response body was empty, expected JSON");
                        }
                        onHeadlessApiError.Invoke(error);
                    }
                    catch (Exception e)
                    {
                        onServerError.Invoke(new ServerError(code, e.Message + " | " + resultBody));
                    }
                }
                else
                {
                    onServerError.Invoke(new ServerError(code, resultBody));
                }
            }
            catch (Exception e)
            {
                onServerError.Invoke(new ServerError(0, e.Message));
            }
        }

        public override void LogApiError(HeadlessApiError error)
        {
            Debug.LogError("Error received from API: " + error.title);
        }

        public override void LogServerError(ServerError error)
        {
            Debug.LogError("Unexpected server error (" + error.Code + ") from API: " + error.Body);
        }

        public override void LogDebug(string message)
        {
            Debug.Log("[DEBUG] " + message);
        }
    }
}
