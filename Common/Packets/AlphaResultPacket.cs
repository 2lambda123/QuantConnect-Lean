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

using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Orders;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Provides a packet type for transmitting alpha insights data
    /// </summary>
    public class AlphaResultPacket : Packet
    {
        /// <summary>
        /// The user's id that deployed the alpha stream
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// The deployed alpha id. This is the id generated upon submssion to the alpha marketplace.
        /// If this is a user backtest or live algo then this will not be specified
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AlphaId { get; set; }

        /// <summary>
        /// The algorithm's unique identifier
        /// </summary>
        public string AlgorithmId { get; set; }

        /// <summary>
        /// The generated insights
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<Insight> Insights { get; set; }

        /// <summary>
        /// The generated OrderEvents
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<OrderEvent> OrderEvents { get; set; }

        /// <summary>
        /// The new or updated Orders
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<Order> Orders { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaResultPacket"/> class
        /// </summary>
        public AlphaResultPacket()
            : base(PacketType.AlphaResult) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaResultPacket"/> class
        /// </summary>
        /// <param name="algorithmId">The algorithm's unique identifier</param>
        /// <param name="userId">The user's id</param>
        /// <param name="insights">Alphas generated by the algorithm</param>
        /// <param name="orderEvents">OrderEvents generated by the algorithm</param>
        /// <param name="orders">Orders generated or updated by the algorithm</param>
        public AlphaResultPacket(
            string algorithmId,
            int userId,
            List<Insight> insights = null,
            List<OrderEvent> orderEvents = null,
            List<Order> orders = null
        )
            : base(PacketType.AlphaResult)
        {
            UserId = userId;
            AlgorithmId = algorithmId;
            Insights = insights;
            OrderEvents = orderEvents;
            Orders = orders;
        }
    }
}
