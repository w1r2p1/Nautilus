// -------------------------------------------------------------------------------------------------
// <copyright file="RedisExecutionDatabase.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   https://nautechsystems.io
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Redis.Execution
{
    using System.Collections.Generic;
    using System.Linq;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core.CQS;
    using Nautilus.Core.Message;
    using Nautilus.DomainModel.Aggregates;
    using Nautilus.DomainModel.Entities;
    using Nautilus.DomainModel.Events;
    using Nautilus.DomainModel.Events.Base;
    using Nautilus.DomainModel.Identifiers;
    using Nautilus.Execution.Engine;
    using Nautilus.Execution.Interfaces;
    using StackExchange.Redis;
    using Order = Nautilus.DomainModel.Aggregates.Order;

    /// <summary>
    /// Provides an execution database implemented with Redis.
    /// </summary>
    public class RedisExecutionDatabase : ExecutionDatabase, IExecutionDatabase
    {
        private readonly IServer redisServer;
        private readonly IDatabase redisDatabase;
        private readonly ISerializer<Command> commandSerializer;
        private readonly ISerializer<Event> eventSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisExecutionDatabase"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        /// <param name="connection">The redis connection multiplexer.</param>
        /// <param name="commandSerializer">The command serializer.</param>
        /// <param name="eventSerializer">The event serializer.</param>
        /// <param name="optionLoadCache">The option flag to load caches from Redis on instantiation.</param>
        public RedisExecutionDatabase(
            IComponentryContainer container,
            ConnectionMultiplexer connection,
            ISerializer<Command> commandSerializer,
            ISerializer<Event> eventSerializer,
            bool optionLoadCache = true)
            : base(container)
        {
            this.redisServer = connection.GetServer(RedisConstants.LocalHost, RedisConstants.DefaultPort);
            this.redisDatabase = connection.GetDatabase();
            this.commandSerializer = commandSerializer;
            this.eventSerializer = eventSerializer;

            this.OptionLoadCache = optionLoadCache;

            if (this.OptionLoadCache)
            {
                this.Log.Information($"The OptionLoadCache is {this.OptionLoadCache}");
                this.LoadCaches();
            }
            else
            {
                this.Log.Warning($"The OptionLoadCache is {this.OptionLoadCache} " +
                                 $"(this should only be done in a testing environment).");
            }
        }

        /// <summary>
        /// Gets a value indicating whether the execution database will load the caches on instantiation.
        /// </summary>
        public bool OptionLoadCache { get; }

        /// <inheritdoc />
        public override void LoadAccountsCache()
        {
            this.Log.Debug("Re-caching accounts from the database...");

            this.CachedAccounts.Clear();

            var accountKeys = this.redisServer.Keys(pattern: Key.Accounts).ToArray();
            if (accountKeys.Length == 0)
            {
                this.Log.Information("No accounts found in the database.");
                return;
            }

            foreach (var key in accountKeys)
            {
                var events = new Queue<RedisValue>(this.redisDatabase.ListRange(key));
                if (events.Count == 0)
                {
                    this.Log.Error($"Cannot load account {key} from the database (no events persisted).");
                    continue;
                }

                var initial = this.eventSerializer.Deserialize(events.Dequeue());
                if (initial.Type != typeof(AccountStateEvent))
                {
                    this.Log.Error($"Cannot load account {key} from the database (event not AccountStateEvent, was {initial.Type}).");
                    continue;
                }

                var account = new Account((AccountStateEvent)initial);
                this.CachedAccounts[account.Id] = account;
            }

            foreach (var kvp in this.CachedAccounts)
            {
                this.Log.Information($"Cached {kvp.Key}.");
            }
        }

        /// <inheritdoc />
        public override void LoadOrdersCache()
        {
            this.Log.Debug("Re-caching orders from the database...");

            this.CachedOrders.Clear();

            var orderKeys = this.redisServer.Keys(pattern: Key.Orders).ToArray();
            if (orderKeys.Length == 0)
            {
                this.Log.Information("No orders found in the database.");
                return;
            }

            foreach (var key in orderKeys)
            {
                var events = new Queue<RedisValue>(this.redisDatabase.ListRange(key));
                if (events.Count == 0)
                {
                    this.Log.Error($"Cannot load order {key} from the database (no events persisted).");
                    continue;
                }

                var initial = this.eventSerializer.Deserialize(events.Dequeue());
                if (initial.Type != typeof(OrderInitialized))
                {
                    this.Log.Error($"Cannot load order {key} from the database (first event not OrderInitialized, was {initial.Type}).");
                    continue;
                }

                var order = new Order((OrderInitialized)initial);
                while (events.Count > 0)
                {
                    var nextEvent = (OrderEvent)this.eventSerializer.Deserialize(events.Dequeue());
                    if (nextEvent is null)
                    {
                        this.Log.Error("Could not deserialize OrderEvent.");
                        continue;
                    }

                    order.Apply(nextEvent);
                }

                this.CachedOrders[order.Id] = order;
            }

            this.Log.Information($"Cached {this.CachedOrders.Count} order(s).");
        }

        /// <inheritdoc />
        public override void LoadPositionsCache()
        {
            this.Log.Debug("Re-caching positions from the database...");

            this.CachedPositions.Clear();

            var positionKeys = this.redisServer.Keys(pattern: Key.Positions).ToArray();
            if (positionKeys.Length == 0)
            {
                this.Log.Information("No positions found in the database.");
                return;
            }

            foreach (var key in positionKeys)
            {
                var events = new Queue<RedisValue>(this.redisDatabase.ListRange(key));
                if (events.Count == 0)
                {
                    this.Log.Error($"Cannot load position {key} from the database (no events persisted).");
                    continue;
                }

                var position = new Position(
                    new PositionId(key.ToString().Split(':').Last()),
                    (OrderFillEvent)this.eventSerializer.Deserialize(events.Dequeue()));
                while (events.Count > 0)
                {
                    var nextEvent = (OrderFillEvent)this.eventSerializer.Deserialize(events.Dequeue());
                    if (nextEvent is null)
                    {
                        this.Log.Error("Could not deserialize OrderFillEvent.");
                        continue;
                    }

                    position.Apply(nextEvent);
                }

                this.CachedPositions[position.Id] = position;
            }

            this.Log.Information($"Cached {this.CachedPositions.Count} position(s).");
        }

        /// <inheritdoc />
        public override void Flush()
        {
            this.ClearCaches();

            this.Log.Debug("Flushing database...");
            this.redisServer.FlushDatabase();
            this.Log.Information("Database flushed.");
        }

        /// <inheritdoc />
        public CommandResult AddAtomicOrder(AtomicOrder order, TraderId traderId, AccountId accountId, StrategyId strategyId, PositionId positionId)
        {
            var resultEntry = this.AddOrder(
                order.Entry,
                traderId,
                accountId,
                strategyId,
                positionId);
            if (resultEntry.IsFailure)
            {
                return resultEntry;
            }

            var resultStopLoss = this.AddOrder(
                order.StopLoss,
                traderId,
                accountId,
                strategyId,
                positionId);
            if (resultStopLoss.IsFailure)
            {
                return resultStopLoss;
            }

            if (order.TakeProfit != null)
            {
                var resultTakeProfit = this.AddOrder(
                    order.TakeProfit,
                    traderId,
                    accountId,
                    strategyId,
                    positionId);
                if (resultTakeProfit.IsFailure)
                {
                    return resultTakeProfit;
                }
            }

            return CommandResult.Ok();
        }

        /// <inheritdoc />
        public CommandResult AddAccount(Account account)
        {
            if (this.CachedAccounts.ContainsKey(account.Id))
            {
                return CommandResult.Fail($"The {account.Id} already existed in the cache (was not unique).");
            }

            this.redisDatabase.ListRightPush(Key.Account(account.Id), this.eventSerializer.Serialize(account.LastEvent), When.Always, CommandFlags.FireAndForget);

            this.CachedAccounts[account.Id] = account;

            this.Log.Debug($"Added Account(Id={account.Id.Value}).");

            return CommandResult.Ok();
        }

        /// <inheritdoc />
        public CommandResult AddOrder(Order order, TraderId traderId, AccountId accountId, StrategyId strategyId, PositionId positionId)
        {
            if (this.CachedOrders.ContainsKey(order.Id))
            {
                return CommandResult.Fail($"The {order.Id} already existed in the cache (was not unique).");
            }

            this.redisDatabase.SetAdd(Key.IndexTraders, traderId.Value, CommandFlags.FireAndForget);
            this.redisDatabase.SetAdd(Key.IndexTraderOrders(traderId), order.Id.Value, CommandFlags.FireAndForget);
            this.redisDatabase.SetAdd(Key.IndexTraderPositions(traderId), positionId.Value, CommandFlags.FireAndForget);
            this.redisDatabase.SetAdd(Key.IndexTraderStrategies(traderId), strategyId.Value, CommandFlags.FireAndForget);
            this.redisDatabase.SetAdd(Key.IndexTraderStrategyOrders(traderId, strategyId), order.Id.Value, CommandFlags.FireAndForget);
            this.redisDatabase.SetAdd(Key.IndexTraderStrategyPositions(traderId, strategyId), positionId.Value, CommandFlags.FireAndForget);
            this.redisDatabase.SetAdd(Key.IndexAccountOrders(accountId), order.Id.Value, CommandFlags.FireAndForget);
            this.redisDatabase.SetAdd(Key.IndexAccountPositions(accountId), order.Id.Value, CommandFlags.FireAndForget);
            this.redisDatabase.HashSet(Key.IndexOrderTrader, new[] { new HashEntry(order.Id.Value, traderId.Value) }, CommandFlags.FireAndForget);
            this.redisDatabase.HashSet(Key.IndexOrderAccount, new[] { new HashEntry(order.Id.Value, accountId.Value) }, CommandFlags.FireAndForget);
            this.redisDatabase.HashSet(Key.IndexOrderPosition, new[] { new HashEntry(order.Id.Value, positionId.Value) }, CommandFlags.FireAndForget);
            this.redisDatabase.HashSet(Key.IndexOrderStrategy, new[] { new HashEntry(order.Id.Value, strategyId.Value) }, CommandFlags.FireAndForget);
            this.redisDatabase.HashSet(Key.IndexPositionTrader, new[] { new HashEntry(positionId.Value, traderId.Value) }, CommandFlags.FireAndForget);
            this.redisDatabase.HashSet(Key.IndexPositionAccount, new[] { new HashEntry(positionId.Value, positionId.Value) }, CommandFlags.FireAndForget);
            this.redisDatabase.HashSet(Key.IndexPositionStrategy, new[] { new HashEntry(positionId.Value, positionId.Value) }, CommandFlags.FireAndForget);
            this.redisDatabase.SetAdd(Key.IndexPositionOrders(positionId), order.Id.Value, CommandFlags.FireAndForget);
            this.redisDatabase.SetAdd(Key.IndexOrders, order.Id.Value, CommandFlags.FireAndForget);

            this.redisDatabase.ListRightPush(Key.Order(order.Id), this.eventSerializer.Serialize(order.LastEvent), When.Always, CommandFlags.FireAndForget);

            this.CachedOrders[order.Id] = order;

            this.Log.Debug($"Added Order(Id={order.Id.Value}).");

            return CommandResult.Ok();
        }

        /// <inheritdoc />
        public CommandResult AddPosition(Position position)
        {
            if (this.CachedPositions.ContainsKey(position.Id))
            {
                return CommandResult.Fail($"The {position.Id} already existed in the cache (was not unique).");
            }

            this.redisDatabase.SetAdd(Key.IndexPositions, position.Id.Value, CommandFlags.FireAndForget);
            if (position.IsOpen)
            {
                this.redisDatabase.SetAdd(Key.IndexPositionsOpen, position.Id.Value, CommandFlags.FireAndForget);
            }
            else
            {
                // The position should always be open when being added
                this.Log.Error($"The added {position} was not open.");
            }

            this.redisDatabase.ListRightPush(Key.Position(position.Id), this.eventSerializer.Serialize(position.LastEvent), When.Always, CommandFlags.FireAndForget);

            this.CachedPositions[position.Id] = position;

            this.Log.Debug($"Added Position(Id={position.Id.Value}).");

            return CommandResult.Ok();
        }

        /// <inheritdoc />
        public void UpdateAccount(Account account)
        {
            this.redisDatabase.ListRightPush(Key.Account(account.Id), this.eventSerializer.Serialize(account.LastEvent), When.Always, CommandFlags.FireAndForget);
        }

        /// <inheritdoc />
        public void UpdateOrder(Order order)
        {
            if (order.IsWorking)
            {
                this.redisDatabase.SetAdd(Key.IndexOrdersWorking, order.Id.Value, CommandFlags.FireAndForget);
                this.redisDatabase.SetRemove(Key.IndexOrdersCompleted, order.Id.Value, CommandFlags.FireAndForget);
            }
            else if (order.IsCompleted)
            {
                this.redisDatabase.SetAdd(Key.IndexOrdersCompleted, order.Id.Value, CommandFlags.FireAndForget);
                this.redisDatabase.SetRemove(Key.IndexOrdersWorking, order.Id.Value, CommandFlags.FireAndForget);
            }

            this.redisDatabase.ListRightPush(Key.Order(order.Id), this.eventSerializer.Serialize(order.LastEvent), When.Always, CommandFlags.FireAndForget);
        }

        /// <inheritdoc />
        public void UpdatePosition(Position position)
        {
            if (position.IsOpen)
            {
                this.redisDatabase.SetAdd(Key.IndexPositionsOpen, position.Id.Value, CommandFlags.FireAndForget);
                this.redisDatabase.SetRemove(Key.IndexPositionsClosed, position.Id.Value, CommandFlags.FireAndForget);
            }
            else if (position.IsClosed)
            {
                this.redisDatabase.SetAdd(Key.IndexPositionsClosed, position.Id.Value, CommandFlags.FireAndForget);
                this.redisDatabase.SetRemove(Key.IndexPositionsOpen, position.Id.Value, CommandFlags.FireAndForget);
            }

            this.redisDatabase.ListRightPush(Key.Position(position.Id), this.eventSerializer.Serialize(position.LastEvent), When.Always, CommandFlags.FireAndForget);
        }

        /// <inheritdoc />
        public override TraderId? GetTraderId(OrderId orderId)
        {
            var traderId = this.redisDatabase.HashGet(Key.IndexOrderTrader, orderId.Value);
            return traderId == RedisValue.Null
                ? null
                : TraderId.FromString(traderId);
        }

        /// <inheritdoc />
        public override TraderId? GetTraderId(PositionId positionId)
        {
            var traderId = this.redisDatabase.HashGet(Key.IndexPositionTrader, positionId.Value);
            return traderId == RedisValue.Null
                ? null
                : TraderId.FromString(traderId);
        }

        /// <inheritdoc />
        public override AccountId? GetAccountId(OrderId orderId)
        {
            var accountId = this.redisDatabase.HashGet(Key.IndexOrderAccount, orderId.Value);
            return accountId == RedisValue.Null
                ? null
                : AccountId.FromString(accountId);
        }

        /// <inheritdoc />
        public override AccountId? GetAccountId(PositionId positionId)
        {
            var accountId = this.redisDatabase.HashGet(Key.IndexPositionAccount, positionId.Value);
            return accountId == RedisValue.Null
                ? null
                : AccountId.FromString(accountId);
        }

        /// <inheritdoc />
        public override PositionId? GetPositionId(OrderId orderId)
        {
            var idValue = this.redisDatabase.HashGet(Key.IndexOrderPosition, orderId.Value);
            return idValue == RedisValue.Null
                ? null
                : new PositionId(idValue);
        }

        /// <inheritdoc />
        public override ICollection<TraderId> GetTraderIds()
        {
            return SetFactory.ConvertToSet(this.redisDatabase.SetMembers(Key.IndexTraders).ToArray(), TraderId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<AccountId> GetAccountIds()
        {
            var accountIds = this.redisServer.Keys(pattern: Key.Accounts)
                .Select(k => k.ToString().Split(':').Last())
                .ToArray();

            return SetFactory.ConvertToSet(accountIds, AccountId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<StrategyId> GetStrategyIds(TraderId traderId)
        {
            return SetFactory.ConvertToSet(this.redisDatabase.SetMembers(Key.IndexTraderStrategies(traderId)), StrategyId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<OrderId> GetOrderIds()
        {
            return SetFactory.ConvertToSet(this.redisDatabase.SetMembers(Key.IndexOrders), OrderId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<OrderId> GetOrderIds(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            var orderIdValues = filterStrategyId is null
                ? this.redisDatabase.SetMembers(Key.IndexTraderOrders(traderId))
                : this.redisDatabase.SetMembers(Key.IndexTraderStrategyOrders(traderId, filterStrategyId));

            return SetFactory.ConvertToSet(orderIdValues, OrderId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<OrderId> GetOrderWorkingIds()
        {
            return SetFactory.ConvertToSet(this.redisDatabase.SetMembers(Key.IndexOrdersWorking), OrderId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<OrderId> GetOrderWorkingIds(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            var orderIdValues = filterStrategyId is null
                ? this.GetIntersection(Key.IndexOrdersWorking, Key.IndexTraderOrders(traderId))
                : this.GetIntersection(Key.IndexOrdersWorking, Key.IndexTraderStrategyOrders(traderId, filterStrategyId));

            return SetFactory.ConvertToSet(orderIdValues, OrderId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<OrderId> GetOrderCompletedIds()
        {
            return SetFactory.ConvertToSet(this.redisDatabase.SetMembers(Key.IndexOrdersCompleted), OrderId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<OrderId> GetOrderCompletedIds(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            var orderIdValues = filterStrategyId is null
                ? this.GetIntersection(Key.IndexOrdersCompleted, Key.IndexTraderOrders(traderId))
                : this.GetIntersection(Key.IndexOrdersCompleted, Key.IndexTraderStrategyOrders(traderId, filterStrategyId));

            return SetFactory.ConvertToSet(orderIdValues, OrderId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<PositionId> GetPositionIds()
        {
            return SetFactory.ConvertToSet(this.redisDatabase.SetMembers(Key.IndexPositions), PositionId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<PositionId> GetPositionIds(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            var positionIdValues = filterStrategyId is null
                ? this.redisDatabase.SetMembers(Key.IndexTraderPositions(traderId))
                : this.redisDatabase.SetMembers(Key.IndexTraderStrategyPositions(traderId, filterStrategyId));

            return SetFactory.ConvertToSet(positionIdValues, PositionId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<PositionId> GetPositionOpenIds()
        {
            return SetFactory.ConvertToSet(this.redisDatabase.SetMembers(Key.IndexPositionsOpen), PositionId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<PositionId> GetPositionOpenIds(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            var positionIdValues = filterStrategyId is null
                ? this.GetIntersection(Key.IndexPositionsOpen, Key.IndexTraderPositions(traderId))
                : this.GetIntersection(Key.IndexPositionsOpen, Key.IndexTraderStrategyPositions(traderId, filterStrategyId));

            return SetFactory.ConvertToSet(positionIdValues, PositionId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<PositionId> GetPositionClosedIds()
        {
            return SetFactory.ConvertToSet(this.redisDatabase.SetMembers(Key.IndexPositionsClosed), PositionId.FromString);
        }

        /// <inheritdoc />
        public override ICollection<PositionId> GetPositionClosedIds(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            var positionIdValues = filterStrategyId is null
                ? this.GetIntersection(Key.IndexPositionsClosed, Key.IndexTraderPositions(traderId))
                : this.GetIntersection(Key.IndexPositionsClosed, Key.IndexTraderStrategyPositions(traderId, filterStrategyId));

            return SetFactory.ConvertToSet(positionIdValues, PositionId.FromString);
        }

        private RedisValue[] GetIntersection(string setKey1, string setKey2)
        {
            return this.redisDatabase.SetCombine(SetOperation.Intersect, setKey1, setKey2);
        }
    }
}
