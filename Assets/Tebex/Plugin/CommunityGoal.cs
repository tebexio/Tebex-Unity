using System;

#nullable enable

namespace Tebex.Plugin
{
    [Serializable]
    public class CommunityGoal
    {
        public int id;
        public DateTime created_at;
        public DateTime updated_at;
        public int account;
        public string name = string.Empty;
        public string description = string.Empty;
        public string image = string.Empty;
        public double target;
        public double current;
        public int repeatable;
        public DateTime? last_achieved;
        public int times_achieved;
        public string status = string.Empty;
        public int sale;
    }
}
