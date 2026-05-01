using System;

namespace Tebex.Plugin
{
    /// <summary>
    /// A player's information returned by the /user endpoint.
    /// </summary>
    [Serializable]
    public class PlayerInfo
    {
        public string id = string.Empty;
        public string username = string.Empty;
        public OnlineCommandPlayerMeta meta = new OnlineCommandPlayerMeta();
        public string uuid = string.Empty;
        public int plugin_username_id;
    }
}
