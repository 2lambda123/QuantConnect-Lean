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

using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class ExanteBrokerageModelTests
    {
        private readonly ExanteBrokerageModel _exanteBrokerageModel = new();
        private readonly Symbol _btcusd = Symbol.Create("BTCUSD", SecurityType.Crypto, "empty");
        private Security _security;

        [SetUp]
        public void Init()
        {
            _security = TestsHelpers.GetSecurity(
                symbol: _btcusd.Value,
                market: _btcusd.ID.Market,
                quoteCurrency: "EUR"
            );
        }

        [Test]
        public void CannotSubmitMarketOrder_IfPriceNotInitialized()
        {
            var order = new Mock<MarketOrder> { Object = { Quantity = 1 } };

            var security = TestsHelpers.GetSecurity(
                symbol: _btcusd.Value,
                market: _btcusd.ID.Market,
                quoteCurrency: "EUR"
            );

            Assert.False(
                _exanteBrokerageModel.CanSubmitOrder(security, order.Object, out var message)
            );
            Assert.NotNull(message);
        }
    }
}
