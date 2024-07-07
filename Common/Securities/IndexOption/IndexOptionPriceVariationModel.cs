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

namespace QuantConnect.Securities.IndexOption
{
    /// <summary>
    /// The index option price variation model
    /// </summary>
    public class IndexOptionPriceVariationModel : IPriceVariationModel
    {
        /// <summary>
        /// Get the minimum price variation from a security
        /// </summary>
        /// <param name="parameters">An object containing the method parameters</param>
        /// <returns>Decimal minimum price variation of a given security</returns>
        public decimal GetMinimumPriceVariation(GetMinimumPriceVariationParameters parameters)
        {
            return IndexOptionSymbolProperties.MinimumPriceVariationForPrice(
                parameters.Security.Symbol,
                parameters.ReferencePrice
            );
        }
    }
}
