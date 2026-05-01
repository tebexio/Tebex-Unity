using System;
using System.Collections.Generic;

namespace Tebex.Plugin
{
    [Serializable]
    public class OfflineCommandsResponse
    {
        public OfflineCommandsMeta meta = new OfflineCommandsMeta();
        public List<Command> commands = new List<Command>();
    }
}
