using System;

namespace Tebex.Headless
{
    /// <summary>
    /// A link a user may use to login and authorize their Basket.
    /// </summary>
    [Serializable]
    public class BasketAuthLink
    {
        public string name = string.Empty;
        public string url = string.Empty;
    }
}
