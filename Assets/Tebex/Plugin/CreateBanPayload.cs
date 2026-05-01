using System;

namespace Tebex.Plugin
{
    /// <summary>
    /// Represents the payload for banning a player from the store.
    /// </summary>
    [Serializable]
    public class CreateBanPayload
    {
        public string reason = string.Empty;
        public string ip = string.Empty;
        public string user = string.Empty;
    }
}
