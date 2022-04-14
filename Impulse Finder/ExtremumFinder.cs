﻿using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.Events;

namespace cAlgo
{
    /// <summary>
    /// Class allows to find all the extrema from the set of market candles
    /// </summary>
    public class ExtremumFinder
    {
        private double m_ExtremumPrice;
        private int m_ExtremumIndex;
        private readonly double m_DeviationPercent;
        private bool m_IsUpDirection;

        /// <summary>
        /// Gets the deviation price in absolute value.
        /// </summary>
        private double DeviationPrice
        {
            get
            {
                double percentRate = m_IsUpDirection ? -0.01 : 0.01;
                return m_ExtremumPrice * (1.0 + m_DeviationPercent * percentRate);
            }
        }

        /// <summary>
        /// Moves the extremum to the (index, price) point.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="price">The price.</param>
        private void MoveExtremum(int index, double price)
        {
            Extrema.Remove(m_ExtremumIndex);
            SetExtremum(index, price);
        }

        /// <summary>
        /// Sets the extremum to the (index, price) point.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="price">The price.</param>
        private void SetExtremum(int index, double price)
        {
            if (OnExtremumSet != null)
            {
                OnExtremumSet(this, new ExtremumEventArgs
                    {
                        OldIndex = m_ExtremumIndex,
                        Index = index,
                        Value = price
                    });
            }

            m_ExtremumIndex = index;
            m_ExtremumPrice = price;
            Extrema[m_ExtremumIndex] = m_ExtremumPrice;
        }

        /// <summary>
        /// Occurs when an extremum is set.
        /// </summary>
        public event EventHandler<ExtremumEventArgs> OnExtremumSet;

        /// <summary>
        /// Gets the collection of extrema found.
        /// </summary>
        public SortedDictionary<int, double> Extrema { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtremumFinder"/> class.
        /// </summary>
        /// <param name="deviationPercent">The deviation percent.</param>
        public ExtremumFinder(double deviationPercent)
        {
            m_DeviationPercent = deviationPercent;
            Extrema = new SortedDictionary<int, double>();
        }

        /// <summary>
        /// Calculates the extrema from <see cref="startDate"/> to <see cref="endDate"/> and <see cref="bars"/>.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="bars">The bars.</param>
        public void Calculate(DateTime startDate, DateTime endDate, Bars bars)
        {
            int startIndex = bars.OpenTimes.GetIndexByTime(startDate);
            int endIndex = bars.OpenTimes.GetIndexByTime(endDate);
            for (int i = startIndex; i <= endIndex; i++)
            {
                Calculate(i, bars);
            }
        }

        /// <summary>
        /// Calculates the extrema for the specified <see cref="index"/> and <see cref="bars"/>.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="bars">The bars.</param>
        public void Calculate(int index, Bars bars)
        {
            double low = bars.LowPrices[index];
            double high = bars.HighPrices[index];
            if (m_ExtremumPrice == 0.0)
            {
                m_ExtremumPrice = high;
            }

            if (bars.ClosePrices.Count < 2)
            {
                return;
            }

            if (m_IsUpDirection ? high >= m_ExtremumPrice : low <= m_ExtremumPrice)
            {
                MoveExtremum(index, m_IsUpDirection ? high : low);
                return;
            }

            if (m_IsUpDirection ? low <= DeviationPrice : high >= DeviationPrice)
            {
                SetExtremum(index, m_IsUpDirection ? low : high);
                m_IsUpDirection = !m_IsUpDirection;
            }
        }
    }
}
