using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobinhoodDesktop
{
    public class StockChartPanel : Panel
    {
        public StockChartPanel(string symbol)
        {
            this.Symbol = symbol;
            this.Resize += StockChartPanel_Resize;

            // Create the summary bar
            SummaryBarPanel = new Panel();
            SummaryBarPanel.Location = new System.Drawing.Point(0, 0);
            SummaryBarPanel.Size = new System.Drawing.Size(600, 25);
            SummaryBarPanel.BackColor = GuiStyle.BACKGROUND_COLOR;

            BuyButton = new GuiButton("Buy");
            BuyButton.Location = new System.Drawing.Point(0, 5);
            SummaryBarPanel.Controls.Add(BuyButton);

            SellButton = new GuiButton("Sell");
            SellButton.Location = new System.Drawing.Point(0, BuyButton.Location.Y);
            SummaryBarPanel.Controls.Add(SellButton);

            SummaryLabel = new Label();
            SummaryLabel.ForeColor = GuiStyle.TEXT_COLOR;
            SummaryLabel.Size = new System.Drawing.Size(200, 20);
            SummaryLabel.BackColor = System.Drawing.Color.Transparent;
            SummaryLabel.Location = new System.Drawing.Point(10, 5);
            SummaryBarPanel.Controls.Add(SummaryLabel);

            CollapseButton = new PictureBox();
            CollapseButton.Image = System.Drawing.Bitmap.FromFile("Content/GUI/Button_Collapse.png");
            CollapseButton.BackColor = System.Drawing.Color.Transparent;
            CollapseButton.Size = CollapseButton.Image.Size;
            CollapseButton.Location = new System.Drawing.Point(SummaryLabel.Location.X + SummaryLabel.Width, 5);
            CollapseButton.MouseUp += CollapseButton_MouseUp;
            SummaryBarPanel.Controls.Add(CollapseButton);

            CloseButton = new PictureBox();
            CloseButton.Image = System.Drawing.Bitmap.FromFile("Content/GUI/Button_Close.png");
            CloseButton.BackColor = System.Drawing.Color.Transparent;
            CloseButton.Size = CollapseButton.Image.Size;
            CloseButton.Location = new System.Drawing.Point(CollapseButton.Location.X + CollapseButton.Width, 5);
            SummaryBarPanel.Controls.Add(CloseButton);
            this.Controls.Add(SummaryBarPanel);

            // Prevent scroll wheel when not over the scroll bar itself
            this.MouseWheel += (object sender, MouseEventArgs e) =>
            {
                if(e.X < (this.Right - 15))
                {
                    ((HandledMouseEventArgs)e).Handled = true;
                }

            };

            this.Canvas = new Panel();
            this.Chart = new StockChart(symbol);
            Canvas.Controls.Add(Chart.Canvas);

            // Request data to fill the stock chart
            Chart.RequestData(DateTime.Now.Date.AddHours(-48), DateTime.Now.Date.AddHours(16), new TimeSpan(0, 1, 0));
            Chart.SetSubscritpion(DataAccessor.Subscribe(Chart.Symbol, DataAccessor.SUBSCRIBE_FIVE_SEC));

            Canvas.Resize += (sender, e) => { Chart.Canvas.Size = Canvas.Size; };
            Canvas.Location = new System.Drawing.Point(SummaryBarPanel.Location.X - 6, SummaryBarPanel.Location.Y + SummaryBarPanel.Height);
            this.Controls.Add(Canvas);
        }


        #region Types
        public struct StockUIConfig
        {
            public string Symbol;
            public int Height;

            public StockUIConfig(StockChartPanel ui)
            {
                this.Symbol = ui.Symbol;
                this.Height = ui.Canvas.Height;
            }
        }
        #endregion

        #region Variables
        /// <summary>
        /// The stock symbol associated with this UI
        /// </summary>
        public string Symbol;

        /// <summary>
        /// The panel used to contain all of the UI's elements
        /// </summary>
        public Panel Canvas;

        /// <summary>
        /// The chart used to plot the stock's price
        /// </summary>
        public StockChart Chart;

        /// <summary>
        /// Label used to provide a summary of the information in the panel
        /// </summary>
        public Label SummaryLabel;

        /// <summary>
        /// The button used to initiate a buy
        /// </summary>
        public GuiButton BuyButton;

        /// <summary>
        /// The button used to initiate a sell
        /// </summary>
        public GuiButton SellButton;

        /// <summary>
        /// The button used to expand/collapse the panel
        /// </summary>
        public PictureBox CollapseButton;

        /// <summary>
        /// The button used to close the panel
        /// </summary>
        public PictureBox CloseButton;

        /// <summary>
        /// The panel comprising the summary bar
        /// </summary>
        private Panel SummaryBarPanel;
        #endregion

        /// <summary>
        ///  Updates the textbox providing the summary of the panel
        ///  <param name="summary">The summary text to set</param>
        /// </summary>
        public void UpdateSummaryText(string summary)
        {
            SummaryLabel.Text = summary;
        }

        /// <summary>
        /// Returns a new object containing the configuration
        /// </summary>
        /// <returns>A configuration representing the state of the object</returns>
        public StockUIConfig SaveConfig()
        {
            return new StockUIConfig(this);
        }

        /// <summary>
        /// Creates a stock UI object from a configuration
        /// </summary>
        /// <param name="cfg">The configuration</param>
        /// <returns>The corresponding stock UI object</returns>
        public static StockChartPanel LoadConfig(StockUIConfig cfg)
        {
            StockChartPanel newUi = new StockChartPanel(cfg.Symbol);
            newUi.Canvas.Size = new System.Drawing.Size(newUi.Canvas.Width, cfg.Height);

            return newUi;
        }

        /// <summary>
        /// Handles the collapse action
        /// </summary>
        /// <param name="sender">The PictureBox object</param>
        /// <param name="e">The mouse information</param>
        private void CollapseButton_MouseUp(object sender, MouseEventArgs e)
        {
            if(Controls.Contains(Canvas))
            {
                Controls.Remove(Canvas);
                CollapseButton.Image.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
                CollapseButton.Refresh();
                this.Size = SummaryBarPanel.Size;
            }
            else
            {
                Controls.Add(Canvas);
                CollapseButton.Image.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
                CollapseButton.Refresh();
                this.Size = new System.Drawing.Size(SummaryBarPanel.Width, Canvas.Location.Y + Canvas.Height);
            }
        }

        /// <summary>
        /// Callback executed when this object is resized
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StockChartPanel_Resize(object sender, System.EventArgs e)
        {
            SummaryBarPanel.Size = new System.Drawing.Size(this.Width, SummaryBarPanel.Height);
            CloseButton.Location = new System.Drawing.Point((SummaryBarPanel.Width - CloseButton.Width) - 10, CloseButton.Location.Y);
            CollapseButton.Location = new System.Drawing.Point((CloseButton.Location.X - CollapseButton.Width) - 5, CollapseButton.Location.Y);
            SellButton.Location = new System.Drawing.Point((CollapseButton.Location.X - SellButton.Width) - 5, SellButton.Location.Y);
            BuyButton.Location = new System.Drawing.Point((SellButton.Location.X - BuyButton.Width) - 5, BuyButton.Location.Y);
            if(Controls.Contains(Canvas))
            {
                Canvas.Size = new System.Drawing.Size(this.Width + 11, (this.Height - SummaryBarPanel.Height - 5));
            }
        }
    }
}
