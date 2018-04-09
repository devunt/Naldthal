// ReSharper disable All

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Naldthal
{
    internal partial class Metadata
    {
        public Dictionary<int, NPC> NPCs { get; set; }
        public Dictionary<int, Item> Items { get; set; }
        public Dictionary<int, InstanceContent> InstanceContents { get; set; }
        public Dictionary<int, string> Placenames { get; set; }
        public Dictionary<int, string> CrafterTypeNames { get; set; }
        public Dictionary<int, string> GathererTypeNames { get; set; }


        internal class Item
        {
            public string Name { get; set; }
        }

        internal class InstanceContent
        {
            public string Name { get; set; }
        }

        internal class NPC
        {
            public string Name { get; set; }

            public string Title { get; set; }

            public Location[] Locations { get; set; }

            internal class Location
            {
                public int PlaceId { get; set; }

                public int X { get; set; }

                public int Y { get; set; }
            }
        }
    }
}