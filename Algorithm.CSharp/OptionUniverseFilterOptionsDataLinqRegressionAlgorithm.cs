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

using QuantConnect.Securities.Option;
using System.Collections.Generic;
using static QuantConnect.Securities.OptionFilterUniverseEx;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm demonstrating the option universe filter feature that allows accessing the option universe data,
    /// including greeks, open interest and implied volatility, and filtering the contracts based on this data, in a Linq fashion.
    /// </summary>
    public class OptionUniverseFilterOptionsDataLinqRegressionAlgorithm : OptionUniverseFilterGreeksRegressionAlgorithm
    {
        protected override void SetOptionFilter(Option security)
        {
            // The filter used for the option security will be equivalent to the following commented one below,
            // but it is more flexible and allows for more complex filtering:

            //security.SetFilter(u => u
            //    .Delta(0.5m, 1.5m)
            //    .Gamma(0.0001m, 0.0006m)
            //    .Vega(0.01m, 1.5m)
            //    .Theta(-2.0m, -0.5m)
            //    .Rho(0.5m, 3.0m)
            //    .ImpliedVolatility(1.0m, 3.0m)
            //    .OpenInterest(100, 500));

            security.SetFilter(u => u
                // This requires the following using statement in order to avoid ambiguity with the System.Linq namespace:
                // using static QuantConnect.Securities.OptionFilterUniverseEx;
                .Where(contractData =>
                {
                    // The contracts received here will already be filtered by the strikes and expirations,
                    // since those filters where applied before this one.

                    // Can access the contract data here and do some filtering based on it is needed:
                    var greeks = contractData.Greeks;
                    var iv = contractData.ImpliedVolatility;
                    var openInterest = contractData.OpenInterest;

                    // More complex math can be done here for filtering, but will be simple here for demonstration sake:
                    return greeks.Delta > 0.5m && greeks.Delta < 1.5m &&
                           greeks.Gamma > 0.0001m && greeks.Gamma < 0.0006m &&
                           greeks.Vega > 0.01m && greeks.Vega < 1.5m &&
                           greeks.Theta > -2.0m && greeks.Theta < -0.5m &&
                           greeks.Rho > 0.5m && greeks.Rho < 3.0m &&
                           iv > 1.0m && iv < 3.0m &&
                           openInterest > 100 && openInterest < 500;
                })
                .Select(contractData =>
                {
                    // Can also select the contracts here, returning a different or mapped one if needed (e.g. the mirror contract call <-> put):
                    return contractData.Symbol;
                }));
        }

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };
    }
}
