﻿using System.Collections.Generic;
using System.Linq;

namespace cAlgo
{
    /// <summary>
    /// Contains pattern-finding logic for the Elliott Waves structures.
    /// </summary>
    public static class PatternFinder
    {
        private const int IMPULSE_EXTREMA_COUNT = 6;

        /// <summary>
        /// Determines whether the specified extrema is an simple impulse.
        /// Simple impulse has <see cref="IMPULSE_EXTREMA_COUNT"/> extrema and 5 waves
        /// </summary>
        /// <param name="extremaList">The extrema - list of sorted arrays.</param>
        /// <param name="correctionAllowancePercent">The correction allowance percent.</param>
        /// <returns>
        ///   <c>true</c> if the specified extrema is an simple impulse; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsSimpleImpulse(
            List<Extremum[]> extremaList,
            double correctionAllowancePercent)
        {
            Extremum[] extrema = extremaList[0];
            int count = extrema.Length;
            if (count != IMPULSE_EXTREMA_COUNT)
                // support 10, 14, 18 as well with a recursive call maybe
            {
                return false;
            }

            Extremum firstItem = extrema[0];
            Extremum lastItem = extrema[count - 1];
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
            if (correctionRatio * 100 > correctionAllowancePercent ||
                correctionRatio < 100d / correctionAllowancePercent)
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
            if (thirdWaveLength < firstWaveLength ||
                thirdWaveLength < fifthWaveLength)
            {
                return false;
            }

            Extremum[] minorExtrema = extremaList.Skip(1).FirstOrDefault();
            if (minorExtrema == null)
            {
                // If we are here, so there are no minor extrema and
                // we've found an impulse
                return true;
            }


            Extremum[][] innerExtremaArray = extremaList.Skip(2).ToArray();
            bool CheckImpulse(Extremum start, Extremum end)
            {
                Extremum[] minorWave = minorExtrema
                    .SkipWhile(a => a.OpenTime < start.OpenTime)
                    .TakeWhile(a => a.OpenTime < end.CloseTime)
                    .ToArray();

                var innerAnalyzeList = new List<Extremum[]>(innerExtremaArray);
                innerAnalyzeList.Insert(0, minorWave);

                bool isWaveImpulse = IsSimpleImpulse(
                    innerAnalyzeList, correctionAllowancePercent);
                return isWaveImpulse;
            }

            // Let's look closer to the impulse waves 1, 3 and 5 using
            // the minor extrema provided
            if (!CheckImpulse(firstItem, firstWaveEnd) ||
                !CheckImpulse(secondWaveEnd, thirdWaveEnd) ||
                !CheckImpulse(fourthWaveEnd, fifthWaveEnd))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified extrema is an impulse.
        /// </summary>
        /// <param name="extremaSet">The extrema collection in the order of usage.</param>
        /// <param name="correctionAllowancePercent">The correction allowance percent.</param>
        /// <returns>
        ///   <c>true</c> if the specified extrema is impulse; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsImpulse(List<Extremum[]> extremaSet,
            double correctionAllowancePercent)
        {
            if (extremaSet == null || extremaSet.Count == 0)
            {
                return false;
            }

            int count = extremaSet[0].Length;
            if (count < IMPULSE_EXTREMA_COUNT)
            {
                // 0 -> wave 1 -> 2 -> 3 -> 4 -> 5
                return false;
            }

            bool isSimpleImpulse = IsSimpleImpulse(
                extremaSet, correctionAllowancePercent);

            return isSimpleImpulse;
        }
    }
}
