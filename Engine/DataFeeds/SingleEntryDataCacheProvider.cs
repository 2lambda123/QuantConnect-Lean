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
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using Ionic.Zip;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Default implementation of the <see cref="IDataCacheProvider"/>
    /// Does not cache data.  If the data is a zip, the first entry is returned
    /// </summary>
    public class SingleEntryDataCacheProvider : IDataCacheProvider
    {
        private readonly IDataProvider _dataProvider;
        private ZipFile _zipFile;
        private Stream _zipFileStream;

        /// <summary>
        /// Constructor that takes the <see cref="IDataProvider"/> to be used to retrieve data
        /// </summary>
        public SingleEntryDataCacheProvider(IDataProvider dataProvider)
        {
            this._dataProvider = dataProvider;
        }

        /// <summary>
        /// Fetch data from the cache
        /// </summary>
        /// <param name="key">A string representing the key of the cached data</param>
        /// <returns>An <see cref="Stream"/> of the cached data</returns>
        public Stream FetchStream(string key)
        {
            var stream = this._dataProvider.Fetch(key);

            if (key.EndsWith(".zip") && stream != null)
            {
                // get the first entry from the zip file
                try
                {
                    var entryStream = Compression.UnzipStream(stream, out this._zipFile);

                    // save the file stream so it can be disposed later
                    this._zipFileStream = stream;

                    return entryStream;
                }
                catch (ZipException exception)
                {
                    Log.Error("SingleEntryDataCacheProvider.Fetch(): Corrupt file: " + key + " Error: " + exception);
                    stream.DisposeSafely();
                    return null;
                }
            }

            return stream;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="key">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data to cache as a byte array</param>
        public void Store(string key, byte[] data)
        {
            //
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this._zipFile?.DisposeSafely();
            this._zipFileStream?.DisposeSafely();
        }

        /// <summary>
        /// Fetch data from the cache as enumerator
        /// </summary>
        /// <param name="key">A string representing the key of the cached data</param>
        /// <param name="config">The subscription config</param>
        /// <param name="startDate">Provide the start date of data to be fetched. Inclusive.</param>
        /// <param name="endDate">Provide the end date of data to be fetched. Inclusive.</param>
        /// <returns>An enumerator of the cached data</returns>
        public IEnumerator<string> FetchEnumerator(string key, SubscriptionDataConfig config, DateTime? startDate = null, DateTime? endDate = null)
        {
            throw new NotImplementedException();
        }
    }
}
