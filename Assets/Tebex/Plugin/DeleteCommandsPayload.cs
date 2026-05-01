using System;

namespace Tebex.Plugin
{
    /// <summary>
    /// Represents the payload for deleting commands on the game server.
    /// </summary>
    [Serializable]
    public class DeleteCommandsPayload
    {
        public int[] ids = Array.Empty<int>();
    }
}
