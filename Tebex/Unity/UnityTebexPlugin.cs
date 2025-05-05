using System;
using System.Net.Http;
using System.Threading.Tasks;
using Tebex.Common;
using Tebex.Plugin;
using Tebex.PluginAPI;
using UnityEngine;

namespace Tebex_Unity.Tebex.Unity
{
    public class UnityTebexPlugin : MonoBehaviour
    {
        private readonly string KEY = "your-key-here";
        private TebexCorePlugin _plugin;
        private static readonly HttpClient Client = new HttpClient();

        private void OnEnable()
        {
            Debug.Log("Starting Tebex...");
            var pluginInitTask = TebexCorePlugin.Initialize(new UnityPluginAdapter(), KEY);
            pluginInitTask.Wait();

            if (!pluginInitTask.IsCompletedSuccessfully)
            {
                Debug.LogError("Tebex failed to start!");
                Debug.LogError(pluginInitTask.Exception);
                return;
            }
            
            _plugin = pluginInitTask.Result;
            Debug.Log("Tebex started successfully!");
        }
    }
    
    public class UnityPluginAdapter : PluginAdapter
    {
        private StandardPluginAdapter _standardPluginAdapter = new StandardPluginAdapter();
        
        public override Task Send<T>(string key, string url, string body, HttpVerb verb, ApiSuccessCallback onSuccess,
            Action<PluginApiError> onPluginApiError, Action<ServerError> onServerError)
        {
            return _standardPluginAdapter.Send<T>(key, url, body, verb, onSuccess, onPluginApiError, onServerError);
        }

        public override bool PlayerHasInventorySlotsAvailable(DuePlayer player, int slots)
        {
            return false; //TODO implement your check for inventory slots here
        }

        public override bool ExecuteCommand(Command command)
        {
            return true; //TODO implement your command execution logic here
        }

        public override bool IsPlayerOnline(DuePlayer player)
        {
            return false; //TODO check if a player is online here
        }

        public override bool IsDebugModeEnabled()
        {
            return true;
        }

        public override void TellPlayer(string userId, string message)
        {
            //TODO send a message to a player here 
        }
        
        public override void LogWarning(string warning, string solution)
        {
            Debug.LogWarning(warning);
            Debug.LogWarning("- " + solution);
        }
        
        public override void LogInfo(string message)
        {
            Debug.Log(message);
        }
        
        public override void LogDebug(string message)
        {
            if (IsDebugModeEnabled())
            {
                Debug.Log("[DEBUG] " + message);    
            }
        }
        
        public override void LogError(string message)
        {
            Debug.LogError("[ERROR] " + message);
        }
    }
}