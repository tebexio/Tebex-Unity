using System;

namespace Tebex.Plugin
{
    [Serializable]
    public class OnlineCommandsPlayer
    {
        public string id = string.Empty;
        public string username = string.Empty;
        public OnlineCommandPlayerMeta meta = new OnlineCommandPlayerMeta();
    }
}
