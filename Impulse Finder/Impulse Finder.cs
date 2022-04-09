﻿using cAlgo.API;

namespace cAlgo
{
    [Indicator(IsOverlay = true, AutoRescale = true, AccessRights = AccessRights.None)]
    public class ImpulseFinder : Indicator
    {
        [Parameter("DeviationPercent", DefaultValue = 0.05, MinValue = 0.01)]
        public double DeviationPercent { get; set; }

        [Output("Out", Color = Colors.LightGray, Thickness = 1, PlotType = PlotType.Line)]
        public IndicatorDataSeries Value { get; set; }

        private bool m_IsUpDirection;
        private double m_ExtremumPrice;
        private int m_ExtremumIndex;
        private string m_LastArrowName;

        private void MoveExtremum(int index, double price)
        {
            Value[m_ExtremumIndex] = double.NaN;
            SetExtremum(index, price);
        }

        private void SetExtremum(int index, double price)
        {
            m_ExtremumIndex = index;
            m_ExtremumPrice = price;
            Value[m_ExtremumIndex] = m_ExtremumPrice;
        }

        private double DeviationPrice
        {
            get
            {
                double percentRate = m_IsUpDirection ? -0.01 : 0.01;
                return m_ExtremumPrice * (1.0 + DeviationPercent * percentRate);
            }
        }

        private void CheckImpulse()
        {
            Chart.DrawIcon(m_LastArrowName,
                m_IsUpDirection 
                    ? ChartIconType.DownTriangle 
                    : ChartIconType.UpTriangle, 
                m_ExtremumIndex,
                m_ExtremumPrice, 
                Color.White);
        }

        public override void Calculate(int index)
        {
            double low = Bars.LowPrices[index];
            double high = Bars.HighPrices[index];
            if (m_ExtremumPrice == 0.0)
            {
                m_ExtremumPrice = high;
            }

            if (Bars.ClosePrices.Count < 2)
            {
                return;
            }

            if (m_IsUpDirection ? high >= m_ExtremumPrice : low <= m_ExtremumPrice)
            {
                MoveExtremum(index, m_IsUpDirection ? high : low);
                CheckImpulse();
                return;
            }

            if (m_IsUpDirection ? low <= DeviationPrice : high >= DeviationPrice)
            {
                m_LastArrowName = "Extremum" + Bars.OpenTimes.Last(1);
                SetExtremum(index, m_IsUpDirection ? low : high);
                m_IsUpDirection = !m_IsUpDirection;
                CheckImpulse();
            }
        }
    }
}
