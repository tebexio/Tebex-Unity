using System;

#nullable enable

namespace Tebex.Plugin
{
    [Serializable]
    public class Ban
    {
        public int id;
        public DateTime time;
        public string ip = string.Empty;
        public string? payment_email;
        public string reason = string.Empty;
        public BanUserInfo user = new BanUserInfo();
    }
}
