﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zip;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.ToolBox;
using QuantConnect.ToolBox.FxVolumeDownloader;

namespace QuantConnect.Tests.ToolBox.FxVolume
{
    [SetUpFixture]
    public class SetUpClass
    {
        [SetUp]
        public void SetUpTests()
        {
            Market.Add("FXCMForexVolume", identifier: 20);
        }
    }

    [TestFixture]
    public class ForexVolumeDownloaderTest
    {
        [SetUp]
        public void SetUpTemporatyFolder()
        {
            var randomFolder = "ForexVolumeTesting_" + Guid.NewGuid().ToString("N").Substring(startIndex: 0, length: 6);
            _dataDirectory = Path.Combine(Path.GetTempPath(), randomFolder);
            _downloader = new ForexVolumeDownloader(_dataDirectory);
        }

        private string _dataDirectory;
        private ForexVolumeDownloader _downloader;
        private readonly Symbol _symbol = Symbol.Create("EURUSD", SecurityType.Base, Market.Decode(code: 20));

        [Ignore("WIP")]
        [TestCase]
        public void DailyDataIsCorrectlyRetrieved()
        {
            var data = _downloader.Get(_symbol, Resolution.Daily, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 22));
            //SaveCsv(data, "DailyData.csv");
        }

        [Ignore("WIP")]
        [TestCase]
        public void HourlyDataIsCorrectlyRetrieved()
        {
            var data = _downloader.Get(_symbol, Resolution.Hour, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 10));
            //SaveCsv(data, "HourData.csv");
        }

        [Ignore("WIP")]
        [TestCase]
        public void MinuteDataIsCorrectlyRetrieved()
        {
            var data = _downloader.Get(_symbol, Resolution.Minute, new DateTime(year: 2012, month: 01, day: 01),
                new DateTime(year: 2012, month: 07, day: 01));
        }

        [TestCase]
        public void RetrievedDailyDataIsCorrectlySaved()
        {
            // Arrange
            var resolution = Resolution.Daily;
            var data = _downloader.Get(_symbol, resolution, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 22));

            // Act
            var writer = new LeanDataWriter(resolution, _symbol, _dataDirectory);
            writer.Write(data);

            // Assert
            var expectedData = data.Cast<ForexVolume>().ToArray();
            var outputFile = Path.Combine(_dataDirectory, "base/fxcmforexvolume/daily/eurusd.zip");

            var actualdata = ReadZipFileData(outputFile);
            var lines = actualdata.Count;
            for (var i = 0; i < lines - 1; i++)
            {
                Assert.AreEqual(expectedData[i].Value, int.Parse(actualdata[i][1]));
                Assert.AreEqual(expectedData[i].Transactions, int.Parse(actualdata[i][2]));
            }
        }

        [TestCase]
        public void RetrievedHourDataIsCorrectlySaved()
        {
            // Arrange
            var resolution = Resolution.Hour;
            var symbol = Symbol.Create("EURUSD", SecurityType.Base, Market.Decode(code: 20));
            var data = _downloader.Get(symbol, resolution, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 17));
            // Act
            var writer = new LeanDataWriter(resolution, symbol, _dataDirectory);
            writer.Write(data);

            // Assert
            var expectedData = data.Cast<ForexVolume>().ToArray();
            var outputFile = Path.Combine(_dataDirectory, "base/fxcmforexvolume/hour/eurusd.zip");

            var actualdata = ReadZipFileData(outputFile);
            var lines = actualdata.Count;
            for (var i = 0; i < lines - 1; i++)
            {
                Assert.AreEqual(expectedData[i].Value, int.Parse(actualdata[i][1]));
                Assert.AreEqual(expectedData[i].Transactions, int.Parse(actualdata[i][2]));
            }
        }

        [TestCase]
        public void RetrievedMinuteDataIsCorrectlySaved()
        {
            // Arrange
            var resolution = Resolution.Minute;
            var data = _downloader.Get(_symbol, resolution, new DateTime(year: 2012, month: 01, day: 01),
                new DateTime(year: 2012, month: 01, day: 07));
            // Act
            var writer = new LeanDataWriter(resolution, _symbol, _dataDirectory);
            writer.Write(data);

            // Assert
            var expectedData = data.Cast<ForexVolume>().ToArray();
            var outputFolder = Path.Combine(_dataDirectory, "base/fxcmforexvolume/minute");

            var actualdata = ReadZipFolderData(outputFolder);
            var lines = actualdata.Count;
            for (var i = 0; i < lines - 1; i++)
            {
                Assert.AreEqual(expectedData[i].Value, int.Parse(actualdata[i][1]));
                Assert.AreEqual(expectedData[i].Transactions, int.Parse(actualdata[i][2]));
            }
        }

        [TestCase]
        public void SavedDailyDataIsCorrectlyRead()
        {
            // Arrange
            var resolution = Resolution.Daily;
            var startDate = new DateTime(year: 2017, month: 04, day: 02);
            var data = _downloader.Get(_symbol, resolution, startDate, new DateTime(year: 2017, month: 04, day: 22));
            var writer = new LeanDataWriter(resolution, _symbol, _dataDirectory);
            writer.Write(data);

            var config = new SubscriptionDataConfig(typeof(ForexVolume), _symbol, resolution, DateTimeZone.Utc,
                DateTimeZone.Utc, fillForward: false, extendedHours: false, isInternalFeed: true, isCustom: true,
                tickType: TickType.Trade, isFilteredSubscription: false,
                dataNormalizationMode: DataNormalizationMode.Raw);

            // Act
            var reader = new LeanDataReader(config, _symbol, resolution, startDate, _dataDirectory);

            // Assert
            var expectedData = data.Cast<ForexVolume>().ToArray();
            var actualData = reader.Parse().Cast<ForexVolume>().ToArray();
            var lines = actualData.Length;
            for (var i = 0; i < lines - 1; i++)
            {
                Assert.AreEqual(expectedData[i].Value, actualData[i].Value);
                Assert.AreEqual(expectedData[i].Transactions, actualData[i].Transactions);
            }
        }

        [TestCase]
        public void SavedMinuteDataIsCorrectlyRead()
        {
            // Arrange
            var resolution = Resolution.Minute;
            var startDate = new DateTime(year: 2017, month: 04, day: 02);
            var data = _downloader.Get(_symbol, resolution, startDate, new DateTime(year: 2017, month: 04, day: 7));
            var writer = new LeanDataWriter(resolution, _symbol, _dataDirectory);
            writer.Write(data);

            var config = new SubscriptionDataConfig(typeof(ForexVolume), _symbol, resolution, DateTimeZone.Utc,
                DateTimeZone.Utc, fillForward: false, extendedHours: false, isInternalFeed: true, isCustom: true,
                tickType: TickType.Trade, isFilteredSubscription: false,
                dataNormalizationMode: DataNormalizationMode.Raw);

            // Act
            var reader = new LeanDataReader(config, _symbol, resolution, startDate, _dataDirectory);

            // Assert
            var expectedData = data.Cast<ForexVolume>().ToArray();
            var actualData = reader.Parse().Cast<ForexVolume>().ToArray();
            var lines = actualData.Length;
            for (var i = 0; i < lines - 1; i++)
            {
                Assert.AreEqual(expectedData[i].Value, actualData[i].Value);
                Assert.AreEqual(expectedData[i].Transactions, actualData[i].Transactions);
            }
        }

        [TestCase]
        public void RetrievedDailyDataIsCorrectlySavedUsingRunMethod()
        {
            // Arrange
            var resolution = Resolution.Daily;

            var startDate = new DateTime(year: 2014, month: 04, day: 01);
            var endDate = startDate.AddMonths(months: 1);
            var data = _downloader.Get(_symbol, resolution, startDate, endDate);

            // Act
            _downloader.Run(_symbol, resolution, startDate, endDate);

            // Assert
            var expectedData = data.Cast<ForexVolume>().ToArray();
            var outputFile = Path.Combine(_dataDirectory, "base/fxcmforexvolume/daily/eurusd.zip");

            var actualdata = ReadZipFileData(outputFile);
            var lines = actualdata.Count;
            for (var i = 0; i < lines - 1; i++)
            {
                Assert.AreEqual(expectedData[i].Value, long.Parse(actualdata[i][1]));
                Assert.AreEqual(expectedData[i].Transactions, int.Parse(actualdata[i][2]));
            }
        }

        //[Ignore("Long test")]
        [TestCase]
        public void RequestWithMoreThan10KMinuteObservationAreCorrectlySaved()
        {
            // Arrange
            var resolution = Resolution.Minute;
            var startDate = new DateTime(year: 2013, month: 04, day: 01);
            var endDate = startDate.AddMonths(months: 1);
            // Act
            _downloader.Run(_symbol, resolution, startDate, endDate);
            // Assert
            var outputFolder = Path.Combine(_dataDirectory, "base/fxcmforexvolume/minute");
            var files = Directory.GetFiles(outputFolder, "*.zip", SearchOption.AllDirectories);
            Assert.AreEqual(expected: 28, actual: files.Length);
        }

        //[Ignore("Long test")]
        [TestCase]
        public void RequestWithMoreThan10KHourlyObservationAreCorrectlySaved()
        {
            // Arrange
            var resolution = Resolution.Hour;
            var startDate = new DateTime(year: 2014, month: 01, day: 01);
            var endDate = startDate.AddYears(value: 2);
            // Act
            _downloader.Run(_symbol, resolution, startDate, endDate);
            // Assert
            var outputFile = Path.Combine(_dataDirectory, "base/fxcmforexvolume/hour/eurusd.zip");
            var observationsCount = ReadZipFileData(outputFile).Count;
            // 2 years x 52 weeks x 5 days x 24 hours = 12480 hours
            Assert.True(observationsCount >= 12480, string.Format("Actual observations: {0}", observationsCount));
        }

        private List<string[]> ReadZipFolderData(string outputFolder)
        {
            var actualdata = new List<string[]>();
            var files = Directory.GetFiles(outputFolder, "*.zip");
            foreach (var file in files)
            {
                actualdata.AddRange(ReadZipFileData(file));
            }
            return actualdata;
        }

        private static List<string[]> ReadZipFileData(string dataZipFile)
        {
            var actualdata = new List<string[]>();
            ZipFile zipFile;
            using (var unzipped = QuantConnect.Compression.Unzip(dataZipFile, out zipFile))
            {
                string line;
                while ((line = unzipped.ReadLine()) != null)
                {
                    actualdata.Add(line.Split(','));
                }
            }
            return actualdata;
        }

        private void SaveCsv(IEnumerable<IBaseData> data, string fileName)
        {
            var sb = new StringBuilder("DateTime,Volume,Transactions\n");

            foreach (var obs in data)
            {
                sb.AppendLine(string.Format("{0:yyyy/MM/dd HH:mm},{1},{2}", obs.Time, obs.Value,
                    ((ForexVolume) obs).Transactions));
            }
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                fileName);
            File.WriteAllText(filePath, sb.ToString());
        }
    }
}