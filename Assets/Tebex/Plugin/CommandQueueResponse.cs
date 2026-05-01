using System;
using System.Collections.Generic;

namespace Tebex.Plugin
{
    /// <summary>
    /// Response from /queue
    /// </summary>
    [Serializable]
    public class CommandQueueResponse
    {
        public CommandQueueMeta meta = new CommandQueueMeta();
        public List<DuePlayer> players = new List<DuePlayer>();
    }
}
