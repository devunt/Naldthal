// ReSharper disable All

using System.Collections.Generic;

namespace Naldthal
{
    internal class Data
    {
        public Metadata Metadata { get; set; }
        public Dictionary<int, Shop[]> Shops { get; set; }
        public Dictionary<int, GCShop> GCShops { get; set; }
        public Dictionary<int, FCShop> FCShops { get; set; }
        public Dictionary<int, int[]> InstanceContentIds { get; set; }
        public Dictionary<int, Crafting[]> Craftings { get; set; }
        public Dictionary<int, Gathering[]> Gatherings { get; set; }


        internal class Shop
        {
            public int CostId { get; set; }
            public int CostAmount { get; set; }
            public int[] SellerIds { get; set; }
        }

        internal class GCShop
        {
            public int CostAmount { get; set; }
        }

        internal class FCShop
        {
            public int CostAmount { get; set; }
        }

        internal class Crafting
        {
            public int CrafterType { get; set; }
            public int Level { get; set; }
        }

        internal class Gathering
        {
            public int GathererType { get; set; }
        }
    }
}
