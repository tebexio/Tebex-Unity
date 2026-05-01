using System;

namespace Tebex.Plugin
{
    /// <summary>
    /// Represents the payload for adding an amount to a gift card.
    /// </summary>
    [Serializable]
    public class TopUpGiftCardPayload
    {
        public string amount = string.Empty;
    }
}
