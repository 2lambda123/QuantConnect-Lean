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

using System;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Algorithm.Framework.Risk
{
    /// <summary>
    /// Provides an implementation of <see cref="IRiskManagementModel"/> that limits the maximum possible loss
    /// measured from the highest unrealized profit
    /// </summary>
    public class TrailingStopRiskManagementModel : RiskManagementModel
    {
        private readonly decimal _maximumDrawdownPercent;
        private readonly Dictionary<Symbol, decimal> _trailingHighs = new Dictionary<Symbol, decimal>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TrailingStopRiskManagementModel"/> class
        /// </summary>
        /// <param name="maximumDrawdownPercent">The maximum percentage relative drawdown allowed for algorithm portfolio compared with the highest unrealized profit, defaults to 5% drawdown per security</param>
        public TrailingStopRiskManagementModel(decimal maximumDrawdownPercent = 0.05m)
        {
            _maximumDrawdownPercent = Math.Abs(maximumDrawdownPercent);
        }

        /// <summary>
        /// Manages the algorithm's risk at each time step
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The current portfolio targets to be assessed for risk</param>
        public override IEnumerable<IPortfolioTarget> ManageRisk(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            foreach (var kvp in algorithm.Securities)
            {
                var symbol = kvp.Key;
                var security = kvp.Value;

                // Remove if not invested
                if (!security.Invested)
                {
                    _trailingHighs.Remove(symbol);
                    continue;
                }

                var high = security.High;
                decimal maxHigh;

                if (!_trailingHighs.TryGetValue(symbol, out maxHigh))
                {
                    _trailingHighs.Add(symbol, high);
                    continue;
                }

                // Check for new max high and update
                if (maxHigh < high)
                {
                    _trailingHighs[symbol] = high;
                    continue;
                }

                if (security.Low < maxHigh * (1m - _maximumDrawdownPercent))
                {
                    // liquidate
                    yield return new PortfolioTarget(security.Symbol, 0);
                }
            }
        }
    }
}
