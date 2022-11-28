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
 *
*/

using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// TDAmeritrade
    /// </summary>
    public class TDAmeritradeBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets();

        /// <summary>
        /// Constructor for TDAmeritrade brokerage model
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public TDAmeritradeBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
        {

        }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
        /// </remarks>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            if (!IsValidOrderSize(security, order.Quantity, out message))
            {
                return false;
            }

            message = null;
            if (!new[] { SecurityType.Equity, SecurityType.Option }.Contains(security.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    StringExtensions.Invariant($"The {nameof(TDAmeritradeBrokerageModel)} does not support {security.Type} security type.")
                );

                return false;
            }

            if (!new[] { OrderType.Market, OrderType.Limit, OrderType.StopMarket, OrderType.StopLimit }.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    StringExtensions.Invariant($"{order.Type} order is not supported by TDAmeritrade. Currently, only Market Order is supported.")
                );

                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// TDAmeritrade does not support update of orders
        /// </summary>
        /// <param name="security">Security</param>
        /// <param name="order">Order that should be updated</param>
        /// <param name="request">Update request</param>
        /// <param name="message">Outgoing message</param>
        /// <returns>Always false as TDAmeritrade does not support update of orders</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, 0, "Brokerage does not support update. You must cancel and re-create instead."); ;
            return false;
        }

        /// <summary>
        /// Provides TDAmeritrade fee model
        /// </summary>
        /// <param name="security">Security</param>
        /// <returns>TDAmeritrade fee model</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new TDAmeritradeFeeModel();
        }

        /// <summary>
        /// Get default markets and specify TDAmeritrade as crypto market
        /// </summary>
        /// <returns>default markets</returns>
        private static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets()
        {
            var map = DefaultMarketMap.ToDictionary();
            map[SecurityType.Equity] = Market.USA;
            return map.ToReadOnlyDictionary();
        }
    }
}
