# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from AlgorithmImports import *
from collections import deque

### <summary>
### Regression test to check Python indicator is keeping backwards compatibility 
### with indicators that do not set WarmUpPeriod.
### </summary>
### <meta name="tag" content="indicators" />
### <meta name="tag" content="indicator classes" />
### <meta name="tag" content="custom indicator" />
### <meta name="tag" content="regression test" />
class CustomWarmUpPeriodIndicatorAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)
        self.AddEquity("SPY", Resolution.Second)

        # Create two python indicators, one defines Warm Up
        self.custom = CSMAWithoutWarmUp('custom', 60)
        self.customWarmUp = CSMAWithWarmUp('customWarmUp', 60)

        # The python custom class must inherit from PythonIndicator to enable Updated event handler
        self.customWarmUp.Updated += self.CustomWarmUpUpdated
        self.custom.Updated += self.CustomUpdated

        self.customWarmUpWindow = RollingWindow[IndicatorDataPoint](5)
        self.customWindow = RollingWindow[IndicatorDataPoint](5)
        self.RegisterIndicator("SPY", self.customWarmUp, Resolution.Minute)
        self.RegisterIndicator("SPY", self.custom, Resolution.Minute)

        self.SetWarmUp(60);

    def CustomWarmUpUpdated(self, sender, updated):
        self.customWarmUpWindow.Add(updated)

    def CustomUpdated(self, sender, updated):
        self.customWindow.Add(updated)

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

        if self.Time.second == 0:
            self.Log(f"   customWarmUp -> IsReady: {self.customWarmUp.IsReady}. Value: {self.customWarmUp.Current.Value}")
            self.Log(f"custom -> IsReady: {self.custom.IsReady}. Value: {self.custom.Value}")

        # Regression test: test fails with an early quit
        diff = abs(self.custom.Current.Value - self.customWarmUp.Current.Value)
        if diff > 1e-10:
            self.Quit(f"Quit: indicators difference is {diff}")

    def OnEndOfAlgorithm(self):
        if not self.customWarmUp.IsReady:
            raise "customWarmUp indicator was expected to be warmed up already"

# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA) With Warm Up Period parameter defined
class CSMAWithWarmUp(PythonIndicator):
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)
        self.WarmUpPeriod = period

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = np.sum(self.queue) / count
        return count == self.queue.maxlen

# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA) without Warm Up Period parameter defined
class CSMAWithoutWarmUp(PythonIndicator):
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = np.sum(self.queue) / count
        return count == self.queue.maxlen
