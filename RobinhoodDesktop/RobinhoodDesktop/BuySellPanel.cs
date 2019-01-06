using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace RobinhoodDesktop
{
    class BuySellPanel : Panel
    {
        public BuySellPanel()
        {
            this.BackColor = GuiStyle.BACKGROUND_COLOR;

            // Create the order type selection button
            BuySellLabel = new Label();
            BuySellLabel.Size = new System.Drawing.Size(50, 25);
            BuySellLabel.Location = new System.Drawing.Point(50, 50);
            BuySellLabel.ForeColor = GuiStyle.TEXT_COLOR;
            BuySellLabel.Font = new System.Drawing.Font(GuiStyle.Font.Name, 10, System.Drawing.FontStyle.Bold);
            BuySellLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.Controls.Add(BuySellLabel);
            OrderTypeButton = new GuiButton(OrderNames[(int)SelectedOrderType]);
            OrderTypeButton.Location = new System.Drawing.Point(BuySellLabel.Bounds.Right + 5, 50);
            OrderTypeButton.MouseClick += (sender, e) => {
                OrderTypeSelectionPanel.Visible = !OrderTypeSelectionPanel.Visible;
                if(!OrderTypeSelectionPanel.Visible) RefreshOrderOptionList();
            };
            this.Controls.Add(OrderTypeButton);

            // Create the order type selection list
            OrderTypeSelectionPanel = new Panel();
            OrderTypeSelectionPanel.Location = new System.Drawing.Point(0, OrderTypeButton.Location.Y + OrderTypeButton.Height + 5);
            for(int idx = 0; idx < OrderNames.Length; idx++)
            {
                Broker.Order.OrderType ot = (Broker.Order.OrderType)idx;
                GuiButton orderButton = new GuiButton(OrderNames[idx]);
                orderButton.Location = new System.Drawing.Point(OrderTypeButton.Location.X + 10, (idx * (orderButton.Height + 5)) + 5);
                orderButton.MouseClick += (sender, e) =>
                {
                    SelectedOrderType = ot;
                    RefreshOrderTypeButtons();
                };
                OrderTypeSelectionPanel.Controls.Add(orderButton);
            }
            OrderTypeSelectionPanel.Visible = false;
            RefreshOrderTypeButtons();
            this.Controls.Add(OrderTypeSelectionPanel);

            this.Resize += (sender, e) =>
            {
                OrderTypeSelectionPanel.Width = this.Width;
                OrderTypeSelectionPanel.Height = (this.Height - OrderTypeSelectionPanel.Location.Y);
            };

            // Create the back button
            BackButton = new PictureBox();
            BackButton.Image = System.Drawing.Bitmap.FromFile("Content/GUI/Back.png");
            BackButton.Size = BackButton.Image.Size;
            BackButton.Location = new System.Drawing.Point(10, 10);
            BackButton.MouseClick += (sender, e) => { this.Visible = false; };
            this.Controls.Add(BackButton);

            // Create the info label
            OrderInfoLabel = new Label();
            OrderInfoLabel.Size = new System.Drawing.Size(this.Width, 60);
            OrderInfoLabel.Text = "";
            OrderInfoLabel.ForeColor = GuiStyle.TEXT_COLOR;
            OrderInfoLabel.Visible = false;
            OrderInfoLabel.LocationChanged += (sender, e) =>
            {
                ReviewOrderButton.Location = new System.Drawing.Point(OrderInfoLabel.Location.X, OrderInfoLabel.Bounds.Bottom + 5);
                EditOrderButton.Location = ReviewOrderButton.Location;
                SubmitOrderButton.Location = new System.Drawing.Point(EditOrderButton.Location.X, EditOrderButton.Bounds.Bottom + 5);
            };
            OrderInfoLabel.Location = new System.Drawing.Point(BackButton.Bounds.Right + 5, 220);
            this.Controls.Add(OrderInfoLabel);

            // Create the order review and submit buttons
            ReviewOrderButton.MouseClick += (sender, e) =>
            {
                OrderInfoLabel.Text = GetOrderSummary();
                OrderInfoLabel.Show();
                ReviewOrderButton.Hide();

                EditOrderButton.Show();
                SubmitOrderButton.Text = string.Format("Submit {0}", (BuySell == Broker.Order.BuySellType.BUY) ? "Buy" : "Sell");
                SubmitOrderButton.Show();
            };
            this.Controls.Add(ReviewOrderButton);
            
            EditOrderButton.MouseClick += (sender, e) =>
            {
                OrderInfoLabel.Hide();
                EditOrderButton.Hide();
                SubmitOrderButton.Hide();
                ReviewOrderButton.Show();
            };
            this.Controls.Add(EditOrderButton);

            SubmitOrderButton.MouseClick += (sender, e) =>
            {
                // Submit the order
                Broker.Order newOrder = new Broker.Order()
                {
                    BuySell = BuySell,
                    LimitPrice = GetTradePrice(),
                    Quantity = Shares,
                    Symbol = Symbol,
                    Type = SelectedOrderType,
                };
                decimal.TryParse(StopPriceTextbox.Text, out newOrder.StopPrice);
                Broker.Instance.SubmitOrder(newOrder);

                // Hide the buy/sell menu
                this.Hide();
            };
            this.Controls.Add(SubmitOrderButton);

            // Create the all and half buying power shortcut buttons
            GuiButton halfButton = new GuiButton("Half");
            halfButton.MouseClick += (sender, e) => {
                if(BuySell == Broker.Order.BuySellType.BUY)
                {
                    if(TargetTotalValue == 0) TargetTotalValue = BuyingPower;
                    TargetTotalValue /= 2;
                    TargetShares = 0;
                    UpdateTransaction();
                }
                else
                {
                    if(TargetShares == 0) TargetShares = OwnedShares;
                    TargetShares /= 2;
                    TargetTotalValue = 0;
                    UpdateTransaction();
                }
            };
            this.Controls.Add(halfButton);
            GuiButton allButton = new GuiButton("All");
            allButton.MouseClick += (sender, e) => 
            {
                if(BuySell == Broker.Order.BuySellType.BUY)
                {
                    TargetTotalValue = BuyingPower;
                    TargetShares = 0;
                    UpdateTransaction();
                }
                else
                {
                    TargetShares = OwnedShares;
                    TargetTotalValue = 0;
                    UpdateTransaction();
                }
            };
            this.Controls.Add(allButton);
            TotalTextbox.LocationChanged += (sender, e) =>
            {
                TextBox t = (TextBox)sender;
                halfButton.Location = new System.Drawing.Point(t.Bounds.Right - halfButton.Width, t.Bounds.Bottom + 5);
                allButton.Location = new System.Drawing.Point(halfButton.Location.X - allButton.Width - 5, halfButton.Location.Y);
            };

            // Create the order textboxes
            Tuple<TextBox, string>[] textboxes = new Tuple<TextBox, string>[]
            {
                new Tuple<TextBox, string>(StopPriceTextbox, "Stop Price" ),
                new Tuple<TextBox, string>(LimitPriceTextbox, "Limit Price" ),
                new Tuple<TextBox, string>(SharesTextbox, "Shares" ),
                new Tuple<TextBox, string>(PricePerShareTextbox, "Market Price" ),
                new Tuple<TextBox, string>(TotalTextbox, "Total Value" )
            };
            foreach(Tuple<TextBox, string> t in textboxes)
            {
                Label name = new Label();
                name.Size = new System.Drawing.Size(150, 20);
                name.Location = new System.Drawing.Point(50, 0);
                name.Text = t.Item2;
                name.ForeColor = GuiStyle.TEXT_COLOR;
                name.Font = new System.Drawing.Font(GuiStyle.FONT_NAME, 9, System.Drawing.FontStyle.Bold);
                this.Controls.Add(name);

                t.Item1.Size = new System.Drawing.Size(100, 20);
                t.Item1.LocationChanged += (sender, e) => { name.Location = new System.Drawing.Point(name.Location.X, ((TextBox)sender).Location.Y); };
                t.Item1.VisibleChanged += (sender, e) => { name.Visible = ((TextBox)sender).Visible; };
                t.Item1.Location = new System.Drawing.Point(name.Bounds.Right, 0);
                t.Item1.ForeColor = GuiStyle.TEXT_COLOR;
                t.Item1.BackColor = GuiStyle.DARK_GREY;

                this.Controls.Add(t.Item1);
            }
            RefreshOrderOptionList();

            // Customization of textboxes
            PricePerShareTextbox.BorderStyle = BorderStyle.None;
            PricePerShareTextbox.BackColor = GuiStyle.BACKGROUND_COLOR;
            PricePerShareTextbox.ForeColor = GuiStyle.PRICE_COLOR_POSITIVE;
            PricePerShareTextbox.ReadOnly = true;
            SharesTextbox.KeyDown += (sender, e) =>
            {
                if((e.KeyCode == Keys.Enter) || (e.KeyCode == Keys.Return))
                {
                    if(decimal.TryParse(((TextBox)sender).Text, out TargetShares))
                    {
                        // Remove focus from the textbox so that it will be updated
                        ((TextBox)sender).Parent.Focus();   
                        UpdateTransaction();
                    }
                    TargetTotalValue = 0;
                }
            };
            TotalTextbox.KeyDown += (sender, e) =>
            {
                if((e.KeyCode == Keys.Enter) || (e.KeyCode == Keys.Return))
                {
                    decimal val;
                    if(decimal.TryParse(TotalTextbox.Text.Replace("$", "").Replace(",", ""), out val))
                    {
                        ((TextBox)sender).Parent.Focus();
                        decimal price = GetTradePrice();
                        if(price != 0) Shares = Math.Floor(val / price);
                        TargetTotalValue = val;
                    }
                    TargetShares = 0;
                }
            };
            LimitPriceTextbox.KeyDown += (sender, e) =>
            {
                if((e.KeyCode == Keys.Enter) || (e.KeyCode == Keys.Return))
                {
                    UpdateTransaction();
                }
            };

            
        }

        #region Constants
        /// <summary>
        /// Display-able names for the order types.
        /// </summary>
        public readonly string[] OrderNames = new string[]
        {
            "Market",       // OrderType.MARKET
            "Limit",        // OrderType.LIMIT
            "Stop",         // OrderType.STOP_LOSS
            "Stop Limit",   // OrderType.STOP_LIMIT
        };
        #endregion

        #region Variables
        /// <summary>
        /// The stock symbol the order is being made for
        /// </summary>
        public string Symbol;

        /// <summary>
        /// The selected buy/sell order type
        /// </summary>
        public Broker.Order.BuySellType BuySell;

        /// <summary>
        /// A label containing some information about the order.
        /// </summary>
        public Label OrderInfoLabel;

        /// <summary>
        /// A label to show the buy or sell selection
        /// </summary>
        public Label BuySellLabel;

        /// <summary>
        /// A panel containing the order type selection interface
        /// </summary>
        public Panel OrderTypeSelectionPanel;

        /// <summary>
        /// Button that opens the menu to select the order type
        /// </summary>
        public GuiButton OrderTypeButton;

        /// <summary>
        /// Button used to exit the menu
        /// </summary>
        public PictureBox BackButton;

        /// <summary>
        /// Textbox used to set the stop price for the order.
        /// </summary>
        public TextBox StopPriceTextbox = new TextBox();

        /// <summary>
        /// Textbox used to set the limit price for the order.
        /// </summary>
        public TextBox LimitPriceTextbox = new TextBox();

        /// <summary>
        /// Textbox used to display/set the number of shares being transacted in the order.
        /// </summary>
        public TextBox SharesTextbox = new TextBox();

        /// <summary>
        /// Textbox used to display/set the price per share for the order
        /// </summary>
        public TextBox PricePerShareTextbox = new TextBox();

        /// <summary>
        /// Textbox used to display/set the total amount of money involved in the order.
        /// </summary>
        public TextBox TotalTextbox = new TextBox();

        /// <summary>
        /// Reviews the order to confirm before it is executed
        /// </summary>
        public GuiButton ReviewOrderButton = new GuiButton("Review");

        /// <summary>
        /// Returns to the order setup page
        /// </summary>
        public GuiButton EditOrderButton = new GuiButton("Edit");

        /// <summary>
        /// Submits the order to be executed
        /// </summary>
        public GuiButton SubmitOrderButton = new GuiButton("Submit");

        /// <summary>
        /// The currently selected order type
        /// </summary>
        public Broker.Order.OrderType SelectedOrderType;

        /// <summary>
        /// Accesses the current price of the stock being bought or sold
        /// </summary>
        public DataAccessor.Subscription MarketPrice;

        /// <summary>
        /// The currently available buying power
        /// </summary>
        private decimal BuyingPower;

        /// <summary>
        /// The number of shares currently owned
        /// </summary>
        private decimal OwnedShares;

        /// <summary>
        /// The number of shares selected for this trasaction
        /// </summary>
        private decimal Shares
        {
            set
            {
                // Ensure the value is valid
                if(BuySell == Broker.Order.BuySellType.BUY)
                {
                    orderShares = Math.Max(0, Math.Min(value, GetMaxBuyShares()));
                }
                else
                {
                    orderShares = Math.Max(0, Math.Min(value, OwnedShares));
                }

                if(!SharesTextbox.Focused) SharesTextbox.Text = string.Format("{0}", orderShares);
                if(!TotalTextbox.Focused) TotalTextbox.Text = string.Format("{0:c}", (Shares * GetTradePrice()));
            }
            get { return orderShares; }
        }
        private decimal orderShares;

        /// <summary>
        /// The target number of shares the user would like to trasact in the order
        /// </summary>
        private decimal TargetShares;

        /// <summary>
        /// The target total dollar amount the user would like to transact in the order
        /// </summary>
        private decimal TargetTotalValue;
        #endregion

        /// <summary>
        /// Brings up the order menu
        /// </summary>
        /// <param name="symbol">The stock the order is for</param>
        /// <param name="buySell">Indicates if the order is a buy or sell</param>
        public void ShowOrderMenu(string symbol, Broker.Order.BuySellType buySell)
        {
            this.Symbol = symbol;
            this.BuySell = buySell;
            this.BuyingPower = 0;

            DataAccessor.Search(symbol, (Dictionary<string, string> results) => {
                string stockName;
                if(results.TryGetValue(Symbol, out stockName))
                {
                    this.BeginInvoke((Action)(() =>
                    {
                        BuySellLabel.Text = ((buySell == Broker.Order.BuySellType.BUY) ? "Buy" : "Sell");
                        OrderInfoLabel.Text = string.Format("{0} Order: {1}\n{2}", (buySell == Broker.Order.BuySellType.BUY) ? "Buy" : "Sell", Symbol, stockName);
                        this.Visible = true;
                    }));
                }
            });

            Broker.Instance.GetAccountInfo((account) => { this.BuyingPower = account.BuyingPower; });

            MarketPrice = DataAccessor.Subscribe(symbol, DataAccessor.SUBSCRIBE_ONE_SEC);
            MarketPrice.Notify += (DataAccessor.Subscription s) =>
            {
                this.BeginInvoke((Action)(() =>
                {
                    PricePerShareTextbox.Text = string.Format("{0:c}", s.Price);
                    UpdateTransaction();
                }));
            };

            // Select the default order type
            SelectedOrderType = Broker.Order.OrderType.MARKET;
            RefreshOrderTypeButtons();
            RefreshOrderOptionList();

            // Select the default number of shares for the order
            Broker.Instance.GetPositions().TryGetValue(Symbol, out OwnedShares);
            Shares = ((BuySell == Broker.Order.BuySellType.SELL) ? OwnedShares : 0);

            // Clear the target values initially
            TargetShares = 0;
            TargetTotalValue = 0;

            // Ensure the order review menu is closed
            ReviewOrderButton.Show();
            OrderInfoLabel.Hide();
            EditOrderButton.Hide();
            SubmitOrderButton.Hide();
        }

        /// <summary>
        /// Refreshes the order type button list to indicate which one is selected
        /// </summary>
        private void RefreshOrderTypeButtons()
        {
            foreach(GuiButton b in OrderTypeSelectionPanel.Controls)
            {
                b.SetImage((Array.IndexOf(OrderNames, b.Text) == (int)SelectedOrderType) ? GuiButton.ButtonImage.GREEN_WHITE : GuiButton.ButtonImage.GREEN_TRANSPARENT);
            }
            if(OrderTypeButton != null)
            {
                OrderTypeButton.Text = OrderNames[(int)SelectedOrderType];
                OrderTypeButton.Refresh();
            }
        }

        /// <summary>
        /// Displays the options corresponding to the current order type.
        /// </summary>
        private void RefreshOrderOptionList()
        {
            int pos = OrderTypeButton.Location.Y + OrderTypeButton.Height + 50;

            Tuple<TextBox, bool>[] textboxes = new Tuple<TextBox, bool>[]
            {
                new Tuple<TextBox, bool>(StopPriceTextbox, ((SelectedOrderType == Broker.Order.OrderType.STOP) || (SelectedOrderType == Broker.Order.OrderType.STOP_LIMIT))),
                new Tuple<TextBox, bool>(LimitPriceTextbox, ((SelectedOrderType == Broker.Order.OrderType.STOP_LIMIT) || (SelectedOrderType == Broker.Order.OrderType.LIMIT))),
                new Tuple<TextBox, bool>(SharesTextbox, true),
                new Tuple<TextBox, bool>(PricePerShareTextbox, true),
                new Tuple<TextBox, bool>(TotalTextbox, true)
            };
            foreach(Tuple<TextBox, bool> t in textboxes)
            {
                t.Item1.Visible = t.Item2;
                if(t.Item2)
                {
                    t.Item1.Location = new System.Drawing.Point(t.Item1.Location.X, pos);
                    pos += t.Item1.Height + 5;
                }
            }

            // Update the position of the review nad submit buttons
            OrderInfoLabel.Location = new System.Drawing.Point(OrderInfoLabel.Location.X, textboxes[textboxes.Length - 1].Item1.Bounds.Bottom + 50);

            // Update the maximum buy amount and number of shares

        }

        /// <summary>
        /// Updates the transaction because something changed
        /// </summary>
        private void UpdateTransaction()
        {
            if(TargetTotalValue != 0)
            {
                decimal price = GetTradePrice();
                if(price != 0) Shares = Math.Floor(TargetTotalValue / price);
            }
            else if(TargetShares != 0) Shares = TargetShares;

            OrderInfoLabel.Text = GetOrderSummary();
        }

        /// <summary>
        /// Determines the maximum number of shares that could be purchaged given the current price and buying power
        /// </summary>
        /// <returns>The maximum number of shares</returns>
        private decimal GetMaxBuyShares()
        {
            decimal effectiveBuyingPower = BuyingPower;

            if((SelectedOrderType == Broker.Order.OrderType.MARKET) || (SelectedOrderType == Broker.Order.OrderType.STOP))
            {
                // Allow a 5% buffer for market orders
                effectiveBuyingPower *= 0.945M;
            }

            decimal price = GetTradePrice();
            return (price != 0) ? Math.Floor(effectiveBuyingPower / price) : 0;
        }

        /// <summary>
        /// Determines how much money it would take to buy the maximum number of shares
        /// </summary>
        private decimal GetMaxBuyAmount()
        {
            return (GetMaxBuyShares() * GetTradePrice());
        }

        /// <summary>
        /// Determines what price the trade will be executed at
        /// </summary>
        /// <returns>The trade execution price</returns>
        private decimal GetTradePrice()
        {
            decimal price = MarketPrice.Price;

            if((SelectedOrderType == Broker.Order.OrderType.LIMIT) || (SelectedOrderType == Broker.Order.OrderType.STOP_LIMIT))
            {
                decimal.TryParse(LimitPriceTextbox.Text.Replace("$", "").Replace(",", ""), out price);
            }

            return price;
        }

        /// <summary>
        /// Generates a string summarizing the order
        /// </summary>
        /// <returns>The order summary string</returns>
        private string GetOrderSummary()
        {
            bool buy = (BuySell == Broker.Order.BuySellType.BUY);
            string condition_str = "INVALID";
            decimal stopPrice = 0;
            decimal.TryParse(StopPriceTextbox.Text, out stopPrice);
            if(SelectedOrderType == Broker.Order.OrderType.MARKET) condition_str = string.Format("at the current market price of around {0:c}", GetTradePrice());
            else if(SelectedOrderType == Broker.Order.OrderType.LIMIT) condition_str = string.Format("once the price {0} {1:c}", buy ? "drops beneath" : "rises above", GetTradePrice());
            else if(SelectedOrderType == Broker.Order.OrderType.STOP) condition_str = string.Format("at the current market price if that price {0} {1:c}", buy ? "rises above" : "drops beneath", stopPrice);
            else if(SelectedOrderType == Broker.Order.OrderType.STOP_LIMIT) condition_str = string.Format("at a price no {0} than {1:c} if the market price {2} {3:c}", buy ? "higher" : "lower", GetTradePrice(), (buy ? "rises above" : "drops beneath"), stopPrice);

            return string.Format("Submit a {0} order of {1} shares to be executed {2}.", (buy ? "BUY" : "SELL"), Shares, condition_str);
        }
    }
}
