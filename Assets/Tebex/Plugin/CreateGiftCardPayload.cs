using System;

namespace Tebex.Plugin
{
    /// <summary>
    /// Represents the payload for creating a gift card on the store.
    /// </summary>
    [Serializable]
    public class CreateGiftCardPayload
    {
        public DateTime expires_at;
        public string note = string.Empty;
        public double amount;
    }
}
