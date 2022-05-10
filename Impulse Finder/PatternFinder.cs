﻿using System;

namespace cAlgo
{
    /// <summary>
    /// Contains pattern-finding logic for the Elliott Waves structures.
    /// </summary>
    public class PatternFinder
    {
        private readonly double m_CorrectionAllowancePercent;
        private readonly double m_Deviation;
        private readonly IBarsProvider m_BarsProvider;
        private const int IMPULSE_EXTREMA_COUNT = 6;
        private const int SIMPLE_EXTREMA_COUNT = 2;
        private const int IMPULSE_EXTREMA_INDEX = IMPULSE_EXTREMA_COUNT - 1;
        private const int IMPULSE_EXTREMA_STEP_COUNT = 4;
        private const int ZIGZAG_EXTREMA_COUNT = 4;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternFinder"/> class.
        /// </summary>
        /// <param name="correctionAllowancePercent">The correction allowance percent.</param>
        /// <param name="deviation">The deviation.</param>
        /// <param name="barsProvider">The bars provider.</param>
        public PatternFinder(double correctionAllowancePercent,
            double deviation,
            IBarsProvider barsProvider)
        {
            m_CorrectionAllowancePercent = correctionAllowancePercent;
            m_Deviation = deviation;
            m_BarsProvider = barsProvider;
        }

        /// <summary>
        /// Determines whether the specified interval has a zigzag.
        /// </summary>
        /// <param name="start">The start of the interval.</param>
        /// <param name="end">The end of the interval.</param>
        /// <returns>
        ///   <c>true</c> if the specified interval has a zigzag; otherwise, <c>false</c>.
        /// </returns>
        private bool IsZigzag(DateTime start, DateTime end)
        {
            var minorExtremumFinder =
                new ExtremumFinder(m_Deviation, m_BarsProvider);
            minorExtremumFinder.Calculate(start, end);
            Extremum[] extrema = minorExtremumFinder.ToExtremaArray();
            int count = extrema.Length;

            if (count == IMPULSE_EXTREMA_COUNT ||
                count == IMPULSE_EXTREMA_COUNT + 1)
            {
                return false;
            }

            if (count == ZIGZAG_EXTREMA_COUNT ||
                count == ZIGZAG_EXTREMA_COUNT + 1)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified extrema is an simple impulse.
        /// Simple impulse has <see cref="IMPULSE_EXTREMA_COUNT"/> extrema and 5 waves
        /// </summary>
        /// <param name="extrema">The extrema.</param>
        /// <returns>
        ///   <c>true</c> if the specified extrema is an simple impulse; otherwise, <c>false</c>.
        /// </returns>
        private bool IsSimpleImpulse(Extremum[] extrema)
        {
            int count = extrema.Length;
            if (count == 0)
            {
                return false;
            }

            if (count == SIMPLE_EXTREMA_COUNT)
            {
                return true;
            }

            if (count == ZIGZAG_EXTREMA_COUNT)
            {
                return false;
            }

            int countRest = count - IMPULSE_EXTREMA_COUNT;
            int indexRest = countRest - 1;

            if (countRest == 0 /*|| countRest == 1*/)
            {

            } else if (countRest % IMPULSE_EXTREMA_STEP_COUNT == 0)
            {
                for (int i = 1; i < count; i++)
                {
                    //int endIndex = i + IMPULSE_EXTREMA_INDEX;
                    //if (endIndex >= count)
                    //{
                    //    return false;
                    //}

                    if (i % 2 == 0)// We don't want to consider corrections
                    {
                        continue;
                    }

                    bool isFirstWaveImpulse = IsSimpleImpulse(extrema[..i]);
                    if (!isFirstWaveImpulse)
                    {
                        continue;
                    }
                    // handle 2nd
                    for (int j = i; j < count; j++)
                    {
                        if (j % 2 == 0)
                        {
                            continue;
                        }
                        
                        bool isThirdWaveImpulse = IsSimpleImpulse(extrema[i..j]);
                        bool isFifthWaveImpulse = IsSimpleImpulse(extrema[j..]);

                        // handle 4th
                        if (isFifthWaveImpulse && isThirdWaveImpulse)
                        {
                            var builtExtrema = new Extremum[IMPULSE_EXTREMA_STEP_COUNT];
                            builtExtrema[0] = extrema[0];
                            builtExtrema[IMPULSE_EXTREMA_INDEX] = extrema[^1];
                            builtExtrema[1] = extrema[i];
                            builtExtrema[2] = extrema[j];

                            return true;
                        }
                    }
                }

                return false;
            }
            else
            {
                return false;
            }

            Extremum firstItem = extrema[0];
            Extremum lastItem = extrema[^1];
            bool isUp = lastItem.Value > firstItem.Value;
            
            Extremum firstWaveEnd = extrema[1];
            Extremum secondWaveEnd = extrema[2];
            Extremum thirdWaveEnd = extrema[3];
            Extremum fourthWaveEnd = extrema[4];
            Extremum fifthWaveEnd = extrema[5];

            double secondWaveDuration = (secondWaveEnd.OpenTime - firstWaveEnd.OpenTime).TotalSeconds;
            double fourthWaveDuration = (fourthWaveEnd.OpenTime - thirdWaveEnd.OpenTime).TotalSeconds;
            if (secondWaveDuration <= 0 || fourthWaveDuration <= 0)
            {
                return false;
            }

            // Check harmony between 2nd and 4th waves 
            double correctionRatio = fourthWaveDuration / secondWaveDuration;
            if (correctionRatio * 100 > m_CorrectionAllowancePercent ||
                correctionRatio < 100d / m_CorrectionAllowancePercent)
            {
                return false;
            }

            // Check the overlap rule
            if (isUp && firstWaveEnd.Value >= fourthWaveEnd.Value ||
                !isUp && firstWaveEnd.Value <= fourthWaveEnd.Value)
            {
                return false;
            }

            double firstWaveLength = (isUp ? 1 : -1) *
                                     (firstWaveEnd.Value - firstItem.Value);

            double thirdWaveLength = (isUp ? 1 : -1) *
                                     (thirdWaveEnd.Value - secondWaveEnd.Value);

            double fifthWaveLength = (isUp ? 1 : -1) *
                                     (fifthWaveEnd.Value - fourthWaveEnd.Value);

            if (firstWaveLength <= 0 ||
                thirdWaveLength <= 0 ||
                fifthWaveLength <= 0)
            {
                return false;
            }

            // Check the 3rd wave length
            if (thirdWaveLength < firstWaveLength &&
                thirdWaveLength < fifthWaveLength)
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Determines whether the interval between the dates is an impulse.
        /// </summary>
        /// <param name="dateStart">The date start.</param>
        /// <param name="dateEnd">The date end.</param>
        /// <returns>
        ///   <c>true</c> if the interval is impulse; otherwise, <c>false</c>.
        /// </returns>
        public bool IsImpulse(DateTime dateStart, DateTime dateEnd)
        {
            //bool isZigzag = IsZigzag(dateStart, dateEnd);


            // System.Diagnostics.Debugger.Launch();
            //// Let's look closer to the impulse waves 1, 3 and 5.
            //// We shouldn't pass zigzags in it
            //if (IsZigzag(firstItem.OpenTime, firstWaveEnd.CloseTime)
            //    || IsZigzag(secondWaveEnd.OpenTime, thirdWaveEnd.CloseTime)
            //    || IsZigzag(fourthWaveEnd.OpenTime, fifthWaveEnd.CloseTime))
            //{
            //    return false;
            //}

            var minorExtremumFinder = new ExtremumFinder(m_Deviation, m_BarsProvider);
            minorExtremumFinder.Calculate(dateStart, dateEnd);
            Extremum[] extrema = minorExtremumFinder.ToExtremaArray();
            bool isSimpleImpulse = IsSimpleImpulse(extrema);
            if (isSimpleImpulse)
            {
                return true;
            }

            return false;
        }
    }
}
