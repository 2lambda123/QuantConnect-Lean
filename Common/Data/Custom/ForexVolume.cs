﻿using QuantConnect.Util;
using System;
using System.Globalization;

namespace QuantConnect.Data.Custom
{
    /// <summary>
    /// FXCM Real FOREX Volume and Transaction data from its clients base, available for the following pairs:
    ///     - EURUSD, USDJPY, GBPUSD, USDCHF, EURCHF, AUDUSD, USDCAD,
    ///       NZDUSD, EURGBP, EURJPY, GBPJPY, EURAUD, EURCAD, AUDJPY
    /// FXCM only provides support for FX symbols which produced over 110 million average daily volume (ADV) during 2013.
    /// This limit is imposed to ensure we do not highlight low volume/low ticket symbols in addition to other financial reporting concerns.
    ///
    /// </summary>
    /// <seealso cref="QuantConnect.Data.BaseData" />
    public class ForexVolume : BaseData
    {
        /// <summary>
        ///     Sum of opening and closing Transactions for the entire time interval.
        /// </summary>
        /// <value>
        ///     The transactions.
        /// </value>
        public int Transactions { get; set; }

        /// <summary>
        ///     Sum of opening and closing Volume for the entire time interval.
        ///     The volume measured in the QUOTE CURRENCY.
        /// </summary>
        /// <remarks>Please remember to convert this data to a common currency before making comparison between different pairs.</remarks>
        public long Value { get; set; }

        /// <summary>
        ///     Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        ///     String URL of source file.
        /// </returns>
        /// <exception cref="System.NotImplementedException">FOREX Volume data is not available in live mode, yet.</exception>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode) throw new NotImplementedException("FOREX Volume data is not available in live mode, yet.");
            var source = LeanData.GenerateZipFilePath(Globals.DataFolder, config.Symbol, SecurityType.Base,
                "FXCMForexVolume",
                date, config.Resolution);
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile);
        }

        /// <summary>
        ///     Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method,
        ///     and returns a new instance of the object
        ///     each time it is called. The returned object is assumed to be time stamped in the config.ExchangeTimeZone.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="line">Line of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        ///     Instance of the T:BaseData object generated by this line of the CSV
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            DateTime time;
            var obs = line.Split(',');
            if (config.Resolution == Resolution.Minute)
            {
                time = date.Date.AddMilliseconds(int.Parse(obs[0]));
            }
            else
            {
                time = DateTime.ParseExact(obs[0], "yyyyMMdd HH:mm", CultureInfo.InvariantCulture);
            }
            return new ForexVolume
            {
                DataType = MarketDataType.Base,
                Symbol = config.Symbol,
                Time = time,
                Value = long.Parse(obs[1]),
                Transactions = int.Parse(obs[2])
            };
        }
    }
}