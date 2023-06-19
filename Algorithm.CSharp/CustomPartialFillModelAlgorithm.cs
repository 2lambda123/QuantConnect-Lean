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

using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm that implements a fill model with partial fills
    /// </summary>
    /// <meta name="tag" content="transaction fees and slippage" />
    /// <meta name="tag" content="custom fill models" />
    public class CustomPartialFillModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private SecurityHolding _holdings;

        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);
            SetEndDate(2019, 3, 1);

            var equity = AddEquity("SPY", Resolution.Hour);
            _spy = equity.Symbol;
            _holdings = equity.Holdings;

            // Set the fill model
            equity.SetFillModel(new CustomPartialFillModel(this));
        }

        public override void OnData(Slice data)
        {
            var openOrders = Transactions.GetOpenOrders(_spy);
            if (openOrders.Count != 0) return;

            if (Time.Day > 10 && _holdings.Quantity <= 0)
            {
                MarketOrder(_spy, 105, true);
            }
            else if (Time.Day > 20 && _holdings.Quantity >= 0)
            {
                MarketOrder(_spy, -100, true);
            }
        }

        /// <summary>
        /// Implements a custom fill model that inherit from FillModel. Override the MarketFill method to simulate partially fill orders
        /// </summary>
        internal class CustomPartialFillModel : FillModel
        {
            private readonly QCAlgorithm _algorithm;
            private readonly Dictionary<int, decimal> _absoluteRemainingByOrderId;

            public CustomPartialFillModel(QCAlgorithm algorithm)
                : base()
            {
                _algorithm = algorithm;
                _absoluteRemainingByOrderId = new Dictionary<int, decimal>();
            }

            public override OrderEvent MarketFill(Security asset, MarketOrder order)
            {
                decimal absoluteRemaining;
                if (!_absoluteRemainingByOrderId.TryGetValue(order.Id, out absoluteRemaining))
                {
                    absoluteRemaining = order.AbsoluteQuantity;
                }

                // Create the object
                var fill = base.MarketFill(asset, order);

                // Set the fill amount to the maximum 10-multiple smaller than the order.Quantity for long orders
                // Set the fill amount to the minimum 10-multiple greater than the order.Quantity for short orders
                fill.FillQuantity = Math.Sign(order.Quantity) * 10m * Math.Floor(Math.Abs(order.Quantity) / 10m);

                if (absoluteRemaining < 10 || absoluteRemaining == Math.Abs(fill.FillQuantity))
                {
                    fill.FillQuantity = Math.Sign(order.Quantity) * absoluteRemaining;
                    fill.Status = OrderStatus.Filled;
                    _absoluteRemainingByOrderId.Remove(order.Id);
                }
                else
                {
                    fill.Status = OrderStatus.PartiallyFilled;
                    _absoluteRemainingByOrderId[order.Id] = absoluteRemaining - Math.Abs(fill.FillQuantity);
                    var price = fill.FillPrice;
                    //_algorithm.Debug($"{_algorithm.Time} - Partial Fill - Remaining {_absoluteRemainingByOrderId[order.Id]} Price - {price}");
                }
                return fill;
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 582;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "99"},
            {"Average Win", "0.04%"},
            {"Average Loss", "-0.04%"},
            {"Compounding Annual Return", "2.444%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "0.108"},
            {"Net Profit", "0.395%"},
            {"Sharpe Ratio", "1.131"},
            {"Probabilistic Sharpe Ratio", "52.416%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1.22"},
            {"Alpha", "-0.009"},
            {"Beta", "0.041"},
            {"Annual Standard Deviation", "0.015"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-5.468"},
            {"Tracking Error", "0.115"},
            {"Treynor Ratio", "0.419"},
            {"Total Fees", "$99.00"},
            {"Estimated Strategy Capacity", "$83000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "43.72%"},
            {"OrderListHash", "7121e8aa523e8f05e784af14f4216fdd"}
        };
    }
}
