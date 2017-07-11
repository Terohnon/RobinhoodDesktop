using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobinhoodDesktop
{
    public class SearchList : Panel
    {

        public SearchList()
        {
            SearchboxText = new TextBox();
            SearchboxText.Size = new System.Drawing.Size(this.Width - 20, 15);
            SearchboxText.Location = new System.Drawing.Point(10, 5);
            SearchboxText.TextChanged += SearchboxText_HandleKeypress;
            this.Controls.Add(SearchboxText);

            this.BackColor = System.Drawing.Color.FromArgb(90, 10, 10, 10);
            this.AutoScroll = true;
            this.Resize += OnResize;
        }

        #region Types
        /// <summary>
        /// Callback function to add a symbol to a watchlist
        /// </summary>
        /// <param name="symbol">The stock symbol to add</param>
        public delegate void AddToWatlistCallback(string symbol);

        public class StockListItem : Panel
        {
            /// <summary>
            /// The label representing the stock
            /// </summary>
            public Label SymbolLabel;

            /// <summary>
            /// The label respresenting the full stock name
            /// </summary>
            public Label StockLabel;

            /// <summary>
            /// The label representing the current stock price
            /// </summary>
            public Label PriceLabel;

            /// <summary>
            /// The button used to add the stock to the watchlist
            /// </summary>
            public PictureBox AddButton;

            public StockListItem(SearchList master, string symbol, string name)
            {
                this.SymbolLabel = new Label();
                this.SymbolLabel.Size = new System.Drawing.Size(50, 15);
                this.SymbolLabel.Location = new System.Drawing.Point(5, 5);
                this.SymbolLabel.Text = symbol;
                Controls.Add(SymbolLabel);

                this.StockLabel = new Label();
                this.StockLabel.Size = new System.Drawing.Size(150, 15);
                this.StockLabel.Location = new System.Drawing.Point(this.SymbolLabel.Location.X + this.SymbolLabel.Width + 5, this.SymbolLabel.Location.Y);
                this.StockLabel.Text = name;
                Controls.Add(StockLabel);

                this.AddButton = new PictureBox();
                this.AddButton.Location = new System.Drawing.Point(this.StockLabel.Location.X + this.StockLabel.Width + 5, this.StockLabel.Location.Y);
                this.AddButton.Image = System.Drawing.Bitmap.FromFile("Content/GUI/Button_Add.png");
                this.AddButton.Size = AddButton.Image.Size;
                this.AddButton.BackColor = System.Drawing.Color.Transparent;
                this.AddButton.MouseUp += (object sender, MouseEventArgs e) =>
                {
                    master.AddToWatchlist(symbol);
                    master.ClearSearchResults();
                };
                Controls.Add(this.AddButton);

                this.Size = new System.Drawing.Size(this.AddButton.Location.X + this.AddButton.Width + 5, this.AddButton.Location.Y + this.AddButton.Height);
            }
        }
        #endregion

        #region Variables
        /// <summary>
        /// The textbox used for search input
        /// </summary>
        public TextBox SearchboxText;

        /// <summary>
        /// Callback function to add a stock symbol to a watchlist
        /// </summary>
        public AddToWatlistCallback AddToWatchlist;

        /// <summary>
        /// The current search results
        /// </summary>
        private List<StockListItem> SearchResults = new List<StockListItem>();
        #endregion

        /// <summary>
        /// Performs a search on symbols close to the specified string
        /// </summary>
        /// <param name="symbol"></param>
        public void Search(string symbol)
        {
            DataAccessor.Search(symbol, (Dictionary<string, string> r) => 
                {
                    this.BeginInvoke((Action<Dictionary<string, string>>)((Dictionary<string, string> results) =>
                    {
                        this.SuspendLayout();
                        ClearSearchResults();
                        int yPos = 35;
                        foreach(var pair in results)
                        {
                            StockListItem item = new StockListItem(this, pair.Key, pair.Value);
                            item.Location = new System.Drawing.Point(5, yPos);
                            Controls.Add(item);

                            yPos += 35;
                        }
                        this.ResumeLayout();
                    }), new object[] { r });
                    
                });
        }

        /// <summary>
        /// Clears any previous search results
        /// </summary>
        private void ClearSearchResults()
        {
            List<Control> savedControls = new List<Control>()
            {
                SearchboxText
            };

            // Remove all but the saved controls
            for(int i = 0; i < Controls.Count; i++)
            {
                if(!savedControls.Contains(Controls[i]))
                {
                    Controls.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Handles a keypress in the searchbox
        /// </summary>
        /// <param name="sender">The searchbox object</param>
        /// <param name="e">Information on the keys that are pressed</param>
        private void SearchboxText_HandleKeypress(Object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(SearchboxText.Text))
            {
                Search(SearchboxText.Text);
            }
            else
            {
                ClearSearchResults();
            }
        }

        /// <summary>
        /// Callback that is executed when the control is resized
        /// <param name="sender">The parent object</param>
        /// <param name="e">The event arguments</param>
        /// </summary>
        public void OnResize(object sender, EventArgs e)
        {
            SearchboxText.Size = new System.Drawing.Size(this.Size.Width - (10 + 20), SearchboxText.Height);
        }
    }
}
