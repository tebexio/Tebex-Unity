#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using Tebex.Headless;
using UnityEngine;
#endif

namespace Tebex.Common
{
    public static class Json
    {
        public static string SerializeObject<T>(T obj)
        {
            #if UNITY_5_3_OR_NEWER
            return JsonUtility.ToJson(obj);
            #endif
        }
        
        public static T DeserializeObject<T>(string json)
        {
            #if UNITY_5_3_OR_NEWER
            var deserialized = JsonUtility.FromJson<T>(json);
            
            // reading a basket, we must apply packages list to parsed object, which may not be handled by unity properly
            if (typeof(WrappedBasket) == typeof(T))
            {
                Debug.LogWarning("Deserialized basket " + deserialized);
                
                var packagesStr = "\"packages\":[";
                var packagesStart = json.IndexOf(packagesStr) + packagesStr.Length - 1;
                var couponsStart = json.IndexOf(",\"coupons\":");
                var packagesJson = json.Substring(packagesStart, couponsStart - packagesStart); // should be the opening [ of the packages list
                
                // enclose in data: {} for JsonUtility to parse
                packagesJson = "{\"data\":" + packagesJson + "}";
                
                Debug.LogWarning(packagesJson);
                var packages = JsonUtility.FromJson<WrappedBasketPackages>(packagesJson);
                Debug.LogError(packages.data.Count);
                WrappedBasket b = deserialized as WrappedBasket;
                b.data.packages = packages.data;
                Debug.LogWarning("Wrote packages to basket " + b.data.packages.Count);
                deserialized = (T) (object) b;
                Debug.LogWarning("Serialized basket " + deserialized);
            }

            return deserialized;
#endif
        }
    }
}