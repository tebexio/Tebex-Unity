using System;

namespace Tebex.Plugin
{
    /// <summary>
    /// Information about the store's game server.
    /// </summary>
    [Serializable]
    public class Server
    {
        public int id;
        public string name = string.Empty;
    }
}
