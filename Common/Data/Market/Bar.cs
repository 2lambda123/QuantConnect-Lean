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

using System;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Base Bar Class: Open, High, Low, Close and Period.
    /// </summary>
    public class Bar : IBar
    {
        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// High price of the bar during the time period.
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Low price of the bar during the time period.
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Closing price of the bar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// The period of the bar, (second, minute, daily, ect...)
        /// </summary>
        public TimeSpan Period { get; set; }

        /// <summary>
        /// Default initializer to setup an empty bar.
        /// </summary>
        public Bar()
        {
            Open = 0; 
            High = 0;
            Low = 0; 
            Close = 0;
            Period = TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Initializer to setup an empty bar with a given period.
        /// </summary>
        /// <param name="period">The period of this bar, specify null for default of 1 minute</param>
        public Bar(TimeSpan period)
        {
            Open = 0;
            High = 0;
            Low = 0;
            Close = 0;
            Period = period;
        }

        /// <summary>
        /// Initializer to setup a bar with a given information.
        /// </summary>
        /// <param name="open">Decimal Opening Price</param>
        /// <param name="high">Decimal High Price of this bar</param>
        /// <param name="low">Decimal Low Price of this bar</param>
        /// <param name="close">Decimal Close price of this bar</param>
        /// <param name="period">The period of this bar, specify null for default of 1 minute</param>
        public Bar(decimal open, decimal high, decimal low, decimal close, TimeSpan period)
        {
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Period = period;
        }
    }
}