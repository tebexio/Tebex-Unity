using System;

namespace Tebex.Plugin
{
    [Serializable]
    public class JoinEvent
    {
        public string username_id;
        public string event_type;
        public DateTime event_date;
        public string ip_address;

        public JoinEvent(string usernameId, string eventType, string ipAddress)
        {
            username_id = usernameId;
            event_type = eventType;
            event_date = DateTime.UtcNow;
            ip_address = ipAddress;
        }
    }
}
