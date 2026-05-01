using System;

#nullable enable

namespace Tebex.Headless
{
    [Serializable]
    public class Tier
    {
        public int id;
        public string created_at = string.Empty;
        public long username_id = -1L;
        public Package package = new Package();
        public bool active = false;
        public string recurring_payment_reference = string.Empty;
        public string next_payment_date = string.Empty;
        public TierStatus status = new TierStatus();
        public Package? pending_downgrade_package;
    }
}
