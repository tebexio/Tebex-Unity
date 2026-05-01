using System;

namespace Tebex.Plugin
{
    /// <summary>
    /// A due player is one returned by /queue to indicate we have some commands to run.
    /// </summary>
    [Serializable]
    public class DuePlayer
    {
        public int id;
        public string name = string.Empty;
        public string uuid = string.Empty;
    }
}
