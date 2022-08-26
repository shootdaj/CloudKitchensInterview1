using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp
{
    /// <summary>
    ///     Order Processor used to process orders to completion.
    /// </summary>
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
                    OutputShelves();

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
                    OutputShelves();

                    var orderToRemove = overflowShelf.Orders.Single(x => x.Id.Equals(order.Id));
                    overflowShelf.Orders.Remove(orderToRemove);
                    return;
                }
            }

            Logger.Log(
                $"{order.Id} - Order was discarded"
            );
            OutputShelves();
        }

        private void PlaceOrderOnShelf(Order order)
        {
            var shelf = Shelves.Single(shelf => shelf.ShelfType.Equals(order.ShelfType));

            if (!IsShelfFull(shelf.ShelfType))
            {
                Logger.Log($"{order.Id} - Adding order to shelf {shelf.ShelfType}");
                OutputShelves();

                lock (shelf)
                {
                    shelf.Orders.Add(order);
                }
            }
            else
            {
                Logger.Log($"{order.Id} - Shelf {shelf.ShelfType} is full");
                OutputShelves();

                var overflowShelf = Shelves.Single(s => s.ShelfType.Equals(ShelfType.Overflow));
                if (!IsShelfFull(ShelfType.Overflow))
                {
                    Logger.Log($"{order.Id} - Adding order to shelf {ShelfType.Overflow}");
                    OutputShelves();

                    lock (overflowShelf)
                    {
                        overflowShelf.Orders.Add(order);
                    }
                }
                else
                {
                    Logger.Log($"{order.Id} - Shelf {ShelfType.Overflow} is full");
                    OutputShelves();

                    if (overflowShelf.Orders.Any(x => x.ShelfType.Equals(ShelfType.Frozen)) &&
                        !IsShelfFull(ShelfType.Frozen))
                    {
                        Logger.Log(
                            $"{order.Id} - Adding order to shelf {ShelfType.Overflow} and moving random order back to shelf {ShelfType.Frozen}"
                        );
                        OutputShelves();

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
                        OutputShelves();

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
                        OutputShelves();

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
                        OutputShelves();
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

        private void OutputShelves()
        {
            //Logger.Log("-------");
            //Shelves.ForEach(
            //    s =>
            //    {
            //        lock (s)
            //        {
            //            Logger.Log($"Shelf {s.ShelfType} contains:");
            //            s.Orders.ForEach(
            //                o => { Logger.Log($"{o.Id.ToString()} {o.Name}"); }
            //            );
            //        }

            //        Logger.Log("--");
            //    }
            //);
            //Logger.Log("====================");
        }

        /// <summary>
        ///     Post a new order to be processed
        /// </summary>
        /// <param name="order">Order to be processed</param>
        public async Task PostOrder(Order order)
        {
            Logger.Log($"{order.Id} - Received order");
            OutputShelves();

            Logger.Log($"{order.Id} - Placing order on shelf");
            OutputShelves();
            PlaceOrderOnShelf(order);

            Logger.Log($"{order.Id} - Dispatching courier");
            OutputShelves();

            await Task.Delay(new Random().Next(MinCourierDelay, MaxCourierDelay));
            FinishOrder(order);
        }
    }
}