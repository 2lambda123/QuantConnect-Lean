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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class KaufmanEfficiencyRatioTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new KaufmanEfficiencyRatio(10);
        }

        protected override string TestFileName => "spy_ker.txt";

        protected override string TestColumnName => "KER";

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 0.001); }
        }

        [Test]
        public override void ResetsProperly()
        {
            var kef = new KaufmanEfficiencyRatio(2);
            var reference = System.DateTime.Today;

            kef.Update(reference.AddDays(1), 1.0m);
            kef.Update(reference.AddDays(2), 2.0m);
            kef.Update(reference.AddDays(3), 3.0m);
            Assert.IsTrue(kef.IsReady);

            kef.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(kef);
        }
    }
}
