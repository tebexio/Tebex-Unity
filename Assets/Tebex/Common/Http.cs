#if UNITY_5_3_OR_NEWER
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
#endif

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tebex.Common
{
    public static class Http
    {
        public static Task<(int statusCode, string body)> Send(string url, string body, HttpVerb verb, Dictionary<string, string> headers = null)
        {
#if UNITY_5_3_OR_NEWER
            var tcs = new TaskCompletionSource<(int, string)>();
            CoroutineRunner.Instance.Run(SendCoroutine(url, body, verb, headers, tcs));
            return tcs.Task;
#endif
        }

        public static Task<Texture2D> LoadTexture(string url)
        {
#if UNITY_5_3_OR_NEWER
            var tcs = new TaskCompletionSource<Texture2D>();
            CoroutineRunner.Instance.Run(LoadTextureCoroutine(url, tcs));
            return tcs.Task;
#endif
        }

#if UNITY_5_3_OR_NEWER
        private static IEnumerator SendCoroutine(string url, string body, HttpVerb verb, Dictionary<string, string> headers, TaskCompletionSource<(int, string)> tcs)
        {
            var request = new UnityWebRequest(url, verb.ToString());

            if (body != null && body.Length > 0)
            {
                var bodyData = System.Text.Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bodyData);
                request.uploadHandler.contentType = "application/json";
            }
            request.downloadHandler = new DownloadHandlerBuffer();

            if (headers != null)
            {
                foreach (var kv in headers)
                {
                    request.SetRequestHeader(kv.Key, kv.Value);
                }
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError)
            {
                tcs.SetException(new Exception(request.error));
            }
            else
            {
                tcs.SetResult(((int)request.responseCode, request.downloadHandler.text));
            }
        }

        private static IEnumerator LoadTextureCoroutine(string url, TaskCompletionSource<Texture2D> tcs)
        {
            var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError)
            {
                tcs.SetException(new Exception(request.error));
            }
            else
            {
                tcs.SetResult(DownloadHandlerTexture.GetContent(request));
            }
        }
    }

    internal class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;

        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("TebexCoroutineRunner");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CoroutineRunner>();
                }
                return _instance;
            }
        }

        public void Run(IEnumerator coroutine)
        {
            StartCoroutine(coroutine);
        }
    }
#endif
}
