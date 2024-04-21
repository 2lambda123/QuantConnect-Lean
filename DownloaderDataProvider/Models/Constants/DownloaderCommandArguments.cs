/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using McMaster.Extensions.CommandLineUtils;

namespace QuantConnect.Lean.DownloaderDataProvider.Models.Constants
{
    public sealed class DownloaderCommandArguments
    {
        public const string CommandDownloaderDataProvider = "data-provider";

        public const string CommandDestinationDirectory = "destination-dir";

        public const string CommandDataType = "data-type";

        public const string CommandTickers = "tickers";

        public const string CommandSecurityType = "security-type";

        public const string CommandMarketName = "market";

        public const string CommandResolution = "resolution";

        public const string CommandStartDate = "start-date";

        public const string CommandEndDate = "end-date";
    }
}
