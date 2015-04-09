﻿using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Live trading data feed used for paper trading.
    /// </summary>
    public class PaperTradingDataFeed : LiveTradingDataFeed
    {
        // Unused for now but will be used later:
        private readonly LiveNodePacket _job;

        //Live data queue stream:
        private readonly IDataQueueHandler _queue;

        /// <summary>
        /// Creates a new PaperTradingDataFeed for the algorithm/job
        /// </summary>
        /// <param name="algorithm">The algorithm to receive the data, used for a complete listing of active securities</param>
        /// <param name="dataSource">Queable Source of the data</param>
        /// <param name="job">The job being run</param>
        public PaperTradingDataFeed(IAlgorithm algorithm, IDataQueueHandler dataSource, LiveNodePacket job)
            : base(algorithm, dataSource)
        {
            _job = job;
            _queue = dataSource;

            // create a lookup keyed by SecurityType
            var symbols = new Dictionary<SecurityType, List<string>>();

            // Only subscribe equities and forex symbols
            foreach (var security in algorithm.Securities.Values)
            {
                if (security.Type == SecurityType.Equity || security.Type == SecurityType.Forex)
                {
                    if (!symbols.ContainsKey(security.Type)) symbols.Add(security.Type, new List<string>());
                    symbols[security.Type].Add(security.Symbol);
                }
            }

            // request for data from these symbols
            _queue.Subscribe(job, symbols);
        }

        /// <summary>
        /// Gets the next ticks from the live trading feed
        /// </summary>
        /// <returns>The next ticks to be processed</returns>
        public override IEnumerable<Tick> GetNextTicks()
        {
            return _queue.GetNextTicks();
        }
    }
}
