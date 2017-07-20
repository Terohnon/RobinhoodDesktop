using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobinhoodDesktop
{
    public class StockUI
    {
        public StockUI(string symbol)
        {
            this.Symbol = symbol;

            this.Canvas = new Panel();
            this.Chart = new StockChart();
            Canvas.Controls.Add(Chart.Canvas);

            Canvas.Resize += Canvas_Resize;
        }
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
        /// The button used to initiate a stock buy
        /// </summary>
        public PictureBox BuyButton;

        /// <summary>
        /// The button used to initiate a stock sell
        /// </summary>
        public PictureBox SellButton;
        #endregion

        /// <summary>
        /// Callback that is executed when the UI is resized
        /// </summary>
        /// <param name="sender">The UI object</param>
        /// <param name="e">Additional parameters</param>
        private void Canvas_Resize(object sender, EventArgs e)
        {
            Chart.Canvas.Size = Canvas.Size;
        }
    }
}
