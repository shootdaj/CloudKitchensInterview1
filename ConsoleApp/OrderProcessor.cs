using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ConsoleApp
{
    //1. Add locking
    //2. Explain choices
    //3. 

    internal class OrderProcessor
    {
        public OrderProcessor(
            ILogger logger,
            List<Shelf> shelves
        )
        {
            Logger = logger;
            Shelves = shelves;

            ReceivedOrderBlock = new TransformBlock<Order, Order>(
                order =>
                {
                    Logger.Log($"{order.Id} - Received order");
                    return order;
                }
            );

            ShelveBlock = new TransformBlock<Order, Order>(
                order =>
                {
                    Logger.Log($"{order.Id} - Placing order on shelf");
                    PlaceOrderOnShelf(order);
                    return order;
                }
            );

            DispatchCourierBlock = new TransformBlock<Order, Order>(
                order =>
                {
                    Logger.Log($"{order.Id} - Dispatching courier");
                    return order;
                }
            );

            FinishOrderBlock = new ActionBlock<Order>(
                async order =>
                {
                    await Task.Delay(2000); //;new Random().Next(2000, 6000));
                    FinishOrder(order);
                }
            );

            ReceivedOrderBlock.LinkTo(DispatchCourierBlock);
            DispatchCourierBlock.LinkTo(ShelveBlock);
            ShelveBlock.LinkTo(FinishOrderBlock);
        }

        private TransformBlock<Order, Order> ReceivedOrderBlock { get; }

        private TransformBlock<Order, Order> ShelveBlock { get; }

        private TransformBlock<Order, Order> DispatchCourierBlock { get; }

        private ActionBlock<Order> FinishOrderBlock { get; }

        public ILogger Logger { get; }

        public List<Shelf> Shelves { get; }

        private void FinishOrder(Order order)
        {
            Logger.Log(
                $"{order.Id} - Finishing order from shelf {order.ShelfType}"
            );

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
                        Logger.Log(
                            $"{order.Id} - Adding order to shelf {ShelfType.Overflow} and discarding random order from shelf {ShelfType.Overflow}"
                        );

                        lock (overflowShelf)
                        {
                            var orderToDiscard = overflowShelf.Orders.First();
                            overflowShelf.Orders.Remove(orderToDiscard);
                            overflowShelf.Orders.Add(order);
                        }
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

        public void PostOrder(Order order)
        {
            ReceivedOrderBlock.Post(order);
        }

        //public async Task ProcessOrder(Order order)
        //{
        //    DispatchCourier(order);
        //}

        //private async Task DispatchCourier(Order order)
        //{
        //}
    }
}