using System;

namespace Tebex.Plugin
{
    /// <summary>
    /// Information about the webstore account.
    /// </summary>
    [Serializable]
    public class Account
    {
        public int id;
        public string domain = string.Empty;
        public string name = string.Empty;
        public Currency currency = new Currency();
        public bool online_mode;
        public string game_type = string.Empty;
        public bool log_events;
    }
}
