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
using System.IO;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// An instance of the <see cref="IDataProvider"/> that will attempt to retrieve files not present on the filesystem from the API
    /// </summary>
    public class ApiDataProvider : IDataProvider
    {
        private readonly int _uid = Config.GetInt("job-user-id", 0);
        private readonly string _token = Config.Get("api-access-token", "1");
        private readonly string _dataPath = Config.Get("data-folder", "../../../Data/");
        private static readonly int DownloadPeriod = Config.GetInt("api-data-update-period", 5);
        private readonly Api.Api _api;

        /// <summary>
        /// Initialize a new instance of the <see cref="ApiDataProvider"/>
        /// </summary>
        public ApiDataProvider()
        {
            _api = new Api.Api();

            _api.Initialize(_uid, _token, _dataPath);
        }

        /// <summary>
        /// Retrieves data to be used in an algorithm.
        /// If file does not exist, an attempt is made to download them from the api
        /// </summary>
        /// <param name="filePath">File path representing where the data requested</param>
        /// <returns>A <see cref="Stream"/> of the data requested</returns>
        public Stream Fetch(string filePath)
        {
            Symbol symbol;
            DateTime date;
            Resolution resolution;

            // Fetch the details of this data request
            if (LeanData.TryParsePath(filePath, out symbol, out date, out resolution))
            {
                if (!File.Exists(filePath) || IsOutOfDate(resolution, filePath))
                {
                    return DownloadData(filePath);
                }

                // Use the file already on the disk
                return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            Log.Error("ApiDataProvider.Fetch(): failed to parse file path key {0}", filePath);
            return null;
        }

        /// <summary>
        /// Determine if the file is out of date based on configuration and needs to be updated
        /// </summary>
        /// <param name="resolution">Data resolution</param>
        /// <param name="filepath">Path to the file</param>
        /// <returns>True if the file is out of date</returns>
        /// <remarks>Files are only "out of date" for Hourly/Daily data because this data is stored all in one file</remarks>
        public static bool IsOutOfDate(Resolution resolution, string filepath)
        {
            return resolution >= Resolution.Hour &&
                (DateTime.Now - TimeSpan.FromDays(DownloadPeriod)) > File.GetLastWriteTime(filepath);
        }

        /// <summary>
        /// Attempt to download data using the Api for and return a FileStream of that data.
        /// </summary>
        /// <param name="filePath">The path to store the file</param>
        /// <returns>A FileStream of the data</returns>
        private FileStream DownloadData(string filePath)
        {
            Log.Trace($"ApiDataProvider.Fetch(): Attempting to get data from QuantConnect.com's data library for {filePath}.");

            var downloadSuccessful = _api.DownloadData(filePath);

            if (downloadSuccessful)
            {
                Log.Trace($"ApiDataProvider.Fetch(): Successfully retrieved data for {filePath}.");

                return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            // Failed to download
            Log.Error("ApiDataProvider.Fetch(): Unable to remotely retrieve data for path {0}. " +
                "Please make sure you have the necessary data in your online QuantConnect data library.",
                filePath);
            return null;
        }
    }
}
