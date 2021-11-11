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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the Kaufman Efficiency Ratio or Efficiency Ratio (KEF).
    /// 
    /// </summary>
    public class KaufmanEfficiencyRatio : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        private decimal _sumRoc1;
        private decimal _periodRoc;
        private decimal _trailingValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="KaufmanEfficiencyRatio"/> class using the specified name and period.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the Efficiency Ratio (ER)</param>
        public KaufmanEfficiencyRatio(string name, int period)
            : base(name, period + 1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KaufmanEfficiencyRatio"/> class using the specified period.
        /// </summary>
        /// <param name="period">The period of the Kaufman Efficiency Ratio (KEF)</param>
        public KaufmanEfficiencyRatio(int period)
            : this($"KEF({period})", period)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= Period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => Period;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <param name="window">The window for the input history</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            if (Samples < Period)
            {
                if (Samples > 1)
                {
                    _sumRoc1 += Math.Abs(input.Value - window[1].Value);
                }

                return input.Value;
            }

            if (Samples == Period)
            {
                _sumRoc1 += Math.Abs(input.Value - window[1].Value);
            }

            var newTrailingValue = window[Period - 1];
            _periodRoc = input.Value - newTrailingValue.Value;

            if (Samples > Period)
            {
                // Adjust sumROC1:
                // - Remove trailing ROC1 
                // - Add new ROC1
                _sumRoc1 -= Math.Abs(_trailingValue - newTrailingValue.Value);
                _sumRoc1 += Math.Abs(input.Value - window[1].Value);
            }

            // Save the trailing value.
            _trailingValue = newTrailingValue.Value;

            // Calculate the efficiency ratio
            var kaufmanEfficiencyRatio = (_sumRoc1 <= _periodRoc) || _sumRoc1 == 0 ? 1m : Math.Abs(_periodRoc / _sumRoc1);

            return kaufmanEfficiencyRatio;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _sumRoc1 = 0;
            _periodRoc = 0;
            _trailingValue = 0;
            base.Reset();
        }
    }
}
