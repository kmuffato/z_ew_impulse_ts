﻿using System;
using System.IO;
using System.Linq;
using cAlgo.API;
using Newtonsoft.Json;

namespace cAlgo.Json
{
    /// <summary>
    /// Provides market bars from a json file.
    /// </summary>
    /// <seealso cref="cAlgo.IBarsProvider" />
    public class JsonBarsProvider : IBarsProvider
    {
        private readonly string m_InputJsonFile;
        private JsonTimeFrame m_JsonTimeFrame;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonBarsProvider"/> class.
        /// </summary>
        /// <param name="inputJsonFile">The input json file.</param>
        /// <param name="timeFrame">The time frame.</param>
        /// <param name="limit">The limit.</param>
        public JsonBarsProvider(
            string inputJsonFile, TimeFrame timeFrame, int limit)
        {
            m_InputJsonFile = inputJsonFile;
            TimeFrame = timeFrame;
            Limit = limit;
        }

        /// <summary>
        /// Gets the low price of the candle by the <see cref="index" /> specified.
        /// </summary>
        /// <param name="index">The index.</param>
        public double GetLowPrice(int index)
        {
            return m_JsonTimeFrame.Bars[index].Low;
        }

        /// <summary>
        /// Gets the high price of the candle by the <see cref="index" /> specified.
        /// </summary>
        /// <param name="index">The index.</param>
        public double GetHighPrice(int index)
        {
            return m_JsonTimeFrame.Bars[index].High;
        }

        /// <summary>
        /// Gets the open time of the candle by the <see cref="index" /> specified
        /// </summary>
        /// <param name="index">The index.</param>
        public DateTime GetOpenTime(int index)
        {
            return m_JsonTimeFrame.Bars[index].OpenTime;
        }

        /// <summary>
        /// Gets the total count of bars collected.
        /// </summary>
        public int Count => m_JsonTimeFrame.Bars.Length;

        /// <summary>
        /// Loads the bars until <see cref="Limit" /> was reached.
        /// </summary>
        /// <exception cref="System.Exception">Cannot parse the file {m_InputJsonFile}</exception>
        public void LoadBars()
        {
            JsonHistory jsonHistory = JsonConvert.DeserializeObject<JsonHistory>(
                File.ReadAllText(m_InputJsonFile));
            if (jsonHistory == null)
            {
                throw new Exception($"Cannot parse the file {m_InputJsonFile}");
            }

            m_JsonTimeFrame = jsonHistory.JsonTimeFrames
                .Single(a => a.TimeFrameName == TimeFrame.ToString());
        }

        /// <summary>
        /// Gets the limit amount for bars loaded.
        /// </summary>
        public int Limit { get; }

        /// <summary>
        /// Gets the start bar index according by limit.
        /// </summary>
        public int StartIndexLimit => Count - Limit;

        /// <summary>
        /// Gets the time frame of the current instance.
        /// </summary>
        public TimeFrame TimeFrame { get; }

        private TimeSpan TimeSpan => TimeFrameHelper.TimeFrames[TimeFrame].TimeSpan;

        /// <summary>
        /// Gets the int index of bar (candle) by datetime.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        public int GetIndexByTime(DateTime dateTime)
        {
            return GetIndexByTimeInner(0, Count - 1, dateTime);
        }

        private int GetIndexByTimeInner(
            int startIndex, int endIndex, DateTime dateTime)
        {
            double midIndexDouble = ((double)endIndex - startIndex) / 2;
            if (midIndexDouble < 1)
            {
                return -1;
            }

            int midIndex = Convert.ToInt32(midIndexDouble);

            DateTime midDateTime = GetOpenTime(midIndex);
            if (dateTime >= midDateTime)
            {
                if (dateTime < midDateTime.Add(TimeSpan))
                {
                    return midIndex;
                }

                startIndex = midIndex;
            }
            else
            {
                endIndex = midIndex;
            }

            return GetIndexByTimeInner(startIndex, endIndex, dateTime);
        }

        /// <summary>
        /// Gets the open time for the last bar available.
        /// </summary>
        public DateTime GetLastBarOpenTime()
        {
            return m_JsonTimeFrame.Bars[Count - 1].OpenTime;
        }
    }
}
