/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Interfaces;
using QuantConnect.Orders.Serialization;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using QuantConnect.Securities.Positions;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Order struct for placing new trade
    /// </summary>
    public abstract class Order: OrderParameters
    {
        /// <summary>
        /// Added a default constructor for JSON Deserialization:
        /// </summary>
        protected Order()
        {
            Time = new DateTime();
            PriceCurrency = string.Empty;
            Symbol = Symbol.Empty;
            Status = OrderStatus.None;
            Tag = string.Empty;
            BrokerId = new List<string>();
            Properties = new OrderProperties();
        }

        /// <summary>
        /// New order constructor
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="tag">User defined data tag for this order</param>
        /// <param name="properties">The order properties for this order</param>
        protected Order(Symbol symbol, decimal quantity, DateTime time, string tag = "", IOrderProperties properties = null)
        {
            Time = time;
            PriceCurrency = string.Empty;
            Quantity = quantity;
            Symbol = symbol;
            Status = OrderStatus.None;
            Tag = tag;
            BrokerId = new List<string>();
            Properties = properties ?? new OrderProperties();
        }

        /// <summary>
        /// Creates an enumerable containing each position resulting from executing this order.
        /// </summary>
        /// <remarks>
        /// This is provided in anticipation of a new combo order type that will need to override this method,
        /// returning a position for each 'leg' of the order.
        /// </remarks>
        /// <returns>An enumerable of positions matching the results of executing this order</returns>
        public virtual IEnumerable<IPosition> CreatePositions(SecurityManager securities)
        {
            var security = securities[Symbol];
            yield return new Position(security, Quantity);
        }

        /// <summary>
        /// Gets the value of this order at the given market price in units of the account currency
        /// NOTE: Some order types derive value from other parameters, such as limit prices
        /// </summary>
        /// <param name="security">The security matching this order's symbol</param>
        /// <returns>The value of this order given the current market price</returns>
        public decimal GetValue(Security security)
        {
            var value = GetValueImpl(security);
            return value*security.QuoteCurrency.ConversionRate*security.SymbolProperties.ContractMultiplier;
        }

        /// <summary>
        /// Gets the order value in units of the security's quote currency for a single unit.
        /// A single unit here is a single share of stock, or a single barrel of oil, or the
        /// cost of a single share in an option contract.
        /// </summary>
        /// <param name="security">The security matching this order's symbol</param>
        protected abstract decimal GetValueImpl(Security security);

        /// <summary>
        /// Gets a new unique incremental id for this order
        /// </summary>
        /// <returns>Returns a new id for this order</returns>
        internal int GetNewId()
        {
            return Interlocked.Increment(ref _incrementalId);
        }

        /// <summary>
        /// Modifies the state of this order to match the update request
        /// </summary>
        /// <param name="request">The request to update this order object</param>
        public virtual void ApplyUpdateOrderRequest(UpdateOrderRequest request)
        {
            if (request.OrderId != Id)
            {
                throw new ArgumentException("Attempted to apply updates to the incorrect order!");
            }
            if (request.Quantity.HasValue)
            {
                Quantity = request.Quantity.Value;
            }
            if (request.Tag != null)
            {
                Tag = request.Tag;
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            var tag = string.IsNullOrEmpty(Tag) ? string.Empty : $": {Tag}";
            return Invariant($"OrderId: {Id} (BrokerId: {string.Join(",", BrokerId)}) {Status} {Type} order for {Quantity} unit{(Quantity == 1 ? "" : "s")} of {Symbol}{tag}");
        }

        /// <summary>
        /// Creates a deep-copy clone of this order
        /// </summary>
        /// <returns>A copy of this order</returns>
        public abstract Order Clone();

        /// <summary>
        /// Copies base Order properties to the specified order
        /// </summary>
        /// <param name="order">The target of the copy</param>
        protected void CopyTo(Order order)
        {
            order.Id = Id;
            order.Time = Time;
            order.LastFillTime = LastFillTime;
            order.LastUpdateTime = LastUpdateTime;
            order.CanceledTime = CanceledTime;
            order.BrokerId = BrokerId.ToList();
            order.ContingentId = ContingentId;
            order.Price = Price;
            order.PriceCurrency = PriceCurrency;
            order.Quantity = Quantity;
            order.Status = Status;
            order.Symbol = Symbol;
            order.Tag = Tag;
            order.Properties = Properties.Clone();
            order.OrderSubmissionData = OrderSubmissionData?.Clone();
        }

        /// <summary>
        /// Creates a new Order instance from a SerializedOrder instance
        /// </summary>
        /// <remarks>Used by the <see cref="SerializedOrderJsonConverter"/></remarks>
        public static Order FromSerialized(SerializedOrder serializedOrder)
        {
            var sid = SecurityIdentifier.Parse(serializedOrder.Symbol);
            var symbol = new Symbol(sid, sid.Symbol);

            TimeInForce timeInForce = null;
            var type = System.Type.GetType($"QuantConnect.Orders.TimeInForces.{serializedOrder.TimeInForceType}", throwOnError: false, ignoreCase: true);
            if (type != null)
            {
                timeInForce = (TimeInForce) Activator.CreateInstance(type, true);
                if (timeInForce is GoodTilDateTimeInForce)
                {
                    var expiry = QuantConnect.Time.UnixTimeStampToDateTime(serializedOrder.TimeInForceExpiry.Value);
                    timeInForce = new GoodTilDateTimeInForce(expiry);
                }
            }

            var createdTime = QuantConnect.Time.UnixTimeStampToDateTime(serializedOrder.CreatedTime);

            var order = CreateOrder(serializedOrder.OrderId, serializedOrder.Type, symbol, serializedOrder.Quantity,
                DateTime.SpecifyKind(createdTime, DateTimeKind.Utc),
                serializedOrder.Tag,
                new OrderProperties { TimeInForce = timeInForce },
                serializedOrder.LimitPrice ?? 0,
                serializedOrder.StopPrice ?? 0,
                serializedOrder.TriggerPrice ?? 0);

            order.OrderSubmissionData = new OrderSubmissionData(serializedOrder.SubmissionBidPrice,
                serializedOrder.SubmissionAskPrice,
                serializedOrder.SubmissionLastPrice);

            order.BrokerId = serializedOrder.BrokerId;
            order.ContingentId = serializedOrder.ContingentId;
            order.Price = serializedOrder.Price;
            order.PriceCurrency = serializedOrder.PriceCurrency;
            order.Status = serializedOrder.Status;

            if (serializedOrder.LastFillTime.HasValue)
            {
                var time = QuantConnect.Time.UnixTimeStampToDateTime(serializedOrder.LastFillTime.Value);
                order.LastFillTime = DateTime.SpecifyKind(time, DateTimeKind.Utc);
            }
            if (serializedOrder.LastUpdateTime.HasValue)
            {
                var time = QuantConnect.Time.UnixTimeStampToDateTime(serializedOrder.LastUpdateTime.Value);
                order.LastUpdateTime = DateTime.SpecifyKind(time, DateTimeKind.Utc);
            }
            if (serializedOrder.CanceledTime.HasValue)
            {
                var time = QuantConnect.Time.UnixTimeStampToDateTime(serializedOrder.CanceledTime.Value);
                order.CanceledTime = DateTime.SpecifyKind(time, DateTimeKind.Utc);
            }

            return order;
        }

        /// <summary>
        /// Creates an <see cref="Order"/> to match the specified <paramref name="request"/>
        /// </summary>
        /// <param name="request">The <see cref="SubmitOrderRequest"/> to create an order for</param>
        /// <returns>The <see cref="Order"/> that matches the request</returns>
        public static Order CreateOrder(SubmitOrderRequest request)
        {
            return CreateOrder(request.OrderId, request.OrderType, request.Symbol, request.Quantity, request.Time,
                request.Tag, request.OrderProperties, request.LimitPrice, request.StopPrice, request.TriggerPrice);
        }

        private static Order CreateOrder(int orderId, OrderType type, Symbol symbol, decimal quantity, DateTime time,
            string tag, IOrderProperties properties, decimal limitPrice, decimal stopPrice, decimal triggerPrice)
        {
            Order order;
            switch (type)
            {
                case OrderType.Market:
                    order = new MarketOrder(symbol, quantity, time, tag, properties);
                    break;

                case OrderType.Limit:
                    order = new LimitOrder(symbol, quantity, limitPrice, time, tag, properties);
                    break;

                case OrderType.StopMarket:
                    order = new StopMarketOrder(symbol, quantity, stopPrice, time, tag, properties);
                    break;

                case OrderType.StopLimit:
                    order = new StopLimitOrder(symbol, quantity, stopPrice, limitPrice, time, tag, properties);
                    break;
                
                case OrderType.LimitIfTouched:
                    order = new LimitIfTouchedOrder(symbol, quantity, triggerPrice, limitPrice, time, tag, properties);
                    break;

                case OrderType.MarketOnOpen:
                    order = new MarketOnOpenOrder(symbol, quantity, time, tag, properties);
                    break;

                case OrderType.MarketOnClose:
                    order = new MarketOnCloseOrder(symbol, quantity, time, tag, properties);
                    break;

                case OrderType.OptionExercise:
                    order = new OptionExerciseOrder(symbol, quantity, time, tag, properties);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            order.Status = OrderStatus.New;
            order.Id = orderId;
            return order;
        }
    }
}
