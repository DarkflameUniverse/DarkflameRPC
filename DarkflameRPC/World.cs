using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordRPC.Example
{
    class World
    {
        public string worldName;
        public string worldID;
        public string worldDescription;
        public string worldLargeIcon;

        public World(string name, string ID, string description, string largeIcon)
        {
            worldName = name;
            worldID = ID;
            worldDescription = description;
            worldLargeIcon = largeIcon;
        }
    }
}
