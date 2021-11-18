using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChaoWorld.Core.Models
{
    public class BroadcastChannels
    {
        public ulong General { get; set; }
        public ulong Races { get; set; }
        public ulong Tournaments { get; set; }
        public ulong Expeditions { get; set; }
    }
}
