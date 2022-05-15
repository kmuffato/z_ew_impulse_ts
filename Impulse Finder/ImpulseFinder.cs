﻿using System;
using cAlgo.API;

namespace cAlgo
{
    /// <summary>
    /// Indicator can find possible setups based on initial impulses (wave 1 or A)
    /// </summary>
    /// <seealso cref="Indicator" />
    [Indicator(IsOverlay = true, AutoRescale = true, AccessRights = AccessRights.FullAccess)]
    public class ImpulseFinder : Indicator
    {
        [Output("EnterPrices", LineColor = "Gray")]
        public IndicatorDataSeries EnterPrices { get; set; }

        [Output("TakeProfits", LineColor = "Green")]
        public IndicatorDataSeries TakeProfits { get; set; }

        [Output("StopLosses", LineColor = "Orange")]
        public IndicatorDataSeries StopLosses { get; set; }

        /// <summary>
        /// Gets or sets the allowance to impulse recognition in percents.
        /// </summary>
        [Parameter("DeviationPercent", DefaultValue = 0.5, MinValue = 0.01)]
        public double DeviationPercent { get; set; }

        /// <summary>
        /// Gets or sets the allowance for the correction harmony (2nd and 4th waves).
        /// </summary>
        [Parameter("DeviationPercentCorrection", DefaultValue = 150, MinValue = 1)]
        public double DeviationPercentCorrection { get; set; }
        
        private string StartSetupLineChartName =>
            "StartSetupLine" + Bars.OpenTimes.Last(1);

        private string EndSetupLineChartName =>
            "EndSetupLine" + Bars.OpenTimes.Last(1);

        private string EnterChartName => "Enter" + Bars.OpenTimes.Last(1);

        private string StopChartName => "SL" + Bars.OpenTimes.Last(1);

        private string ProfitChartName => "TP" + Bars.OpenTimes.Last(1);

        private SetupFinder m_SetupFinder;
        private IBarsProvider m_BarsProvider;
        private Bars m_BarsMinor;

        private const double MINOR_TF_RATIO = 5;

        /// <summary>
        /// Custom initialization for the Indicator. This method is invoked when an indicator is launched.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            TimeFrame minorTf =
                TimeFrameHelper.GetNextTimeFrame(TimeFrame, MINOR_TF_RATIO);
            m_BarsMinor = MarketData.GetBars(minorTf);
            m_BarsProvider = new CTraderBarsProvider(Bars, MarketData);
            var minorBarsProvider = new CTraderBarsProvider(m_BarsMinor, MarketData);
            m_BarsProvider.LoadBars();
            m_SetupFinder = new SetupFinder(
                DeviationPercent,
                DeviationPercentCorrection,
                minorBarsProvider,
                m_BarsProvider);
            m_SetupFinder.OnEnter += OnEnter;
            m_SetupFinder.OnStopLoss += OnStopLoss;
            m_SetupFinder.OnTakeProfit += OnTakeProfit;
        }

        protected override void OnDestroy()
        {
            m_SetupFinder.OnEnter -= OnEnter;
            m_SetupFinder.OnStopLoss -= OnStopLoss;
            m_SetupFinder.OnTakeProfit -= OnTakeProfit;
            base.OnDestroy();
        }

        private void OnStopLoss(object sender, EventArgs.LevelEventArgs e)
        {
            Chart.DrawIcon(StopChartName, ChartIconType.Star, e.Level.Index
                , e.Level.Price, Color.Red);
            Print($"SL hit! Price:{e.Level.Price}");
        }

        private void OnTakeProfit(object sender, EventArgs.LevelEventArgs e)
        {
            Chart.DrawIcon(ProfitChartName, ChartIconType.Star, e.Level.Index, e.Level.Price, Color.Green);
            Print($"TP hit! Price:{e.Level.Price}");
        }

        private void OnEnter(object sender, EventArgs.SignalEventArgs e)
        {
            Chart.DrawTrendLine(StartSetupLineChartName, e.TakeProfit.Index, e.TakeProfit.Price, e.Level.Index, e.Level.Price, Color.Gray);
            Chart.DrawTrendLine(EndSetupLineChartName, e.StopLoss.Index, e.StopLoss.Price, e.Level.Index, e.Level.Price, Color.Gray);
            Chart.DrawIcon(EnterChartName, ChartIconType.Star, e.Level.Index, e.Level.Price, Color.White);

            EnterPrices[e.Level.Index] = e.Level.Price;
            TakeProfits[e.Level.Index] = e.TakeProfit.Price;
            StopLosses[e.Level.Index] = e.StopLoss.Price;
            Print($"New setup found! Price:{e.Level.Price}");
        }

        private bool m_SavedFileTest = false;

        /// <summary>
        /// Calculate the value(s) of indicator for the given index.
        /// </summary>
        /// <param name="index">The index of calculated value.</param>
        public override void Calculate(int index)
        {
            try
            {
                m_SetupFinder.CheckSetup(index);
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debugger.Launch();
                Print(ex.Message);
            }

            if (!IsLastBar)
            {
                return;
            }

            if (m_SavedFileTest)
            {
                return;
            }

            m_SavedFileTest = true;
            
            Print($"History calculation is completed, index {index}");
            // Here we want to save the market data to the file.
            // The code below is for testing purposes only.
            //m_SavedFileTest = true;
            //var jsonBarKeeper = new JsonBarKeeper();
            //jsonBarKeeper.Save(m_BarsProviders, SymbolName);
        }
    }
}
