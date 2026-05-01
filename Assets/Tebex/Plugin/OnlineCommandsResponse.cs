using System;
using System.Collections.Generic;

namespace Tebex.Plugin
{
    /// <summary>
    /// Response received from /queue/online-commands
    /// </summary>
    [Serializable]
    public class OnlineCommandsResponse
    {
        public OnlineCommandsPlayer player = new OnlineCommandsPlayer();
        public List<Command> commands = new List<Command>();
    }
}
