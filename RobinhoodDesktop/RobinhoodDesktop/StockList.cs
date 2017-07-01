using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace RobinhoodDesktop
{
    public class StockList : Panel
    {


        #region Types
        public class StockLine : Panel
        {
            public StockLine()
            {
                Initialize();
            }

            public StockLine(string symbol)
            {
                Initialize();
                SetTicker(symbol);
            }

            private void Initialize()
            {
                this.Size = new System.Drawing.Size(300, 50);

                TickerLabel = new Label();
                TickerLabel.Size = new System.Drawing.Size(50, 15);
                TickerLabel.Location = new System.Drawing.Point(5, (this.Size.Height / 2) - (TickerLabel.Size.Height / 2));
                Controls.Add(TickerLabel);

                SummaryChart = new StockChartBasic();
                SummaryChart.Canvas.Size = new System.Drawing.Size(150, this.Size.Height);
                SummaryChart.Canvas.Location = new System.Drawing.Point((TickerLabel.Location.X + TickerLabel.Size.Width) + 5, 0);
                Controls.Add(SummaryChart.Canvas);

                InfoLabel = new Label();
                InfoLabel.Location = new System.Drawing.Point((SummaryChart.Canvas.Location.X + SummaryChart.Canvas.Width) + 5, TickerLabel.Location.Y);
                InfoLabel.Size = new System.Drawing.Size(this.Size.Width - InfoLabel.Location.X, 15);
                Controls.Add(InfoLabel);
            }

            #region Variables
            /// <summary>
            /// The symbol this line is showing
            /// </summary>
            public string Symbol;

            /// <summary>
            /// The label for the stock ticker symbol
            /// </summary>
            private Label TickerLabel;

            /// <summary>
            /// The chart used to show the summary of the stock price
            /// </summary>
            private StockChartBasic SummaryChart;

            /// <summary>
            /// The label used to display information about the stock
            /// </summary>
            private Label InfoLabel;
            #endregion

            /// <summary>
            /// Sets the ticker symbol being shown by this line
            /// </summary>
            /// <param name="symbol"></param>
            public void SetTicker(string symbol)
            {
                this.Symbol = symbol;
                TickerLabel.Text = symbol;
            }

            /// <summary>
            /// Updates the item with new data
            /// </summary>
            /// <param name="data"></param>
            public void Update(DataTable data)
            {
                SummaryChart.SetChartData(data);
                BeginInvoke((Action)(() => { InfoLabel.Text = string.Format("{0:c}", (float)data.Rows[data.Rows.Count - 1][StockChartBasic.PRICE_DATA_TAG]); }));
            }
        }
        #endregion

        #region Variables
        /// <summary>
        /// The list of stocks this is representing
        /// </summary>
        private List<StockLine> Stocks = new List<StockLine>();
        #endregion


        /// <summary>
        /// Adds a new symbol to the list
        /// </summary>
        /// <param name="symbol">The ticker symbol to add</param>
        public void Add(string symbol)
        {
            // Ensure the symbol is not already in the list
            bool isNew = true;
            for(int i = 0; i < Stocks.Count; i++)
            {
                if(Stocks[i].Name.Equals(symbol))
                {
                    isNew = false;
                    break;
                }
            }
            if(isNew)
            {
                StockLine newLine = new StockLine(symbol);
                newLine.Location = new System.Drawing.Point(5, (int)((Stocks.Count + 0.5) * (newLine.Height + 5)));
                Stocks.Add(newLine);
                this.Controls.Add(newLine);
                this.Refresh();

                // Request data to fill the summary chart
                DataAccessor.GetPriceHistory(symbol, DateTime.Now.Date.AddHours(-12), DateTime.Now.Date.AddHours(16), new TimeSpan(0, 1, 0), newLine.Update);
            }
        }
    }
}
