using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace RobinhoodDesktop
{
    public class StockList : Panel
    {
        /// <summary>
        /// The stock list instance
        /// </summary>
        public static StockList Instance;

        public StockList()
        {
            Instance = this;
        }

        #region Interfaces
        public interface SummaryInterface
        {

        }
        #endregion

        #region Types
        /// <summary>
        /// Base type for the summary panels that are added to each stock list item
        /// </summary>
        public class SummaryPanel : Panel
        {
            /// <summary>
            /// The parent stock line this panel is a part of
            /// </summary>
            public StockLine ParentLine;

            /// <summary>
            /// The data subscription used to refresh the panel
            /// </summary>
            public DataAccessor.Subscription StockSubscription;

            public SummaryPanel(DataAccessor.Subscription subscription)
            {
                this.StockSubscription = subscription;
            }

            public virtual void Initialize()
            {

            }


            public virtual void Destroy()
            {
                if(StockSubscription != null)
                {
                    DataAccessor.Unsubscribe(StockSubscription);
                    StockSubscription = null;
                }
            }
        }

        public class StockLine : Panel
        {
            public StockLine(string symbol, StockList parentList, SummaryPanel infoPanel)
            {
                this.Size = new System.Drawing.Size(275, 50);
                ParentList = parentList;

                TickerLabel = new Label();
                TickerLabel.Size = new System.Drawing.Size(50, 15);
                TickerLabel.Location = new System.Drawing.Point(5, (this.Size.Height / 2) - (TickerLabel.Size.Height / 2));
                TickerLabel.MouseUp += (sender, e) => { this.OnMouseUp(e); };
                Controls.Add(TickerLabel);

                SummaryChart = new StockChartBasic(symbol);
                SummaryChart.Canvas.Size = new System.Drawing.Size(150, this.Size.Height);
                SummaryChart.Canvas.Location = new System.Drawing.Point((TickerLabel.Location.X + TickerLabel.Size.Width) + 5, 0);
                SummaryChart.Canvas.MouseUp += (sender, e) => { this.OnMouseUp(e); };
                SummaryChart.SetSubscritpion(infoPanel.StockSubscription);
                Controls.Add(SummaryChart.Canvas);

                InfoPanel = infoPanel;
                InfoPanel.Location = new System.Drawing.Point((SummaryChart.Canvas.Location.X + SummaryChart.Canvas.Width) + 5, 0);
                InfoPanel.Size = new System.Drawing.Size(this.Size.Width - InfoPanel.Location.X, this.Height);
                InfoPanel.ParentLine = this;
                InfoPanel.Initialize();
                Controls.Add(InfoPanel);

                this.Symbol = symbol;
                TickerLabel.Text = symbol;
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
            public StockChartBasic SummaryChart;

            /// <summary>
            /// The label used to display information about the stock
            /// </summary>
            public SummaryPanel InfoPanel;

            /// <summary>
            /// The list this line belongs to
            /// </summary>
            public StockList ParentList;
            #endregion
        }

        

        /// <summary>
        /// Displays the current price of the stock, as well as the percentage it has changed
        /// </summary>
        public class PercentageChangeSummary : SummaryPanel
        {
            public Label PriceLabel;
            public Label PercentageLabel;
            public Decimal RefPrice;

            public PercentageChangeSummary(DataAccessor.Subscription subscription) : base(subscription)
            {
                PriceLabel = new Label();
                //PriceLabel.Font = GuiStyle.Font;
                //PriceLabel.ForeColor = GuiStyle.DARK_GREY;
                PriceLabel.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
                PriceLabel.Size = new System.Drawing.Size(60, 15);
                PriceLabel.Location = new System.Drawing.Point(0, 5);
                Controls.Add(PriceLabel);

                PercentageLabel = new Label();
                //PercentageLabel.Font = GuiStyle.Font;
                //PercentageLabel.ForeColor = GuiStyle.DARK_GREY;
                PercentageLabel.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
                PercentageLabel.Size = PriceLabel.Size;
                PercentageLabel.Location = new System.Drawing.Point(PriceLabel.Location.X, PriceLabel.Location.Y + PriceLabel.Height + 5);
                Controls.Add(PercentageLabel);
            }

            public override void Initialize()
            {
                DataAccessor.Accessor.GetStockInfo(StockSubscription.Symbol, (DataAccessor.StockInfo info) => { this.RefPrice = info.PreviousClose; });
                StockSubscription.Notify += (DataAccessor.Subscription s) =>
                {
                    this.BeginInvoke((Action)(() =>
                    {
                        PriceLabel.Text = string.Format("{0:c}", s.Price);
                        PercentageLabel.Text = string.Format("{0}{1:P2}", (s.Price >= RefPrice) ? "+" : "-", (RefPrice != 0) ? Math.Abs((s.Price - RefPrice) / RefPrice) : 0);
                    }));
                };
            }
        }

        /// <summary>
        /// Displays the current total value of the position, as well as the percentage it has changed
        /// </summary>
        public class PositionChangeSummary : PercentageChangeSummary
        {
            public Broker.PositionSubscription PositionSubscription;

            public PositionChangeSummary(DataAccessor.Subscription subscription) : base(subscription)
            {

            }

            public override void Initialize()
            {
                PositionSubscription = Broker.Instance.SubscribeToPositionInfo(StockSubscription.Symbol);
                PositionSubscription.Notify += (Broker.PositionSubscription sub) =>
                {
                    if(sub.PositionInfo.Shares > 0)
                    {
                        this.RefPrice = sub.PositionInfo.AverageBuyPrice;
                    }
                    else
                    {
                        // Remove the position indication once there are no shares left
                        ParentLine.ParentList.Remove(ParentLine);
                    }
                };
                StockSubscription.Notify += (DataAccessor.Subscription s) =>
                {
                    this.BeginInvoke((Action)(() =>
                    {
                        PriceLabel.Text = string.Format("{0:c}", (s.Price * PositionSubscription.PositionInfo.Shares));
                        PercentageLabel.Text = string.Format("{0}{1:P2}", (s.Price >= RefPrice) ? "+" : "-", (RefPrice != 0) ? Math.Abs((s.Price - RefPrice) / RefPrice) : 0);
                    }));
                };
            }

            public override void Destroy()
            {
                Broker.Instance.UnsubscribePosition(PositionSubscription);
                PositionSubscription = null;

                base.Destroy();
            }
        }

        public class OrderSummary : SummaryPanel
        {
            public Label OrderLabel;
            public Broker.Order OrderInfo;
            public GuiButton CancelOrderButton;

            public OrderSummary(Broker.Order orderInfo) : base(DataAccessor.Subscribe(orderInfo.Symbol, DataAccessor.SUBSCRIBE_FIVE_SEC))
            {
                this.OrderInfo = orderInfo;

                OrderLabel = new Label();
                //OrderLabel.Font = GuiStyle.Font;
                //OrderLabel.ForeColor = GuiStyle.DARK_GREY;
                OrderLabel.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
                OrderLabel.Text = string.Format("{0} {1}", orderInfo.BuySell == Broker.Order.BuySellType.BUY ? "Buy" : "Sell", orderInfo.Quantity);
                OrderLabel.Size = new System.Drawing.Size(60, 15);
                OrderLabel.Location = new System.Drawing.Point(5, 5);
                Controls.Add(OrderLabel);

                CancelOrderButton = new GuiButton("Cancel");
                CancelOrderButton.Location = new System.Drawing.Point(OrderLabel.Location.X, OrderLabel.Location.Y + OrderLabel.Height + 5);
                CancelOrderButton.MouseClick += (sender, e) =>
                {
                    Broker.Instance.CancelOrder(OrderInfo.Symbol);
                };
                Controls.Add(CancelOrderButton);
            }
        }

        /// <summary>
        /// Displays a notification
        /// </summary>
        public class NotificationSummary : PercentageChangeSummary
        {
            public PictureBox CloseButton;
            public Broker.PositionSubscription PositionSubscription;

            public NotificationSummary(DataAccessor.Subscription subscription) : base(subscription)
            {
                CloseButton = new PictureBox();
                CloseButton.Image = new System.Drawing.Bitmap(System.Drawing.Bitmap.FromFile("Content/GUI/Button_Close.png"), new System.Drawing.Size(12, 12));
                CloseButton.Size = CloseButton.Image.Size;
                CloseButton.Location = new System.Drawing.Point(45, 0);
                CloseButton.MouseUp += (sender, e) => {
                    ParentLine.ParentList.Remove(ParentLine);
                };
                this.Controls.Add(CloseButton);
                this.Controls.SetChildIndex(CloseButton, 0);

                // Automatically open the buy/sell window when the notification is clicked
                this.MouseUp += (sender, e) =>
                {
                    if(PositionSubscription.PositionInfo.Shares > 0)
                    {
                        BuySellPanel.Instance.ShowOrderMenu(StockSubscription.Symbol, Broker.Order.BuySellType.SELL);
                    }
                    else
                    {
                        BuySellPanel.Instance.ShowOrderMenu(StockSubscription.Symbol, Broker.Order.BuySellType.BUY);
                    }
                };
                base.PriceLabel.MouseUp += (sender, e) => { this.OnMouseUp(e); };
                base.PercentageLabel.MouseUp += (sender, e) => { this.OnMouseUp(e); };
            }

            public override void Initialize()
            {

                // Attempt to get a reference price either as the purchase price if there is a position in this stock, or the day's starting price otherwise
                DataAccessor.Accessor.GetStockInfo(StockSubscription.Symbol, (DataAccessor.StockInfo info) => 
                {
                    if(PositionSubscription.PositionInfo.Shares == 0) this.RefPrice = info.PreviousClose;
                });
                PositionSubscription = Broker.Instance.SubscribeToPositionInfo(StockSubscription.Symbol);
                PositionSubscription.Notify += (Broker.PositionSubscription sub) =>
                {
                    if(sub.PositionInfo.Shares > 0)
                    {
                        this.RefPrice = sub.PositionInfo.AverageBuyPrice;
                    }
                };

                // Update the notification periodically
                StockSubscription.Notify += (DataAccessor.Subscription s) =>
                {
                    this.BeginInvoke((Action)(() =>
                    {
                        PriceLabel.Text = string.Format("{0:c}", s.Price);
                        PercentageLabel.Text = string.Format("{0}{1:P2}", (s.Price >= RefPrice) ? "+" : "-", (RefPrice != 0) ? Math.Abs((s.Price - RefPrice) / RefPrice) : 0);
                    }));
                };

                // Set the notification background color
                this.ParentLine.BackColor = GuiStyle.NOTIFICATION_COLOR;
            }

            public override void Destroy()
            {
                Broker.Instance.UnsubscribePosition(PositionSubscription);
                PositionSubscription = null;

                base.Destroy();
            }
        }
#endregion

#region Constants
        /// <summary>
        /// A group of positions currently held
        /// </summary>
        public const string POSITIONS = "Positions";

        /// <summary>
        /// A group of stocks being watched
        /// </summary>
        public const string WATCHLIST = "Watchlist";

        /// <summary>
        /// A group of currently pending orders
        /// </summary>
        public const string ORDERS = "Orders";

        /// <summary>
        /// A group of currently pending notifications
        /// </summary>
        public const string NOTIFICATIONS = "Notifications";
#endregion

#region Variables
        /// <summary>
        /// Callback function to add a new UI element for the specified stock
        /// </summary>
        public SearchList.StockSymbolCallback AddStockUi;

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
        /// <param name="group">The group the symbol should be added as part of</param>
        /// <param name="symbol">The ticker symbol to add</param>
        /// <param name="infoPanel">A panel that displays information about the stock</param>
        public void Add(string group, string symbol, SummaryPanel infoPanel = null)
        {
            if(this.IsHandleCreated)
            {
                this.BeginInvoke((Action)(() =>
                {
                    AddInternal(group, symbol, infoPanel);
                }));
            }
            else
            {
                AddInternal(group, symbol, infoPanel);
            }
        }

        /// <summary>
        /// Adds a new symbol to the list
        /// </summary>
        /// <param name="group">The group the symbol should be added as part of</param>
        /// <param name="symbol">The ticker symbol to add</param>
        /// <param name="infoPanel">A panel that displays information about the stock</param>
        private void AddInternal(string group, string symbol, SummaryPanel infoPanel)
        {
            DataAccessor.Subscription sub = null;
            if(infoPanel == null)
            {
                sub = DataAccessor.Subscribe(symbol, DataAccessor.SUBSCRIBE_FIVE_SEC);
                infoPanel = new PercentageChangeSummary(sub);
            }

            // Ensure the symbol is not already in the list
            bool isNew = true;
            List<StockLine> stockList;
            if(!Stocks.TryGetValue(group, out stockList))
            {
                stockList = new List<StockLine>();
                Stocks.Add(group, stockList);
                if(!GroupOrder.Contains(group)) GroupOrder.Add(group);
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
                StockLine newLine = new StockLine(symbol, this, infoPanel);
                newLine.Location = new System.Drawing.Point(5, (int)((Stocks.Count + 0.5) * (newLine.Height + 5)));
                newLine.MouseUp += (sender, e) => { AddStockUi(symbol); };
                stockList.Add(newLine);
                this.Controls.Add(newLine);
                this.Refresh();

                // Request data to fill the summary chart
                newLine.SummaryChart.RequestData(DateTime.Now.Date.AddHours(-12), DateTime.Now.Date.AddHours(16), new TimeSpan(0, 1, 0));
            }
        }

        /// <summary>
        /// Removes a symbol from the list
        /// </summary>
        /// <param name="group">The group the symbol should be added as part of</param>
        /// <param name="symbol">The stock symbol to remove</param>
        public void Remove(string group, string symbol)
        {
            List<StockLine> stockList;
            if(Stocks.TryGetValue(group, out stockList))
            {
                for(int i = 0; i < stockList.Count; i++)
                {
                    if(stockList[i].Name.Equals(symbol))
                    {
                        Remove(stockList[i]);
                        i--;
                    }
                }
            }
        }

        /// <summary>
        /// Removes the specified line from the list
        /// </summary>
        /// <param name="item"></param>
        public void Remove(StockLine item)
        {
            foreach(var stockList in Stocks)
            {
                if(stockList.Value.Contains(item))
                {
                    this.Controls.Remove(item);
                    item.InfoPanel.Destroy();
                    stockList.Value.Remove(item);

                    // Check if the group is now empty
                    if(stockList.Value.Count == 0)
                    {
                        Stocks.Remove(stockList.Key);
                        Controls.Remove(GroupLabels[stockList.Key]);
                        GroupLabels.Remove(stockList.Key);
                    }

                    // Redraw the list
                    Refresh();
                    break;
                }
            }
        }

        /// <summary>
        /// Clears all symbols from the specified group
        /// </summary>
        /// <param name="group"></param>
        public void Clear(string group)
        {
            List<StockLine> stockList;
            if(Stocks.TryGetValue(group, out stockList))
            {
                foreach(StockLine l in stockList) l.InfoPanel.Destroy();
                stockList.Clear();
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
                if(Stocks.ContainsKey(group))
                {
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
            }

            base.Refresh();
        }
    }
}
