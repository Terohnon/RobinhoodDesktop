using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BasicallyMe.RobinhoodNet;

namespace RobinhoodDesktop.HomePage
{
    public partial class HomePageForm : Form
    {
        public HomePageForm()
        {
            InitializeComponent();
            this.FormClosing += HomePageForm_FormClosing;

            //HomePage.AccountSummaryChart accountChart = new HomePage.AccountSummaryChart();
            //accountChart.Size = new Size(this.Width - 20, this.Height - 20);
            //this.Controls.Add(accountChart);

            Plot = new StockChart();
            //Plot.SetChartData(GenerateExampleData());
            this.Controls.Add(Plot.Canvas);

            this.ResizeEnd += HomePageForm_ResizeEnd;
            HomePageForm_ResizeEnd(this, EventArgs.Empty);

            Robinhood = new RobinhoodInterface();
            DataAccessor.SetAccessor(Robinhood);

            // Create the search box
            SearchHome = new SearchList();
            SearchHome.Size = new Size(300, 300);
            SearchHome.Location = new Point(20, 320);
            Controls.Add(SearchHome);

            // Add test stock symbols to the list
#if false
            StockListHome = new StockList();
            StockListHome.Location = new Point(SearchHome.Location.X, SearchHome.Location.Y + 400);
            StockListHome.Size = new Size(300, 300);
            StockListHome.Add("AMD");
            StockListHome.Add("NVDA");
            StockListHome.Add("ON");
            StockListHome.Add("MU");
            StockListHome.Add("GNTX");
            StockListHome.Add("XLNX");
            StockListHome.Add("TSLA");
            StockListHome.Add("FL");
            StockListHome.Add("FINL");
            StockListHome.Add("VRA");
            Controls.Add(StockListHome);
#endif
        }

        #region Variables
        public StockChart Plot;
        public SearchList SearchHome;
        public StockList StockListHome;
        public RobinhoodInterface Robinhood;
        #endregion

        private static System.Data.DataTable GenerateExampleData()
        {
            System.Data.DataTable dt = new System.Data.DataTable();
            dt.Columns.Add("Time", typeof(DateTime));
            dt.Columns.Add("Price", typeof(float));

            try
            {
                var rh = new RobinhoodClient();
                var history = rh.DownloadHistory("AMD", "5minute", "week").Result;

                foreach(var p in history.HistoricalInfo)
                {
                    dt.Rows.Add(p.BeginsAt.ToLocalTime(), (float)p.OpenPrice);
                }
            }
            catch(Exception ex)
            {
                Environment.Exit(1);
            }

            return dt;
        }

        private void HomePageForm_ResizeEnd(object sender, System.EventArgs e)
        {
            Plot.Canvas.Size = new Size(this.Width - 20, 300);
            //StockListHome.Size = new Size(StockListHome.Width, ((Height - StockListHome.Location.Y) - 40));
        }

        private void HomePageForm_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Robinhood.Close();
        }
    }
}
