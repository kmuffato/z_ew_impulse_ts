﻿using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.EventArgs;

namespace cAlgo
{
    /// <summary>
    /// Class contains the logic of trade setups searching.
    /// </summary>
    public class SetupFinder
    {
        private readonly IBarsProvider m_BarsProvider;
        private readonly PatternFinder m_PatternFinder;
        private readonly ExtremumFinder m_ExtremumFinder;
        private bool m_IsInSetup;
        private int m_SetupStartIndex;
        private int m_SetupEndIndex;
        private double m_SetupStartPrice;
        private double m_SetupEndPrice;
        private double m_TriggerLevel;
        private int m_TriggerBarIndex;

        private const double TRIGGER_LEVEL_RATIO = 0.5;
        
        private const int IMPULSE_END_NUMBER = 1;
        private const int IMPULSE_START_NUMBER = 2;
        // We want to collect at lease this amount of extrema
        // 1. Extremum of a correction.
        // 2. End of the impulse
        // 3. Start of the impulse
        // 4. The previous extremum (to find out, weather this impulse is an initial one or not).
        private const int MINIMUM_EXTREMA_COUNT_TO_CALCULATE = 2;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetupFinder"/> class.
        /// </summary>
        /// <param name="deviationPercent">The deviation percent.</param>
        /// <param name="correctionAllowancePercent">The correction allowance percent.</param>
        /// <param name="barsProvider">The bars provider.</param>
        public SetupFinder(
            double deviationPercent,
            double correctionAllowancePercent,
            IBarsProvider barsProvider)
        {
            m_BarsProvider = barsProvider;
            m_ExtremumFinder = new ExtremumFinder(deviationPercent, m_BarsProvider);
            m_PatternFinder = new PatternFinder(
                correctionAllowancePercent, deviationPercent, barsProvider);
        }

        /// <summary>
        /// Occurs on stop loss.
        /// </summary>
        public event EventHandler<LevelEventArgs> OnStopLoss;

        /// <summary>
        /// Occurs when on take profit.
        /// </summary>
        public event EventHandler<LevelEventArgs> OnTakeProfit;

        /// <summary>
        /// Occurs when a new setup is found.
        /// </summary>
        public event EventHandler<SignalEventArgs> OnEnter;

        /// <summary>
        /// Determines whether the movement from <see cref="startValue"/> to <see cref="endValue"/> is initial. We use current bar position and <see cref="IMPULSE_START_NUMBER"/> to rewind the bars to the past.
        /// </summary>
        /// <param name="startValue">The start value.</param>
        /// <param name="endValue">The end value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns>
        ///   <c>true</c> if the move is initial; otherwise, <c>false</c>.
        /// </returns>
        private bool IsInitialMovement(
            double startValue, double endValue, int startIndex)
        {
            // We want to rewind the bars to be sure this impulse candidate is really an initial one
            bool isInitialMove = false;
            bool isImpulseUp = endValue > startValue;
            for (int curIndex = startIndex - 1; curIndex >= 0; curIndex--)
            {
                double curValue = m_ExtremumFinder
                    .Extrema
                    .ElementAt(curIndex).Value.Value;
                if (isImpulseUp)
                {
                    if (curValue <= startValue)
                    {
                        break;
                    }

                    if (curValue > endValue)
                    {
                        isInitialMove = true;
                        break;
                    }

                    continue;
                }

                if (curValue >= startValue)
                {
                    break;
                }

                if (curValue < endValue)
                {
                    isInitialMove = true;
                    break;
                }
            }

            return isInitialMove;
        }

        /// <summary>
        /// Checks the conditions of possible setup for <see cref="minorIndex"/>.
        /// </summary>
        /// <param name="minorIndex">The index of bar to calculate.</param>
        public void CheckSetup(int minorIndex)
        {
            m_ExtremumFinder.Calculate(minorIndex);
            SortedDictionary<int, Extremum> extrema = m_ExtremumFinder.Extrema;
            int count = extrema.Count;
            if (count < MINIMUM_EXTREMA_COUNT_TO_CALCULATE)
            {
                return;
            }

            double low = m_BarsProvider.GetLowPrice(minorIndex);
            double high = m_BarsProvider.GetHighPrice(minorIndex);

            int startIndex = count - IMPULSE_START_NUMBER;
            int endIndex = count - IMPULSE_END_NUMBER;
            KeyValuePair<int, Extremum> startItem = extrema
                .ElementAt(startIndex);
            KeyValuePair<int, Extremum> endItem = extrema
                .ElementAt(endIndex);

            bool isInSetupBefore = m_IsInSetup;
            void CheckImpulse()
            {
                double startValue = startItem.Value.Value;
                double endValue = endItem.Value.Value;

                var isImpulseUp = endValue > startValue;
                double maxValue = Math.Max(startValue, endValue);
                double minValue = Math.Min(startValue, minorIndex);
                for (int i = endItem.Key + 1; i < minorIndex; i++)
                {
                    if (maxValue <= m_BarsProvider.GetHighPrice(i) ||
                        minValue >= m_BarsProvider.GetLowPrice(i))
                    {
                        return;
                        // The setup is no longer valid, TP or SL is already hit.
                    }
                }

                bool isInitialMove = IsInitialMovement(startValue, endValue, startIndex);
                if (!isInitialMove)
                {
                    // The move (impulse candidate) is no longer initial.
                    return;
                }

                double triggerSize = Math.Abs(endValue - startValue) * TRIGGER_LEVEL_RATIO;

                double triggerLevel;
                bool gotSetup;
                if (isImpulseUp)
                {
                    triggerLevel = endValue - triggerSize;
                    gotSetup = low <= triggerLevel && low > startValue;
                }
                else
                {
                    triggerLevel = endValue + triggerSize;
                    gotSetup = high >= triggerLevel && high < startValue;
                }

                if (!gotSetup)
                {
                    return;
                }
                
                bool isImpulse = m_PatternFinder.IsImpulse(
                    startItem.Value, endItem.Value, 
                    isImpulseUp, out List<Extremum> outExtrema);
                if (!isImpulse)
                {
                    // The move is not an impulse.
                    return;
                }

                if (m_SetupStartIndex == startItem.Key ||
                    m_SetupEndIndex == endItem.Key)
                {
                    // Cannot use the same impulse twice.
                    return;
                }

                if (endItem.Key == minorIndex)
                {
                    // Wait for the next bar
                    return;
                }

                m_TriggerLevel = triggerLevel;
                m_TriggerBarIndex = minorIndex;
                m_IsInSetup = true;
                
                double endAllowance = Math.Abs(triggerLevel - endValue) * Helper.PERCENT_ALLOWANCE_TP / 100;
                double startAllowance = Math.Abs(triggerLevel - startValue) * Helper.PERCENT_ALLOWANCE_SL / 100;

                m_SetupStartIndex = startItem.Key;
                m_SetupEndIndex = endItem.Key;

                if (isImpulseUp)
                {
                    m_SetupStartPrice = startValue - startAllowance;
                    m_SetupEndPrice = endValue - endAllowance;
                }
                else
                {
                    m_SetupStartPrice = startValue + startAllowance;
                    m_SetupEndPrice = endValue + endAllowance;
                }

                var tpArg = new LevelItem(m_SetupEndPrice, m_SetupEndIndex);
                var slArg = new LevelItem(m_SetupStartPrice, m_SetupStartIndex);

                OnEnter?.Invoke(this,
                    new SignalEventArgs(
                        new LevelItem(triggerLevel, minorIndex),
                        tpArg,
                        slArg,
                        outExtrema));
                // Here we should give a trade signal.
            }

            if (!m_IsInSetup)
            {
                for (;;)
                {
                    CheckImpulse();
                    if (m_IsInSetup)
                    {
                        break;
                    }
                    // We don't know how far we are from the nearest initial impulse
                    // so we go deep and check

                    startIndex -= 1;
                    endIndex -= 1;
                    if (startIndex < 0 || endIndex < 0)
                    {
                        break;
                    }

                    startItem = extrema.ElementAt(startIndex);
                    endItem = extrema.ElementAt(endIndex);

                    // If we are no longer between start and end of the impulse
                    if (startItem.Value.Value >= low && endItem.Value.Value >= low ||
                        startItem.Value.Value <= high && endItem.Value.Value <= high)
                    {
                        break;
                    }
                }
            }

            if (!m_IsInSetup)
            {
                return;
            }

            if (!isInSetupBefore)
            {
                return;
            }

            bool isImpulseUp = m_SetupEndPrice > m_SetupStartPrice;
            bool isProfitHit = isImpulseUp && high >= m_SetupEndPrice
                               || !isImpulseUp && low <= m_SetupEndPrice;
            
            if (isProfitHit)
            {
                m_IsInSetup = false;
                OnTakeProfit?.Invoke(this,
                    new LevelEventArgs(new LevelItem(m_SetupEndPrice, minorIndex),
                        new LevelItem(m_TriggerLevel, m_TriggerBarIndex)));
            }

            bool isStopHit = isImpulseUp && low <= m_SetupStartPrice
                             || !isImpulseUp && high >= m_SetupStartPrice;
            if (isStopHit)
            {
                m_IsInSetup = false;
                OnStopLoss?.Invoke(this,
                    new LevelEventArgs(new LevelItem(m_SetupStartPrice, minorIndex),
                        new LevelItem(m_TriggerLevel, m_TriggerBarIndex)));
            }
        }
    }
}
