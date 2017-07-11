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
                this.Size = new System.Drawing.Size(275, 50);

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
        public Dictionary<string, List<StockLine>> Stocks = new Dictionary<string, List<StockLine>>();

        /// <summary>
        /// Determines the order that the groups should be 
        /// </summary>
        public List<string> GroupOrder = new List<string>();

        /// <summary>
        /// Keeps track of the labels for each group
        /// </summary>
        private Dictionary<string, Label> GroupLabels = new Dictionary<string, Label>();
        #endregion


        /// <summary>
        /// Adds a new symbol to the list
        /// </summary>
        /// <param name="symbol">The ticker symbol to add</param>
        public void Add(string group, string symbol)
        {
            // Ensure the symbol is not already in the list
            bool isNew = true;
            List<StockLine> stockList;
            if(!Stocks.TryGetValue(group, out stockList))
            {
                stockList = new List<StockLine>();
                Stocks.Add(group, stockList);
                GroupOrder.Add(group);
                GroupLabels.Add(group, new Label());
                Controls.Add(GroupLabels[group]);
            }
            for(int i = 0; i < stockList.Count; i++)
            {
                if(stockList[i].Symbol.Equals(symbol))
                {
                    isNew = false;
                    break;
                }
            }
            if(isNew)
            {
                StockLine newLine = new StockLine(symbol);
                newLine.Location = new System.Drawing.Point(5, (int)((Stocks.Count + 0.5) * (newLine.Height + 5)));
                stockList.Add(newLine);
                this.Controls.Add(newLine);
                this.Refresh();

                // Request data to fill the summary chart
                DataAccessor.GetPriceHistory(symbol, DateTime.Now.Date.AddHours(-12), DateTime.Now.Date.AddHours(16), new TimeSpan(0, 1, 0), newLine.Update);
            }
        }

        /// <summary>
        /// Removes a symbol from the list
        /// </summary>
        /// <param name="symbol">The stock symbol to remove</param>
        public void Remove(string symbol)
        {
            List<string> emptyGroups = new List<string>();

            foreach(var stockList in Stocks)
            {
                for(int i = 0; i < stockList.Value.Count; i++)
                {
                    if(stockList.Value[i].Name.Equals(symbol))
                    {
                        stockList.Value.RemoveAt(i);
                        i--;
                    }
                }

                // Check if the group is now empty
                if(stockList.Value.Count == 0)
                {
                    emptyGroups.Add(stockList.Key);
                }
            }

            foreach(var group in emptyGroups)
            {
                Stocks.Remove(group);
                GroupOrder.Remove(group);
                Controls.Remove(GroupLabels[group]);
                GroupLabels.Remove(group);
            }
        }

        /// <summary>
        /// Refreshes the control, re-positioning all of the stocks
        /// </summary>
        public override void Refresh()
        {
            int yPos = 5;
            int spacing = 5;

            // Place each group
            for(int i = 0; i < GroupOrder.Count; i++)
            {
                string group = GroupOrder[i];
                GroupLabels[group].Text = group;
                GroupLabels[group].Location = new System.Drawing.Point(GroupLabels[group].Location.X, yPos);
                yPos += (GroupLabels[group].Height + spacing);

                // Place all of the stocks in the group
                foreach(StockLine stock in Stocks[group])
                {
                    stock.Location = new System.Drawing.Point(stock.Location.X, yPos);
                    yPos += (stock.Height + spacing);
                }

                // Add a little extra space between groups
                yPos += spacing;
            }

            base.Refresh();
        }
    }
}
