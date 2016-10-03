﻿/*
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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Regression Channel indicator extends the <see cref="LeastSquaresMovingAverage"/>
    /// with the inclusion of two (upper and lower) channel lines that are distanced from
    /// the linear regression line by a user defined number of standard deviations. 
    /// </summary>
    public class RegressionChannel : Indicator
    {
        /// <summary>
        /// Gets the standard deviation
        /// </summary>
        private IndicatorBase<IndicatorDataPoint> _standardDeviation;

        /// <summary>
        /// Gets the linear regression line
        /// </summary>
        public LeastSquaresMovingAverage LinearRegressionLine { get; private set; }
        
        /// <summary>
        /// Gets the upper channel line (linear regression line + k * stdDev)
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> UpperChannelLine { get; private set; }
        
        /// <summary>
        /// Gets the lower channel line (linear regression line - k * stdDev)
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> LowerChannelLine { get; private set; }

        /// <summary>
        /// The point where the regression line crosses the y-axis (price-axis)
        /// </summary>
        public WindowIndicator<IndicatorDataPoint> Intercept { get { return LinearRegressionLine.Intercept; } }

        /// <summary>
        /// The regression line slope
        /// </summary>
        public WindowIndicator<IndicatorDataPoint> Slope { get { return LinearRegressionLine.Slope; } }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return _standardDeviation.IsReady && LinearRegressionLine.IsReady && UpperChannelLine.IsReady && LowerChannelLine.IsReady; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegressionChannel"/> class.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The number of data points to hold in the window</param>
        /// <param name="k">The number of standard deviations specifying the distance between the linear regression and upper or lower channel lines</param>
        public RegressionChannel(string name, int period, decimal k)
            : base(name)
        {
            _standardDeviation = new StandardDeviation(period);
            LinearRegressionLine = new LeastSquaresMovingAverage(name + "_LinearRegressionLine", period);
            LowerChannelLine = LinearRegressionLine.Minus(_standardDeviation.Times(k), name + "_LowerChannelLine");
            UpperChannelLine = LinearRegressionLine.Plus(_standardDeviation.Times(k), name + "_UpperChannelLine");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeastSquaresMovingAverage"/> class.
        /// </summary>
        /// <param name="period">The number of data points to hold in the window.</param>
        /// <param name="k">The number of standard deviations specifying the distance between the linear regression and upper or lower channel lines</param>
        public RegressionChannel(int period, decimal k)
            : this(string.Format("RC({0},{1})", period, k), period, k)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>
        /// A new value for this indicator
        /// </returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _standardDeviation.Update(input);
            LinearRegressionLine.Update(input);
            LowerChannelLine.Update(input);
            UpperChannelLine.Update(input);

            return LinearRegressionLine.Current;
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators (StandardDeviation, LowerBand, MiddleBand, UpperBand)
        /// </summary>
        public override void Reset()
        {
            _standardDeviation.Reset();
            LinearRegressionLine.Reset();
            LowerChannelLine.Reset();
            UpperChannelLine.Reset();
            base.Reset();
        }
    }
}