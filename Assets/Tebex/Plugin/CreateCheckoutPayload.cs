using System;

namespace Tebex.Plugin
{
    /// <summary>
    /// Represents the payload for creating a checkout link for a player.
    /// </summary>
    [Serializable]
    public class CreateCheckoutPayload
    {
        public int package_id;
        public string username = string.Empty;
    }
}
