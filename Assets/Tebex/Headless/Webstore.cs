using System;

namespace Tebex.Headless
{
    /// <summary>
    /// The main Tebex project / store information.
    /// </summary>
    [Serializable]
    public class Webstore
    {
        public uint id;
        public string description = string.Empty;
        public string name = string.Empty;
        public string webstore_url = string.Empty;
        public string currency = string.Empty;
        public string lang = string.Empty;
        public string logo = string.Empty;
        public string platform_type = string.Empty;
        public string platform_type_id = string.Empty;
        public DateTime created_at = DateTime.MinValue;
    }
}
