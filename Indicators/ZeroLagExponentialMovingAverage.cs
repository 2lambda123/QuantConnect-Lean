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
    /// Represents the zero lag moving average indicator (ZLEMA)
    /// ie a technical indicator that aims is to eliminate the inherent lag associated to all trend 
    /// following indicators which average a price over time.
    /// </summary>
    public class ZeroLagExponentialMovingAverage : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// An exponential moving average is used
        /// </summary>
        private readonly int _period;
        private readonly ExponentialMovingAverage _ema;
        private readonly int _lag;
        private readonly RollingWindow<IndicatorDataPoint> _rollingWindow;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => (Samples >= _lag + 1) && _ema.IsReady;

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _ema.Reset();
            _rollingWindow.Reset();
            base.Reset();
        }

        /// <summary>
        /// Initializes a new instance of the ZeroLagMovingAverage class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the ZLEMA</param>
        public ZeroLagExponentialMovingAverage(string name, int period)
            : base(name, period)
        {
            _period = period;
            _lag = (int)Math.Round((period - 1) / 2.0);
            _ema = new ExponentialMovingAverage(name + "_EMA", period);
            _rollingWindow = new RollingWindow<IndicatorDataPoint>(_lag + 1);
        }

        /// <summary>
        /// Initializes a new instance of the ZeroLagMovingAverage class with the default name and period
        /// </summary>
        /// <param name="period">The period of the ZLEMA</param>
        public ZeroLagExponentialMovingAverage(int period)
            : this($"ZLEMA({period})", period)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            _rollingWindow.Add(input);

            if (Samples >= _lag + 1)
            {
                _ema.Update(input.Time, input.Value + (input.Value - _rollingWindow[_lag].Value));
            }
            else
            {
                return 0;
            }

            if(_ema.IsReady)
            {
                return _ema.Current.Value;
                
            } else
            {
                return 0;
            }
        }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period + (int)Math.Floor(((float)_period) / 2);
    }
}
