using System;

namespace Tebex.Plugin
{
    /// <summary>
    /// Metadata received from /queue
    /// </summary>
    [Serializable]
    public class CommandQueueMeta
    {
        public bool execute_offline;
        public int next_check;
        public bool more;
    }
}
