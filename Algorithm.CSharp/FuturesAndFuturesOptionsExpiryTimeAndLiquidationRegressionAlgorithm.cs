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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests delistings for Futures and Futures Options to ensure that they are delisted at the expected times.
    /// </summary>
    public class FuturesAndFuturesOptionsExpiryTimeAndLiquidationRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _invested;
        private int _liquidated;
        private int _delistingsReceived;

        private Symbol _esFuture;
        private Symbol _esFutureOption;

        private readonly DateTime _expectedExpiryWarningTime = new DateTime(2020, 6, 19);
        private readonly DateTime _expectedExpiryDelistingTime = new DateTime(2020, 6, 20);
        private readonly DateTime _expectedLiquidationTime = new DateTime(2020, 6, 19, 16, 6, 0);

        public override void Initialize()
        {
            SetStartDate(2020, 1, 5);
            SetEndDate(2020, 12, 1);
            SetCash(100000);

            var es = QuantConnect.Symbol.CreateFuture(
                Futures.Indices.SP500EMini,
                Market.CME,
                new DateTime(2020, 6, 19));

            var esOption = QuantConnect.Symbol.CreateOption(
                es,
                Market.CME,
                OptionStyle.American,
                OptionRight.Put,
                3400m,
                new DateTime(2020, 6, 19));

            _esFuture = AddFutureContract(es, Resolution.Minute).Symbol;
            _esFutureOption = AddFutureOptionContract(esOption, Resolution.Minute).Symbol;
        }

        public override void OnData(Slice data)
        {
            foreach (var delisting in data.Delistings.Values)
            {
                // Two warnings and two delisted events should be received for a grand total of 4 events.
                _delistingsReceived++;

                if (delisting.Type == DelistingType.Warning &&
                    delisting.Time != _expectedExpiryWarningTime)
                {
                    throw new Exception($"Expiry warning with time {delisting.Time} but is expected to be {_expectedExpiryWarningTime}");
                }
                if (delisting.Type == DelistingType.Warning && delisting.Time != Time.Date)
                {
                    throw new Exception($"Delisting warning received at an unexpected date: {Time} - expected {delisting.Time}");
                }
                if (delisting.Type == DelistingType.Delisted &&
                    delisting.Time != _expectedExpiryDelistingTime)
                {
                    throw new Exception($"Delisting occurred at unexpected time: {delisting.Time} - expected: {_expectedExpiryDelistingTime}");
                }
                if (delisting.Type == DelistingType.Delisted &&
                    delisting.Time != Time.Date)
                {
                    throw new Exception($"Delisting notice received at an unexpected date: {Time} - expected {delisting.Time}");
                }
            }

            if (!_invested &&
                (data.Bars.ContainsKey(_esFuture) || data.QuoteBars.ContainsKey(_esFuture)) &&
                (data.Bars.ContainsKey(_esFutureOption) || data.QuoteBars.ContainsKey(_esFutureOption)))
            {
                _invested = true;

                MarketOrder(_esFuture, 1);
                MarketOrder(_esFutureOption, 1);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Direction != OrderDirection.Sell || orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            // * Future Liquidation
            // * Future Option Exercise
            // * Underlying Future Liquidation
            _liquidated++;
            if (orderEvent.Symbol.SecurityType == SecurityType.FutureOption && _expectedLiquidationTime != Time)
            {
                throw new Exception($"Expected to liquidate option {orderEvent.Symbol} at {_expectedLiquidationTime}, instead liquidated at {Time}");
            }
            if (orderEvent.Symbol.SecurityType == SecurityType.Future && _expectedLiquidationTime.AddMinutes(-1) != Time && _expectedLiquidationTime != Time)
            {
                throw new Exception($"Expected to liquidate future {orderEvent.Symbol} at {_expectedLiquidationTime} (+1 minute), instead liquidated at {Time}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_invested)
            {
                throw new Exception("Never invested in ES futures and FOPs");
            }
            if (_delistingsReceived != 4)
            {
                throw new Exception($"Expected 4 delisting events received, found: {_delistingsReceived}");
            }
            if (_liquidated != 3)
            {
                throw new Exception($"Expected 3 liquidation events, found {_liquidated}");
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "0.13%"},
            {"Average Loss", "-11.33%"},
            {"Compounding Annual Return", "-5.168%"},
            {"Drawdown", "2.400%"},
            {"Expectancy", "-0.494"},
            {"Net Profit", "-2.399%"},
            {"Sharpe Ratio", "-1.288"},
            {"Probabilistic Sharpe Ratio", "0.028%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.01"},
            {"Alpha", "-0.032"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.025"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "1.149"},
            {"Tracking Error", "0.176"},
            {"Treynor Ratio", "29.081"},
            {"Total Fees", "$9.25"},
            {"Fitness Score", "0.011"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.348"},
            {"Return Over Maximum Drawdown", "-2.155"},
            {"Portfolio Turnover", "0.035"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "-1950789983"}
        };
    }
}
