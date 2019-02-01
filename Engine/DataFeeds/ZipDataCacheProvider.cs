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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zip;
using Ionic.Zlib;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// File provider implements optimized zip archives caching facility. Cache is thread safe.
    /// </summary>
    public class ZipDataCacheProvider : IDataCacheProvider
    {
        private const int CacheSeconds = 10;

        // ZipArchive cache used by the class
        private readonly ConcurrentDictionary<string, CachedZipFile> _zipFileCache = new ConcurrentDictionary<string, CachedZipFile>();
        private DateTime _lastCacheScan = DateTime.MinValue;
        private readonly IDataProvider _dataProvider;

        /// <summary>
        /// Constructor that sets the <see cref="IDataProvider"/> used to retrieve data
        /// </summary>
        public ZipDataCacheProvider(IDataProvider dataProvider)
        {
            this._dataProvider = dataProvider;
        }

        /// <summary>
        /// Does not attempt to retrieve any data
        /// </summary>
        public Stream FetchStream(string key)
        {
            string entryName = null; // default to all entries
            var filename = key;
            var hashIndex = key.LastIndexOf("#", StringComparison.Ordinal);
            if (hashIndex != -1)
            {
                entryName = key.Substring(hashIndex + 1);
                filename = key.Substring(0, hashIndex);
            }

            // handles zip files
            if (filename.GetExtension() == ".zip")
            {
                Stream stream = null;

                // scan the cache once every 3 seconds
                if (this._lastCacheScan == DateTime.MinValue || this._lastCacheScan < DateTime.Now.AddSeconds(-3))
                {
                    this.CleanCache();
                }

                try
                {
                    CachedZipFile existingEntry;
                    if (!this._zipFileCache.TryGetValue(filename, out existingEntry))
                    {
                        var dataStream = this._dataProvider.Fetch(filename);

                        if (dataStream != null)
                        {
                            try
                            {
                                var newItem = new CachedZipFile(dataStream, filename);
                                stream = this.CreateStream(newItem, entryName, filename);

                                if (!this._zipFileCache.TryAdd(filename, newItem))
                                {
                                    newItem.Dispose();
                                }
                            }
                            catch (Exception exception)
                            {
                                if (exception is ZipException || exception is ZlibException)
                                {
                                    Log.Error("ZipDataCacheProvider.Fetch(): Corrupt zip file/entry: " + filename + "#" + entryName + " Error: " + exception);
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            lock (existingEntry)
                            {
                                stream = this.CreateStream(existingEntry, entryName, filename);
                            }
                        }
                        catch (Exception exception)
                        {
                            if (exception is ZipException || exception is ZlibException)
                            {
                                Log.Error("ZipDataCacheProvider.Fetch(): Corrupt zip file/entry: " + filename + "#" + entryName + " Error: " + exception);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }

                    return stream;
                }
                catch (Exception err)
                {
                    Log.Error(err, "Inner try/catch");
                    stream?.DisposeSafely();
                    return null;
                }
            }
            else
            {
                // handles text files
                return this._dataProvider.Fetch(filename);
            }
        }

        /// <summary>
        /// Store the data in the cache. Not implemented in this instance of the IDataCacheProvider
        /// </summary>
        /// <param name="key">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data as a byte array</param>
        public void Store(string key, byte[] data)
        {
            //
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            CachedZipFile zip;
            foreach (var zipFile in this._zipFileCache)
            {
                if (this._zipFileCache.TryRemove(zipFile.Key, out zip))
                {
                    zip.Dispose();
                }
            }
        }

        /// <summary>
        /// Remove items in the cache that are older than the cutoff date
        /// </summary>
        private void CleanCache()
        {
            var clearCacheIfOlderThan = DateTime.Now.AddSeconds(-CacheSeconds);

            // clean all items that that are older than CacheSeconds than the current date
            foreach (var zip in this._zipFileCache)
            {
                if (zip.Value.Uncache(clearCacheIfOlderThan))
                {
                    // removing it from the cache
                    CachedZipFile removed;
                    if (this._zipFileCache.TryRemove(zip.Key, out removed))
                    {
                        // disposing zip archive
                        removed.Dispose();
                    }
                }
            }

            this._lastCacheScan = DateTime.Now;
        }

        /// <summary>
        /// Create a stream of a specific ZipEntry
        /// </summary>
        /// <param name="zipFile">The zipFile containing the zipEntry</param>
        /// <param name="entryName">The name of the entry</param>
        /// <param name="fileName">The name of the zip file on disk</param>
        /// <returns>A <see cref="Stream"/> of the appropriate zip entry</returns>
        private Stream CreateStream(CachedZipFile zipFile, string entryName, string fileName)
        {
            ZipEntry entry;
            if (entryName == null)
            {
                entry = zipFile.EntryCache.FirstOrDefault().Value;
            }
            else
            {
                zipFile.EntryCache.TryGetValue(entryName, out entry);
            }

            if (entry != null)
            {
                var stream = new MemoryStream();

                try
                {
                    stream.SetLength(entry.UncompressedSize);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // The needed size of the MemoryStream is longer than allowed.
                    // just read the data directly from the file.
                    // Note that we cannot use entry.OpenReader() because only one OpenReader
                    // can be open at a time without causing corruption.

                    // We must use fileName instead of zipFile.Name,
                    // because zipFile is initialized from a stream and not a file.
                    var zipStream = new ZipInputStream(fileName);

                    var zipEntry = zipStream.GetNextEntry();

                    // The zip file was empty!
                    if (zipEntry == null)
                    {
                        return null;
                    }

                    // Null entry name, return the first.
                    if (entryName == null)
                    {
                        return zipStream;
                    }

                    // Non-default entry name, return matching one if it exists, otherwise null.
                    while (zipEntry != null)
                    {
                        if (string.Compare(zipEntry.FileName, entryName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return zipStream;
                        }

                        zipEntry = zipStream.GetNextEntry();
                    }
                }

                entry.OpenReader().CopyTo(stream);
                stream.Position = 0;
                return stream;
            }

            return null;
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


    /// <summary>
    /// Type for storing zipfile in cache
    /// </summary>
    public class CachedZipFile : IDisposable
    {
        private readonly DateTime _dateCached;
        private readonly Stream _dataStream;

        /// <summary>
        /// The ZipFile this object represents
        /// </summary>
        private readonly ZipFile _zipFile;

        /// <summary>
        /// Contains all entries of the zip file by filename
        /// </summary>
        public readonly Dictionary<string, ZipEntry> EntryCache = new Dictionary<string, ZipEntry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Path to the ZipFile
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedZipFile"/>
        /// </summary>
        /// <param name="dataStream">Stream containing the zip file</param>
        /// <param name="key">Key that represents the path to the data</param>
        public CachedZipFile(Stream dataStream, string key)
        {
            this._dataStream = dataStream;
            this._zipFile = ZipFile.Read(dataStream);
            foreach (var entry in this._zipFile.Entries)
            {
                this.EntryCache[entry.FileName] = entry;
            }
            this.Key = key;
            this._dateCached = DateTime.Now;
        }

        /// <summary>
        /// Method used to check if this object was created before a certain time
        /// </summary>
        /// <param name="date">DateTime which is compared to the DateTime this object was created</param>
        /// <returns>Bool indicating whether this object is older than the specified time</returns>
        public bool Uncache(DateTime date)
        {
            return this._dateCached < date;
        }

        /// <summary>
        /// Dispose of the ZipFile
        /// </summary>
        public void Dispose()
        {
            this.EntryCache.Clear();
            this._zipFile?.DisposeSafely();
            this._dataStream?.DisposeSafely();

            this.Key = null;
        }
    }
}
