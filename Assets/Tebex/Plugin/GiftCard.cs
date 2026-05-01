using System;

#nullable enable

namespace Tebex.Plugin
{
    [Serializable]
    public class GiftCard
    {
        public int id;
        public string code = string.Empty;
        public GiftCardBalance balance = new GiftCardBalance();
        public string note = string.Empty;
        public bool voided;
        public DateTime created_at;
        public DateTime? expires_at;
    }
}
