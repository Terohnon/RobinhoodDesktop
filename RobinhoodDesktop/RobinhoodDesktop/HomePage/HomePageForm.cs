using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobinhoodDesktop.HomePage
{
    public partial class HomePageForm : Form
    {
        public HomePageForm()
        {
            InitializeComponent();

            //HomePage.AccountSummaryChart accountChart = new HomePage.AccountSummaryChart();
            //accountChart.Size = new Size(this.Width - 20, this.Height - 20);
            //this.Controls.Add(accountChart);

            StockChart plot = new StockChart();
            plot.SetChartData(GenerateExampleData());
            this.Controls.Add(plot.Canvas);
        }

        private static System.Data.DataTable GenerateExampleData()
        {

            // Generate data for the price line
            float price = 10;
            float min = price;
            float max = price;
            DateTime start = new DateTime(2017, 1, 2, 9, 30, 0);
            Random rand = new Random();
            System.Data.DataTable dt = new System.Data.DataTable();
            dt.Columns.Add("Time", typeof(DateTime));
            dt.Columns.Add("Price", typeof(float));
            for(int i = 0; i < 20 * 390; i++)
            {
                price += (float)((rand.NextDouble() * 0.1) - 0.05);
                dt.Rows.Add(start.AddDays(i / 390).AddMinutes(i % 390), price);
            }

            return dt;
        }
    }
}
