using System;

namespace Tebex.Plugin
{
    /// <summary>
    /// The webstore currency.
    /// </summary>
    [Serializable]
    public class Currency
    {
        public string iso4217 = string.Empty;
        public string symbol = string.Empty;
    }
}
