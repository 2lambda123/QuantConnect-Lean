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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing GH issue 6263. Were some data types would get dropped from the warmup feed
    /// </summary>
    public class WarmupDataTypesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _equityGotTradeBars;
        private bool _equityGotQuoteBars;

        private bool _cryptoGotTradeBars;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 10);

            AddEquity("SPY", Resolution.Minute, fillDataForward: false);
            AddCrypto("BTCUSD", Resolution.Hour, market: Market.Bitfinex, fillDataForward: false);

            SetWarmUp(24, Resolution.Hour);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (IsWarmingUp)
            {
                Debug($"[{Time}] Warmup up. SPY: {Securities["SPY"].Price}. BTCUSD: {Securities["BTCUSD"].Price}");
                _equityGotTradeBars |= data.Bars.ContainsKey("SPY");
                _equityGotQuoteBars |= data.QuoteBars.ContainsKey("SPY");

                _cryptoGotTradeBars |= data.Bars.ContainsKey("BTCUSD");
            }
            else
            {
                if (!Portfolio.Invested)
                {
                    Debug($"[{Time}] Buying stock. SPY: {Securities["SPY"].Price}. BTCUSD: {Securities["BTCUSD"].Price}");
                    SetHoldings("SPY", 1);
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_equityGotTradeBars || !_cryptoGotTradeBars)
            {
                throw new Exception("Did not get any TradeBar during warmup");
            }
            if (!_equityGotQuoteBars)
            {
                throw new Exception("Did not get any QuoteBar during warmup");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 4157;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 41;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "261.966%"},
            {"Drawdown", "1.700%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.063%"},
            {"Sharpe Ratio", "66.449"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.079"},
            {"Beta", "0.998"},
            {"Annual Standard Deviation", "0.242"},
            {"Annual Variance", "0.059"},
            {"Information Ratio", "-199.033"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "16.128"},
            {"Total Fees", "$3.45"},
            {"Estimated Strategy Capacity", "$31000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Fitness Score", "0.112"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "50.354"},
            {"Portfolio Turnover", "0.112"},
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
            {"OrderListHash", "643aa35e39e4652b87e69cd97eea0c9f"}
        };
    }
}
