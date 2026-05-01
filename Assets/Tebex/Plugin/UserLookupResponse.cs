using System;
using System.Collections.Generic;

namespace Tebex.Plugin
{
    /// <summary>
    /// Root object returned by the /user endpoint, containing PlayerInfo.
    /// </summary>
    [Serializable]
    public class UserLookupResponse
    {
        public PlayerInfo player = new PlayerInfo();
        public int ban_count;
        public int chargeback_rate;
        public List<PlayerPaymentInfo> payments = new List<PlayerPaymentInfo>();
        public object[] purchase_totals = Array.Empty<object>();
    }
}
