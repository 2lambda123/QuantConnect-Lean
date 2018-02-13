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
using System.IO;
using System.Collections.Concurrent;
using QuantConnect.Logging;
using System.Linq;
using Ionic.Zip;
using Ionic.Zlib;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// File provider implements optimized zip archives caching facility. Cache is thread safe.
    /// </summary>
    public class ZipDataCacheProvider : IDataCacheProvider
    {
        private const int MaxFilesToCache = 10;
        private const int CacheSeconds = 10;

        // ZipArchive cache used by the class.  Store the last time a file was accessed
        // so that we can expire ones that haven't been used in a while.
        private readonly ConcurrentDictionary<string, CachedZipFile> _zipFileCache = new ConcurrentDictionary<string, CachedZipFile>();

        private DateTime _lastCacheScan = DateTime.MinValue;
        private readonly IDataProvider _dataProvider;

        // Ionic.Zip.ZipFile instances are not thread-safe
        private readonly object _zipFileSynchronizer = new object();

        /// <summary>
        /// Constructor that sets the <see cref="IDataProvider"/> used to retrieve data
        /// </summary>
        public ZipDataCacheProvider(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Does not attempt to retrieve any data
        /// </summary>
        public Stream Fetch(string key)
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

                try
                {
                    CachedZipFile existingEntry;
                    if (!GetFromCache(filename, out existingEntry))
                    {
                        var dataStream = _dataProvider.Fetch(filename);

                        if (dataStream != null)
                        {
                            try
                            {
                                var newItem = new CachedZipFile(ZipFile.Read(dataStream), filename);

                                lock (_zipFileSynchronizer)
                                {
                                    stream = CreateStream(newItem.ZipFile, entryName, filename);
                                }

                                AddToCache(filename, newItem);
                            }
                            catch (Exception exception)
                            {
                                if (exception is ZipException || exception is ZlibException)
                                {
                                    Log.Error("ZipDataCacheProvider.Fetch(): Corrupt zip file/entry: " + filename + "#" + entryName + " Error: " + exception);
                                }
                                else throw;
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            lock (_zipFileSynchronizer)
                            {
                                stream = CreateStream(existingEntry.ZipFile, entryName, filename);
                            }
                        }
                        catch (Exception exception)
                        {
                            if (exception is ZipException || exception is ZlibException)
                            {
                                Log.Error("ZipDataCacheProvider.Fetch(): Corrupt zip file/entry: " + filename + "#" + entryName + " Error: " + exception);
                            }
                            else throw;
                        }
                    }

                    return stream;
                }
                catch (Exception err)
                {
                    Log.Error(err, "Inner try/catch");
                    if (stream != null) stream.Dispose();
                    return null;
                }
            }
            else
            {
                // handles text files
                return _dataProvider.Fetch(filename);
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
            lock (_zipFileSynchronizer)
            {
                foreach (var zip in _zipFileCache)
                {
                    zip.Value.ZipFile.Dispose();
                }
            }

            _zipFileCache.Clear();
        }

        /// <summary>
        /// Add a file from the cache, bumping the least frequently accessed
        /// file if necessary.
        /// </summary>
        private void AddToCache(string filename, CachedZipFile zipFile) {
            // If the cache is full, we clean the cache.
            if (_zipFileCache.Count == MaxFilesToCache) {
                CleanCache();
            }

            // If the cache is still full, we remove the oldest value to make room.
            if (_zipFileCache.Count == MaxFilesToCache) {
               // Determine the oldest key.
               var smallest = _zipFileCache.Min(e => e.Value.DateCached);
               var key = _zipFileCache.Where(e => e.Value.DateCached == smallest).First().Key;

               // Remove it.
               CachedZipFile removed;

               if (_zipFileCache.TryRemove(key, out removed)) {
                   // disposing zip archive
                   removed.Dispose();
               }
            }

            // Actually add the new value to the cache.
            _zipFileCache.TryAdd(filename, zipFile);
        }

        /// <summary>
        /// Fetch a file from the cache, updating the time last accessed.
        /// </summary>
        private bool GetFromCache(string filename, out CachedZipFile zipFile) {
            bool result = _zipFileCache.TryGetValue(filename, out zipFile);
            if (zipFile != null) {
                zipFile.Refresh();
            }
            return result;
        }

        /// <summary>
        /// Remove items in the cache that are older than the cutoff date
        /// </summary>
        private void CleanCache()
        {
            var clearCacheIfOlderThan = DateTime.Now.AddSeconds(-CacheSeconds);

            // clean all items that that are older than CacheSeconds than the current date
            foreach (var zip in _zipFileCache)
            {
                if (zip.Value.Uncache(clearCacheIfOlderThan))
                {
                    // removing it from the cache
                    CachedZipFile removed;
                    if (_zipFileCache.TryRemove(zip.Key, out removed))
                    {
                        // disposing zip archive
                        removed.Dispose();
                    }
                }
            }

            _lastCacheScan = DateTime.Now;
        }

        /// <summary>
        /// Create a stream of a specific ZipEntry
        /// </summary>
        /// <param name="zipFile">The zipFile containing the zipEntry</param>
        /// <param name="entryName">The name of the entry</param>
        /// <param name="fileName">The name of the zip file on disk</param>
        /// <returns>A <see cref="Stream"/> of the appropriate zip entry</returns>
        private Stream CreateStream(ZipFile zipFile, string entryName, string fileName)
        {
            var entry = zipFile.Entries.FirstOrDefault(x => entryName == null || string.Compare(x.FileName, entryName, StringComparison.OrdinalIgnoreCase) == 0);
            if (entry != null)
            {
                var stream = new MemoryStream();

                try {
                    stream.SetLength(entry.UncompressedSize);
                } catch (ArgumentOutOfRangeException err) {
                    // The needed size of the MemoryStream is longer than allowed. 
                    // just read the data directly from the file.
                    // Note that we cannot use entry.OpenReader() because only one OpenReader
                    // can be open at a time without causing corruption.

                    // We must use fileName instead of zipFile.Name,
                    // because zipFile is initialized from a stream and not a file.
                    var zipStream = new ZipInputStream(fileName);

                    var zipEntry = zipStream.GetNextEntry();

                    // The zip file was empty!
                    if (zipEntry == null) {
                        return null;
                    }

                    // Null entry name, return the first.
                    if (entryName == null) {
                        return zipStream;
                    }

                    // Non-default entry name, return matching one if it exists, otherwise null.
                    while (zipEntry != null) {
                        if (string.Compare(zipEntry.FileName, entryName, StringComparison.OrdinalIgnoreCase) == 0) {
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
    }


    /// <summary>
    /// Type for storing zipfile in cache
    /// </summary>
    public class CachedZipFile : IDisposable
    {
        private string _key;
        public DateTime DateCached { get; private set; }
        private ZipFile _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedZipFile"/> 
        /// </summary>
        /// <param name="data">ZipFile to be store</param>
        /// <param name="key">Key that represents the path to the data</param>
        public CachedZipFile(ZipFile data, string key)
        {
            _data = data;
            _key = key;
            DateCached = DateTime.Now;
        }

        /// <summary>
        /// Method used to update the last time this file was accessed.
        /// </summary>
        public void Refresh()
        {
            DateCached = DateTime.Now;
        }

        /// <summary>
        /// Method used to check if this object was created before a certain time
        /// </summary>
        /// <param name="date">DateTime which is compared to the DateTime this object was created</param>
        /// <returns>Bool indicating whether this object is older than the specified time</returns>
        public bool Uncache(DateTime date)
        {
            return DateCached < date;
        }

        /// <summary>
        /// The ZipFile this object represents
        /// </summary>
        public ZipFile ZipFile
        {
            get { return _data; }
        }

        /// <summary>
        /// Path to the ZipFile
        /// </summary>
        public string Key
        {
            get { return _key; }
        }

        /// <summary>
        /// Dispose of the ZipFile
        /// </summary>
        public void Dispose()
        {
            if (_data != null)
            {
                _data.Dispose();
            }

            _key = null;
        }
    }
}
