﻿using System;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo
{
    public class CTraderBarsProvider : IBarsProvider
    {
        private readonly Bars m_Bars;
        private readonly MarketData m_MarketData;

        /// <summary>
        /// Initializes a new instance of the <see cref="CTraderBarsProvider"/> class.
        /// </summary>
        /// <param name="bars">The bars.</param>
        /// <param name="marketData">The market data.</param>
        public CTraderBarsProvider(Bars bars, MarketData marketData)
        {
            m_Bars = bars;
            m_MarketData = marketData;
        }

        /// <summary>
        /// Gets the low price of the candle by the <see cref="index" /> specified.
        /// </summary>
        /// <param name="index">The index.</param>
        public double GetLowPrice(int index)
        {
            return m_Bars.LowPrices[index];
        }

        /// <summary>
        /// Gets the high price of the candle by the <see cref="index" /> specified.
        /// </summary>
        /// <param name="index">The index.</param>
        public double GetHighPrice(int index)
        {
            return m_Bars.HighPrices[index];
        }
        
        /// <summary>
        /// Gets the open time of the candle by the <see cref="index" /> specified
        /// </summary>
        /// <param name="index">The index.</param>
        public DateTime GetOpenTime(int index)
        {
            return m_Bars.OpenTimes[index];
        }

        /// <summary>
        /// Gets the total count of bars collected.
        /// </summary>
        public int Count => m_Bars.Count;

        /// <summary>
        /// Gets the time frame of the current instance.
        /// </summary>
        public TimeFrame TimeFrame => m_Bars.TimeFrame;

        /// <summary>
        /// Gets the bars of the specified time frame.
        /// </summary>
        /// <param name="timeFrame">The time frame.</param>
        public IBarsProvider GetBars(TimeFrame timeFrame)
        {
            if (timeFrame == TimeFrame)
            {
                // Maybe it's better to return null
                return this;
            }

            Bars bars = m_MarketData.GetBars(timeFrame);
            var res = new CTraderBarsProvider(bars, m_MarketData);
            return res;
        }

        /// <summary>
        /// Gets the int index of bar (candle) by datetime.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        public int GetIndexByTime(DateTime dateTime)
        {
            return m_Bars.OpenTimes.GetIndexByTime(dateTime);
        }

        /// <summary>
        /// Gets the open time for the latest bar available.
        /// </summary>
        public DateTime GetLastBarOpenTime()
        {
            return m_Bars.LastBar.OpenTime;
        }
    }
}
