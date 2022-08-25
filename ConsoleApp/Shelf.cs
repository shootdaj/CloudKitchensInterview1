using System.Collections.Generic;

namespace ConsoleApp
{
    internal class Shelf
    {
        public Shelf(
            ShelfType shelfType,
            int capacity
        )
        {
            ShelfType = shelfType;
            Orders = new List<Order>(capacity);
        }

        public ShelfType ShelfType { get; }
        public int Capacity => Orders.Capacity;
        public int CurrentLoad => Orders.Count;
        public List<Order> Orders { get; }
    }
}