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
*/

using NUnit.Framework;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class FileExtensionTests
    {
        [TestCaseSource(nameof(ToNormalizedPathReturnsNormalizedPathTestCases))]
        [Platform("Win", Reason = "The paths in these testcases are only forbidden in Windows OS")]
        public void ToNormalizedPathReturnsNormalizedPath(string inputName, string expectedName)
        {
            Assert.AreEqual(expectedName, FileExtension.ToNormalizedPath(inputName));
        }

        [TestCaseSource(nameof(ToNormalizedPathReturnsTheSamePathTestCases))]
        [Platform("Win", Reason = "The paths in these testcases are only forbidden in Windows OS")]
        public void ToNormalizedPathReturnsTheSamePath(string inputName)
        {
            Assert.AreEqual(inputName, FileExtension.ToNormalizedPath(inputName));
        }

        [TestCaseSource(nameof(FromValidReturnsOriginalPathTestCases))]
        [Platform("Win", Reason = "The paths in these testcases are only forbidden in Windows OS")]
        public void FromValidReturnsOriginalName(string inputName, string expectedName)
        {
            Assert.AreEqual(expectedName, FileExtension.FromNormalizedPath(inputName));
        }

        [TestCaseSource(
            nameof(
                ToNormalizedPathAndFromNormalizedPathReturnTheSameNameWhenOSIsNotWindowsTestCases
            )
        )]
        [Platform(Exclude = "Win")]
        public void ToNormalizedPathAndFromNormalizedPathReturnsTheSameNameWhenOSIsNotWindows(
            string inputName
        )
        {
            Assert.AreEqual(inputName, FileExtension.ToNormalizedPath(inputName));
            Assert.AreEqual(inputName, FileExtension.FromNormalizedPath(inputName));
        }

        public static object[] ToNormalizedPathReturnsNormalizedPathTestCases =
        {
            new object[]
            {
                "data\\equity\\usa\\map_files\\AUX",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "AUX"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\NUL",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "NUL"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\PRN",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "PRN"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\CON",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "CON"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM0",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM0"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM1",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM1"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM2",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM2"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM3",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM3"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM4",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM4"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM5",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM5"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM6",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM6"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM7",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM7"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM8",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM8"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM9",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM9"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT0",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT0"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT1",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT1"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT2",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT2"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT3",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT3"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT4",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT4"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT5",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT5"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT6",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT6"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT7",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT7"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT8",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT8"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT9",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT9"
            },
            new object[]
            {
                "data/equity/usa/map_files/AUX",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "AUX"
            },
            new object[]
            {
                "data/equity/usa/map_files/NUL",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "NUL"
            },
            new object[]
            {
                "data/equity/usa/map_files/PRN",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "PRN"
            },
            new object[]
            {
                "data/equity/usa/map_files/CON",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "CON"
            },
            new object[]
            {
                "data/equity/usa/map_files/COM0",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM0"
            },
            new object[]
            {
                "data/equity/usa/map_files/COM1",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM1"
            },
            new object[]
            {
                "data/equity/usa/map_files/COM2",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM2"
            },
            new object[]
            {
                "data/equity/usa/map_files/COM3",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM3"
            },
            new object[]
            {
                "data/equity/usa/map_files/COM4",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM4"
            },
            new object[]
            {
                "data/equity/usa/map_files/COM5",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM5"
            },
            new object[]
            {
                "data/equity/usa/map_files/COM6",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM6"
            },
            new object[]
            {
                "data/equity/usa/map_files/COM7",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM7"
            },
            new object[]
            {
                "data/equity/usa/map_files/COM8",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM8"
            },
            new object[]
            {
                "data/equity/usa/map_files/COM9",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM9"
            },
            new object[]
            {
                "data/equity/usa/map_files/LPT0",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT0"
            },
            new object[]
            {
                "data/equity/usa/map_files/LPT1",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT1"
            },
            new object[]
            {
                "data/equity/usa/map_files/LPT2",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT2"
            },
            new object[]
            {
                "data/equity/usa/map_files/LPT3",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT3"
            },
            new object[]
            {
                "data/equity/usa/map_files/LPT4",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT4"
            },
            new object[]
            {
                "data/equity/usa/map_files/LPT5",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT5"
            },
            new object[]
            {
                "data/equity/usa/map_files/LPT6",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT6"
            },
            new object[]
            {
                "data/equity/usa/map_files/LPT7",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT7"
            },
            new object[]
            {
                "data/equity/usa/map_files/LPT8",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT8"
            },
            new object[]
            {
                "data/equity/usa/map_files/LPT9",
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT9"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM0.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM0.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM1.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM1.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM2.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM2.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM3.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM3.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM4.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM4.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM5.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM5.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM6.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM6.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM7.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM7.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM8.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM8.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\COM9.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM9.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT0.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT0.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT1.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT1.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT2.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT2.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT3.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT3.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT4.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT4.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT5.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT5.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT6.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT6.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT7.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT7.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT8.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT8.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\LPT9.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT9.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\AUX.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "AUX.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\NUL.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "NUL.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\PRN.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "PRN.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\CON.csv",
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "CON.csv"
            },
            new object[] { "AUX.csv", "" + FileExtension.ReservedWordsPrefix + "AUX.csv" },
            new object[] { "NUL.csv", "" + FileExtension.ReservedWordsPrefix + "NUL.csv" },
            new object[] { "PRN.csv", "" + FileExtension.ReservedWordsPrefix + "PRN.csv" },
            new object[] { "CON.csv", "" + FileExtension.ReservedWordsPrefix + "CON.csv" },
            new object[] { "COM0.csv", "" + FileExtension.ReservedWordsPrefix + "COM0.csv" },
            new object[] { "COM1.csv", "" + FileExtension.ReservedWordsPrefix + "COM1.csv" },
            new object[] { "COM2.csv", "" + FileExtension.ReservedWordsPrefix + "COM2.csv" },
            new object[] { "COM3.csv", "" + FileExtension.ReservedWordsPrefix + "COM3.csv" },
            new object[] { "COM4.csv", "" + FileExtension.ReservedWordsPrefix + "COM4.csv" },
            new object[] { "COM5.csv", "" + FileExtension.ReservedWordsPrefix + "COM5.csv" },
            new object[] { "COM6.csv", "" + FileExtension.ReservedWordsPrefix + "COM6.csv" },
            new object[] { "COM7.csv", "" + FileExtension.ReservedWordsPrefix + "COM7.csv" },
            new object[] { "COM8.csv", "" + FileExtension.ReservedWordsPrefix + "COM8.csv" },
            new object[] { "COM9.csv", "" + FileExtension.ReservedWordsPrefix + "COM9.csv" },
            new object[] { "LPT0.csv", "" + FileExtension.ReservedWordsPrefix + "LPT0.csv" },
            new object[] { "LPT1.csv", "" + FileExtension.ReservedWordsPrefix + "LPT1.csv" },
            new object[] { "LPT2.csv", "" + FileExtension.ReservedWordsPrefix + "LPT2.csv" },
            new object[] { "LPT3.csv", "" + FileExtension.ReservedWordsPrefix + "LPT3.csv" },
            new object[] { "LPT4.csv", "" + FileExtension.ReservedWordsPrefix + "LPT4.csv" },
            new object[] { "LPT5.csv", "" + FileExtension.ReservedWordsPrefix + "LPT5.csv" },
            new object[] { "LPT6.csv", "" + FileExtension.ReservedWordsPrefix + "LPT6.csv" },
            new object[] { "LPT7.csv", "" + FileExtension.ReservedWordsPrefix + "LPT7.csv" },
            new object[] { "LPT8.csv", "" + FileExtension.ReservedWordsPrefix + "LPT8.csv" },
            new object[] { "LPT9.csv", "" + FileExtension.ReservedWordsPrefix + "LPT9.csv" },
            new object[] { "AUX.tar.gz", "" + FileExtension.ReservedWordsPrefix + "AUX.tar.gz" },
            new object[] { "NUL.tar.gz", "" + FileExtension.ReservedWordsPrefix + "NUL.tar.gz" },
            new object[] { "PRN.tar.gz", "" + FileExtension.ReservedWordsPrefix + "PRN.tar.gz" },
            new object[] { "CON.tar.gz", "" + FileExtension.ReservedWordsPrefix + "CON.tar.gz" },
            new object[]
            {
                "equity\\usa\\minute\\con\\20150903_trade.zip",
                "equity\\usa\\minute\\"
                    + FileExtension.ReservedWordsPrefix
                    + "con\\20150903_trade.zip"
            },
            new object[]
            {
                "equity\\usa\\minute\\nul\\20150903_trade.zip",
                "equity\\usa\\minute\\"
                    + FileExtension.ReservedWordsPrefix
                    + "nul\\20150903_trade.zip"
            },
            new object[]
            {
                "equity\\usa\\minute\\prn\\20150903_trade.zip",
                "equity\\usa\\minute\\"
                    + FileExtension.ReservedWordsPrefix
                    + "prn\\20150903_trade.zip"
            },
            new object[]
            {
                "equity\\usa\\minute\\aux\\20150903_trade.zip",
                "equity\\usa\\minute\\"
                    + FileExtension.ReservedWordsPrefix
                    + "aux\\20150903_trade.zip"
            },
            new object[]
            {
                "equity\\usa\\minute\\con\\con.zip",
                "equity\\usa\\minute\\"
                    + FileExtension.ReservedWordsPrefix
                    + "con\\"
                    + FileExtension.ReservedWordsPrefix
                    + "con.zip"
            },
            new object[]
            {
                "equity\\usa\\minute\\nul\\nul.zip",
                "equity\\usa\\minute\\"
                    + FileExtension.ReservedWordsPrefix
                    + "nul\\"
                    + FileExtension.ReservedWordsPrefix
                    + "nul.zip"
            },
            new object[]
            {
                "equity\\usa\\minute\\prn\\prn.zip",
                "equity\\usa\\minute\\"
                    + FileExtension.ReservedWordsPrefix
                    + "prn\\"
                    + FileExtension.ReservedWordsPrefix
                    + "prn.zip"
            },
            new object[]
            {
                "equity\\usa\\minute\\aux\\aux.zip",
                "equity\\usa\\minute\\"
                    + FileExtension.ReservedWordsPrefix
                    + "aux\\"
                    + FileExtension.ReservedWordsPrefix
                    + "aux.zip"
            }
        };

        public static object[] ToNormalizedPathReturnsTheSamePathTestCases =
        {
            new object[] { "data\\equity\\usa\\map_files\\AAUX.csv" },
            new object[] { "data\\equity\\usa\\map_files\\ANUL.csv" },
            new object[] { "data\\equity\\usa\\map_files\\APRN.csv" },
            new object[] { "data\\equity\\usa\\map_files\\ACON.csv" },
            new object[] { "data\\equity\\usa\\map_files\\AUXA.csv" },
            new object[] { "data\\equity\\usa\\map_files\\NULA.csv" },
            new object[] { "data\\equity\\usa\\map_files\\PRNA.csv" },
            new object[] { "data\\equity\\usa\\map_files\\CONA.csv" },
            new object[] { "AAUX.csv" },
            new object[] { "ANUL.csv" },
            new object[] { "APRN.csv" },
            new object[] { "ACON.csv" },
            new object[] { "AUXA.csv" },
            new object[] { "NULA.csv" },
            new object[] { "PRNA.csv" },
            new object[] { "CONA.csv" },
            new object[] { "data/equity/usa/map_files/AAUX.csv" },
            new object[] { "data/equity/usa/map_files/ANUL.csv" },
            new object[] { "data/equity/usa/map_files/APRN.csv" },
            new object[] { "data/equity/usa/map_files/ACON.csv" },
            new object[] { "data/equity/usa/map_files/AUXA.csv" },
            new object[] { "data/equity/usa/map_files/NULA.csv" },
            new object[] { "data/equity/usa/map_files/PRNA.csv" },
            new object[] { "data/equity/usa/map_files/CONA.csv" }
        };

        public static object[] FromValidReturnsOriginalPathTestCases =
        {
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM0.csv",
                "data\\equity\\usa\\map_files\\COM0.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM1.csv",
                "data\\equity\\usa\\map_files\\COM1.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM2.csv",
                "data\\equity\\usa\\map_files\\COM2.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM3.csv",
                "data\\equity\\usa\\map_files\\COM3.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM4.csv",
                "data\\equity\\usa\\map_files\\COM4.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM5.csv",
                "data\\equity\\usa\\map_files\\COM5.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM6.csv",
                "data\\equity\\usa\\map_files\\COM6.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM7.csv",
                "data\\equity\\usa\\map_files\\COM7.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM8.csv",
                "data\\equity\\usa\\map_files\\COM8.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "COM9.csv",
                "data\\equity\\usa\\map_files\\COM9.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT0.csv",
                "data\\equity\\usa\\map_files\\LPT0.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT1.csv",
                "data\\equity\\usa\\map_files\\LPT1.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT2.csv",
                "data\\equity\\usa\\map_files\\LPT2.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT3.csv",
                "data\\equity\\usa\\map_files\\LPT3.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT4.csv",
                "data\\equity\\usa\\map_files\\LPT4.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT5.csv",
                "data\\equity\\usa\\map_files\\LPT5.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT6.csv",
                "data\\equity\\usa\\map_files\\LPT6.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT7.csv",
                "data\\equity\\usa\\map_files\\LPT7.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT8.csv",
                "data\\equity\\usa\\map_files\\LPT8.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "LPT9.csv",
                "data\\equity\\usa\\map_files\\LPT9.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "AUX.csv",
                "data\\equity\\usa\\map_files\\AUX.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "NUL.csv",
                "data\\equity\\usa\\map_files\\NUL.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "PRN.csv",
                "data\\equity\\usa\\map_files\\PRN.csv"
            },
            new object[]
            {
                "data\\equity\\usa\\map_files\\" + FileExtension.ReservedWordsPrefix + "CON.csv",
                "data\\equity\\usa\\map_files\\CON.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM0.csv",
                "data/equity/usa/map_files/COM0.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM1.csv",
                "data/equity/usa/map_files/COM1.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM2.csv",
                "data/equity/usa/map_files/COM2.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM3.csv",
                "data/equity/usa/map_files/COM3.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM4.csv",
                "data/equity/usa/map_files/COM4.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM5.csv",
                "data/equity/usa/map_files/COM5.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM6.csv",
                "data/equity/usa/map_files/COM6.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM7.csv",
                "data/equity/usa/map_files/COM7.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM8.csv",
                "data/equity/usa/map_files/COM8.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "COM9.csv",
                "data/equity/usa/map_files/COM9.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT0.csv",
                "data/equity/usa/map_files/LPT0.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT1.csv",
                "data/equity/usa/map_files/LPT1.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT2.csv",
                "data/equity/usa/map_files/LPT2.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT3.csv",
                "data/equity/usa/map_files/LPT3.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT4.csv",
                "data/equity/usa/map_files/LPT4.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT5.csv",
                "data/equity/usa/map_files/LPT5.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT6.csv",
                "data/equity/usa/map_files/LPT6.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT7.csv",
                "data/equity/usa/map_files/LPT7.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT8.csv",
                "data/equity/usa/map_files/LPT8.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "LPT9.csv",
                "data/equity/usa/map_files/LPT9.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "AUX.csv",
                "data/equity/usa/map_files/AUX.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "NUL.csv",
                "data/equity/usa/map_files/NUL.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "PRN.csv",
                "data/equity/usa/map_files/PRN.csv"
            },
            new object[]
            {
                "data/equity/usa/map_files/" + FileExtension.ReservedWordsPrefix + "CON.csv",
                "data/equity/usa/map_files/CON.csv"
            },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "AUX.csv", "AUX.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "NUL.csv", "NUL.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "PRN.csv", "PRN.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "CON.csv", "CON.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "COM0.csv", "COM0.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "COM1.csv", "COM1.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "COM2.csv", "COM2.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "COM3.csv", "COM3.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "COM4.csv", "COM4.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "COM5.csv", "COM5.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "COM6.csv", "COM6.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "COM7.csv", "COM7.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "COM8.csv", "COM8.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "COM9.csv", "COM9.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "LPT0.csv", "LPT0.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "LPT1.csv", "LPT1.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "LPT2.csv", "LPT2.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "LPT3.csv", "LPT3.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "LPT4.csv", "LPT4.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "LPT5.csv", "LPT5.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "LPT6.csv", "LPT6.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "LPT7.csv", "LPT7.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "LPT8.csv", "LPT8.csv" },
            new object[] { "" + FileExtension.ReservedWordsPrefix + "LPT9.csv", "LPT9.csv" },
        };

        public static object[] ToNormalizedPathAndFromNormalizedPathReturnTheSameNameWhenOSIsNotWindowsTestCases =
        {
            new object[] { "data\\equity\\usa\\map_files\\AUX" },
            new object[] { "data\\equity\\usa\\map_files\\NUL" },
            new object[] { "data\\equity\\usa\\map_files\\PRN" },
            new object[] { "data\\equity\\usa\\map_files\\CON" },
            new object[] { "data\\equity\\usa\\map_files\\COM0" },
            new object[] { "data\\equity\\usa\\map_files\\COM1" },
            new object[] { "data\\equity\\usa\\map_files\\COM2" },
            new object[] { "data\\equity\\usa\\map_files\\COM3" },
            new object[] { "data\\equity\\usa\\map_files\\COM4" },
            new object[] { "data\\equity\\usa\\map_files\\COM5" },
            new object[] { "data\\equity\\usa\\map_files\\COM6" },
            new object[] { "data\\equity\\usa\\map_files\\COM7" },
            new object[] { "data\\equity\\usa\\map_files\\COM8" },
            new object[] { "data\\equity\\usa\\map_files\\COM9" },
            new object[] { "data\\equity\\usa\\map_files\\LPT0" },
            new object[] { "data\\equity\\usa\\map_files\\LPT1" },
            new object[] { "data\\equity\\usa\\map_files\\LPT2" },
            new object[] { "data\\equity\\usa\\map_files\\LPT3" },
            new object[] { "data\\equity\\usa\\map_files\\LPT4" },
            new object[] { "data\\equity\\usa\\map_files\\LPT5" },
            new object[] { "data\\equity\\usa\\map_files\\LPT6" },
            new object[] { "data\\equity\\usa\\map_files\\LPT7" },
            new object[] { "data\\equity\\usa\\map_files\\LPT8" },
            new object[] { "data\\equity\\usa\\map_files\\LPT9" },
            new object[] { "data/equity/usa/map_files/AUX" },
            new object[] { "data/equity/usa/map_files/NUL" },
            new object[] { "data/equity/usa/map_files/PRN" },
            new object[] { "data/equity/usa/map_files/CON" },
            new object[] { "data/equity/usa/map_files/COM0" },
            new object[] { "data/equity/usa/map_files/COM1" },
            new object[] { "data/equity/usa/map_files/COM2" },
            new object[] { "data/equity/usa/map_files/COM3" },
            new object[] { "data/equity/usa/map_files/COM4" },
            new object[] { "data/equity/usa/map_files/COM5" },
            new object[] { "data/equity/usa/map_files/COM6" },
            new object[] { "data/equity/usa/map_files/COM7" },
            new object[] { "data/equity/usa/map_files/COM8" },
            new object[] { "data/equity/usa/map_files/COM9" },
            new object[] { "data/equity/usa/map_files/LPT0" },
            new object[] { "data/equity/usa/map_files/LPT1" },
            new object[] { "data/equity/usa/map_files/LPT2" },
            new object[] { "data/equity/usa/map_files/LPT3" },
            new object[] { "data/equity/usa/map_files/LPT4" },
            new object[] { "data/equity/usa/map_files/LPT5" },
            new object[] { "data/equity/usa/map_files/LPT6" },
            new object[] { "data/equity/usa/map_files/LPT7" },
            new object[] { "data/equity/usa/map_files/LPT8" },
            new object[] { "data/equity/usa/map_files/LPT9" },
            new object[] { "data\\equity\\usa\\map_files\\COM0.csv" },
            new object[] { "data\\equity\\usa\\map_files\\COM1.csv" },
            new object[] { "data\\equity\\usa\\map_files\\COM2.csv" },
            new object[] { "data\\equity\\usa\\map_files\\COM3.csv" },
            new object[] { "data\\equity\\usa\\map_files\\COM4.csv" },
            new object[] { "data\\equity\\usa\\map_files\\COM5.csv" },
            new object[] { "data\\equity\\usa\\map_files\\COM6.csv" },
            new object[] { "data\\equity\\usa\\map_files\\COM7.csv" },
            new object[] { "data\\equity\\usa\\map_files\\COM8.csv" },
            new object[] { "data\\equity\\usa\\map_files\\COM9.csv" },
            new object[] { "data\\equity\\usa\\map_files\\LPT0.csv" },
            new object[] { "data\\equity\\usa\\map_files\\LPT1.csv" },
            new object[] { "data\\equity\\usa\\map_files\\LPT2.csv" },
            new object[] { "data\\equity\\usa\\map_files\\LPT3.csv" },
            new object[] { "data\\equity\\usa\\map_files\\LPT4.csv" },
            new object[] { "data\\equity\\usa\\map_files\\LPT5.csv" },
            new object[] { "data\\equity\\usa\\map_files\\LPT6.csv" },
            new object[] { "data\\equity\\usa\\map_files\\LPT7.csv" },
            new object[] { "data\\equity\\usa\\map_files\\LPT8.csv" },
            new object[] { "data\\equity\\usa\\map_files\\LPT9.csv" },
            new object[] { "data\\equity\\usa\\map_files\\AUX.csv" },
            new object[] { "data\\equity\\usa\\map_files\\NUL.csv" },
            new object[] { "data\\equity\\usa\\map_files\\PRN.csv" },
            new object[] { "data\\equity\\usa\\map_files\\CON.csv" },
            new object[] { "AUX.csv" },
            new object[] { "NUL.csv" },
            new object[] { "PRN.csv" },
            new object[] { "CON.csv" },
            new object[] { "COM0.csv" },
            new object[] { "COM1.csv" },
            new object[] { "COM2.csv" },
            new object[] { "COM3.csv" },
            new object[] { "COM4.csv" },
            new object[] { "COM5.csv" },
            new object[] { "COM6.csv" },
            new object[] { "COM7.csv" },
            new object[] { "COM8.csv" },
            new object[] { "COM9.csv" },
            new object[] { "LPT0.csv" },
            new object[] { "LPT1.csv" },
            new object[] { "LPT2.csv" },
            new object[] { "LPT3.csv" },
            new object[] { "LPT4.csv" },
            new object[] { "LPT5.csv" },
            new object[] { "LPT6.csv" },
            new object[] { "LPT7.csv" },
            new object[] { "LPT8.csv" },
            new object[] { "LPT9.csv" },
            new object[] { "AUX.tar.gz" },
            new object[] { "NUL.tar.gz" },
            new object[] { "PRN.tar.gz" },
            new object[] { "CON.tar.gz" },
            new object[] { "equity\\usa\\minute\\con\\20150903_trade.zip" },
            new object[] { "equity\\usa\\minute\\nul\\20150903_trade.zip" },
            new object[] { "equity\\usa\\minute\\prn\\20150903_trade.zip" },
            new object[] { "equity\\usa\\minute\\aux\\20150903_trade.zip" },
            new object[] { "equity\\usa\\minute\\con\\con.zip" },
            new object[] { "equity\\usa\\minute\\nul\\nul.zip" },
            new object[] { "equity\\usa\\minute\\prn\\prn.zip" },
            new object[] { "equity\\usa\\minute\\aux\\aux.zip" }
        };
    }
}
