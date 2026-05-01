using System;

namespace Tebex.Plugin
{
    [Serializable]
    public class Command
    {
        public int id;
        public string command_to_run = string.Empty;
        public long payment;
        public long package_ref;
        public CommandConditions conditions = new CommandConditions();
        public PlayerInfo player = new PlayerInfo();
    }
}
