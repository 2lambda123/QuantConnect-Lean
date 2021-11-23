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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Tests.Engine;
using QuantConnect.Algorithm;
using QuantConnect.Lean.Engine.Results;
using Python.Runtime;
using System.Threading;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityTransactionManagerTests
    {
        private IResultHandler _resultHandler;

        [SetUp]
        public void SetUp()
        {
            _resultHandler = new TestResultHandler(Console.WriteLine);
        }

        [Test]
        public void WorksProperlyWithPyObjects()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var spySecurity = algorithm.AddEquity("SPY");
            var ibmSecurity = algorithm.AddEquity("IBM");
            spySecurity.Exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            ibmSecurity.Exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            spySecurity.SetMarketPrice(new Tick { Value = 270m });
            ibmSecurity.SetMarketPrice(new Tick { Value = 270m });
            algorithm.SetFinishedWarmingUp();

            var transactionHandler = new BrokerageTransactionHandler();

            transactionHandler.Initialize(algorithm, new BacktestingBrokerage(algorithm), _resultHandler);
            Thread.Sleep(250);
            algorithm.Transactions.SetOrderProcessor(transactionHandler);

            var spy = spySecurity.Symbol;
            var ibm = ibmSecurity.Symbol;

            // this order should timeout (no fills received within 5 seconds)
            algorithm.SetHoldings(spy, 0.5m);
            algorithm.SetHoldings(ibm, 0.5m);
            Thread.Sleep(2000);      

            Func<Order, bool> basicOrderFilter = x => true;
            Func<OrderTicket, bool> basicOrderTicketFilter = x => true;
            using (Py.GIL())
            {
                var orders = algorithm.Transactions.GetOrders(basicOrderFilter.ToPython());
                var orderTickets = algorithm.Transactions.GetOrderTickets(basicOrderTicketFilter.ToPython());
                var openOrders = algorithm.Transactions.GetOpenOrders(basicOrderFilter.ToPython());
                var openOrdersTickets = algorithm.Transactions.GetOpenOrderTickets(basicOrderTicketFilter.ToPython());
                var openOrdersRemaining = algorithm.Transactions.GetOpenOrdersRemainingQuantity(basicOrderTicketFilter.ToPython());
                Assert.AreEqual(2, orders.Count());
                Assert.AreEqual(2, orderTickets.Count());
                Assert.AreEqual(2, openOrders.Count);
                Assert.AreEqual(2, openOrdersTickets.Count());
                Assert.AreEqual(368, openOrdersRemaining);
            }
        }
    }
}
