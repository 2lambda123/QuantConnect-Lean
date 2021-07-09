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
using NUnit.Framework;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class BuyingPowerModelTests
    {
        // Current Order Margin 
        [TestCase(-40, 25, -900, 1, 4)]   	    // -1000
        [TestCase(-36, 25, -880, 1, 1)]  	    // -900
        [TestCase(-35, 25, -900, 1, -1)]   	    // -875
        [TestCase(-34, 25, -880, 1, -1)]       	// -850
        [TestCase(48, 25, 1050, 1, -6)]    	   	// 1200
        [TestCase(49, 25, 1212, 1, -1)]   	    // 1225
        [TestCase(44, 25, 1200, 1, 4)]    	    // 1100
        [TestCase(45, 25, 1250, 1, 5)]    	    // 1125
        [TestCase(80, 25, -1250, 1, -130)]      // 2000
        [TestCase(45.5, 25, 1240, 0.5, 4)]   	// 1125
        [TestCase(45.75, 25, 1285, 0.25, 5.5)]	// 1125
        [TestCase(-40, 25, 1500, 1, 100)]       // -1000
        [TestCase(-40.5, 12.5, 1505, .5, 160.5)]// -506.25
        [TestCase(-40.5, 12.5, 1508, .5, 161)]	// -506.25
        public void OrderAdjustmentCalculation(decimal currentHoldings, decimal perUnitMargin, decimal targetMargin, decimal lotSize, decimal expectedOrderSize)
        {
            var currentHoldingsMargin = currentHoldings * perUnitMargin;

            // Determine the order size to get us to our target margin
            var orderSize = BuyingPowerModel.GetAmountToOrder(currentHoldingsMargin, targetMargin, perUnitMargin, lotSize);
            Assert.AreEqual(expectedOrderSize, orderSize);

            // Determine the final margin and assert we have met our target condition
            var resultMargin = currentHoldingsMargin + (orderSize * perUnitMargin);
            Assert.IsTrue(Math.Abs(resultMargin) <= Math.Abs(targetMargin));
        }
    }
}
