using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp
{
    internal class OrderProcessor
    {
        public OrderProcessor(
            ILogger logger,
            List<Shelf> shelves,
            int minCourierDelay,
            int maxCourierDelay
        )
        {
            Logger = logger;
            Shelves = shelves;
            MinCourierDelay = minCourierDelay;
            MaxCourierDelay = maxCourierDelay;
        }

        private int MinCourierDelay { get; }
        private int MaxCourierDelay { get; }

        public ILogger Logger { get; }

        public List<Shelf> Shelves { get; }

        private void FinishOrder(Order order)
        {
            var orderShelf = GetShelfByType(order.ShelfType);

            lock (orderShelf)
            {
                if (orderShelf.Orders.Any(x => x.Id.Equals(order.Id)))
                {
                    Logger.Log(
                        $"{order.Id} - Delivering order from shelf {orderShelf.ShelfType}"
                    );

                    var orderToRemove = orderShelf.Orders.Single(x => x.Id.Equals(order.Id));
                    orderShelf.Orders.Remove(orderToRemove);
                    return;
                }
            }

            var overflowShelf = GetShelfByType(ShelfType.Overflow);

            lock (overflowShelf)
            {
                if (overflowShelf.Orders.Any(x => x.Id.Equals(order.Id)))
                {
                    Logger.Log(
                        $"{order.Id} - Delivering order from shelf {overflowShelf.ShelfType}"
                    );

                    var orderToRemove = overflowShelf.Orders.Single(x => x.Id.Equals(order.Id));
                    overflowShelf.Orders.Remove(orderToRemove);
                }
            }

            Logger.Log(
                $"{order.Id} - Order was discarded"
            );
        }

        private void PlaceOrderOnShelf(Order order)
        {
            var shelf = Shelves.Single(shelf => shelf.ShelfType.Equals(order.ShelfType));

            if (!IsShelfFull(shelf.ShelfType))
            {
                Logger.Log($"{order.Id} - Adding order to shelf {shelf.ShelfType}");

                lock (shelf)
                {
                    shelf.Orders.Add(order);
                }
            }
            else
            {
                Logger.Log($"{order.Id} - Shelf {shelf.ShelfType} is full");

                var overflowShelf = Shelves.Single(s => s.ShelfType.Equals(ShelfType.Overflow));
                if (!IsShelfFull(ShelfType.Overflow))
                {
                    Logger.Log($"{order.Id} - Adding order to shelf {ShelfType.Overflow}");

                    lock (overflowShelf)
                    {
                        overflowShelf.Orders.Add(order);
                    }
                }
                else
                {
                    Logger.Log($"{order.Id} - Shelf {ShelfType.Overflow} is full");

                    if (overflowShelf.Orders.Any(x => x.ShelfType.Equals(ShelfType.Frozen)) &&
                        !IsShelfFull(ShelfType.Frozen))
                    {
                        Logger.Log(
                            $"{order.Id} - Adding order to shelf {ShelfType.Overflow} and moving random order back to shelf {ShelfType.Frozen}"
                        );

                        var shelfToMoveTo = GetShelfByType(ShelfType.Frozen);

                        lock (overflowShelf)
                        {
                            lock (shelfToMoveTo)
                            {
                                var orderToMove = overflowShelf.Orders.First(x => x.ShelfType.Equals(ShelfType.Frozen));
                                overflowShelf.Orders.Remove(orderToMove);
                                shelfToMoveTo.Orders.Add(orderToMove);
                                overflowShelf.Orders.Add(order);
                            }
                        }
                    }
                    else if (overflowShelf.Orders.Any(x => x.ShelfType.Equals(ShelfType.Cold)) &&
                             !IsShelfFull(ShelfType.Cold))
                    {
                        Logger.Log(
                            $"{order.Id} - Adding order to shelf {ShelfType.Overflow} and moving random order back to shelf {ShelfType.Cold}"
                        );

                        var shelfToMoveTo = GetShelfByType(ShelfType.Cold);

                        lock (overflowShelf)
                        {
                            lock (shelfToMoveTo)
                            {
                                var orderToMove = overflowShelf.Orders.First(x => x.ShelfType.Equals(ShelfType.Cold));
                                overflowShelf.Orders.Remove(orderToMove);
                                shelfToMoveTo.Orders.Add(orderToMove);
                                overflowShelf.Orders.Add(order);
                            }
                        }
                    }
                    else if (overflowShelf.Orders.Any(x => x.ShelfType.Equals(ShelfType.Hot)) &&
                             !IsShelfFull(ShelfType.Hot))
                    {
                        Logger.Log(
                            $"{order.Id} - Adding order to shelf {ShelfType.Overflow} and moving random order back to shelf {ShelfType.Hot}"
                        );

                        var shelfToMoveTo = GetShelfByType(ShelfType.Hot);

                        lock (overflowShelf)
                        {
                            lock (shelfToMoveTo)
                            {
                                var orderToMove = overflowShelf.Orders.First(x => x.ShelfType.Equals(ShelfType.Cold));
                                overflowShelf.Orders.Remove(orderToMove);
                                shelfToMoveTo.Orders.Add(orderToMove);
                                overflowShelf.Orders.Add(order);
                            }
                        }
                    }
                    else
                    {
                        Order orderToDiscard;
                        lock (overflowShelf)
                        {
                            orderToDiscard = overflowShelf.Orders.First();
                            overflowShelf.Orders.Remove(orderToDiscard);
                            overflowShelf.Orders.Add(order);
                        }

                        Logger.Log(
                            $"{order.Id} - Adding order to shelf {ShelfType.Overflow} and discarding order {orderToDiscard.Id}"
                        );
                    }
                }
            }
        }

        private Shelf GetShelfByType(ShelfType shelfType)
        {
            return Shelves.Single(x => x.ShelfType.Equals(shelfType));
        }

        private bool IsShelfFull(ShelfType shelfType)
        {
            var shelf = Shelves.Single(s => s.ShelfType == shelfType);
            return IsShelfFull(shelf);
        }

        private bool IsShelfFull(Shelf shelf)
        {
            lock (shelf)
            {
                return shelf.CurrentLoad >= shelf.Capacity;
            }
        }

        public async Task PostOrder(Order order)
        {
            Logger.Log($"{order.Id} - Received order");

            Logger.Log($"{order.Id} - Placing order on shelf");
            PlaceOrderOnShelf(order);

            Logger.Log($"{order.Id} - Dispatching courier");

            await Task.Delay(new Random().Next(MinCourierDelay, MaxCourierDelay));
            FinishOrder(order);
        }
    }
}