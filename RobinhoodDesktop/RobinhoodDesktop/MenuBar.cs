using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace RobinhoodDesktop
{
    public class MenuBar
    {
        public MenuBar(UserConfig config)
        {
            Algorithm = new AlgorithmScreen(config.AlgorithmScreenConfig);

            MenuPanel = new Panel();
            MenuPanel.BackColor = Color.White;
            MenuPanel.Size = new Size(150, 400);

            ToggleButton = new PictureBox();
            ToggleButton.Image = Bitmap.FromFile("Content/GUI/Button_Menu.png");
            ToggleButton.Size = ToggleButton.Image.Size;
            ToggleButton.MouseUp += (sender, e) =>
            {
                if(ToggleButton.Parent.Controls.Contains(MenuPanel))
                {
                    // Hide the menu
                    Hide();
                }
                else
                {
                    // Show the menu
                    Show();
                }
            };
            ToggleButton.LocationChanged += (sender, e) =>
            {
                MenuPanel.Location = new Point(ToggleButton.Location.X, ToggleButton.Location.Y + ToggleButton.Height + 5);
                if(ToggleButton.Parent != null) ToggleButton.Parent.Refresh();
            };

            MenuPanel.Controls.Add(new LogInButton(this));
            MenuPanel.Controls.Add(new ScriptButton(this));
            MenuPanel.Controls.Add(new ScriptSessionButton(this));
            PackMenu();
        }
        #region Types
        public class MenuButton : Panel
        {
            public MenuButton(MenuBar menu, string iconPath)
            {
                this.Menu = menu;

                ButtonImage = new PictureBox();
                ButtonImage.Image = Bitmap.FromFile(iconPath);
                ButtonImage.Size = ButtonImage.Image.Size;
                ButtonImage.Location = new Point(5, 0);
                ButtonImage.MouseUp += (sender, e) => { this.OnMouseUp(e); };
                this.Controls.Add(ButtonImage);
                this.Size = new Size(menu.MenuPanel.Width, ButtonImage.Height);

                this.Paint += MenuButton_draw;
                this.MouseEnter += MenuButton_MouseEnter;
                this.MouseLeave += MenuButton_MouseLeave;
            }
            public PictureBox ButtonImage;
            public string ButtonText;
            public MenuBar Menu;

            protected virtual void MenuButton_draw(object sender, PaintEventArgs e)
            {
                e.Graphics.DrawString(ButtonText, GuiStyle.Font, Brushes.DarkGray, new Point(ButtonImage.Location.X + ButtonImage.Width + 5, 2));
            }

            protected virtual void MenuButton_MouseEnter(object sender, EventArgs e)
            {
                byte r = Menu.MenuPanel.BackColor.R;
                byte g = Menu.MenuPanel.BackColor.G;
                byte b = Menu.MenuPanel.BackColor.B;
                double m = 0.8;

                if((r + g + b) < (128 * 3))
                {
                    // Get brighter
                    this.BackColor = Color.FromArgb(255 - (byte)((255 - r) * m), 255 - (byte)((255 - g) * m), 255 - (byte)((255 - b) * m));
                }
                else
                {
                    // Get darker
                    this.BackColor = Color.FromArgb((byte)(r * m), (byte)(g * m), (byte)(b * m));
                }
            }

            protected virtual void MenuButton_MouseLeave(object sender, EventArgs e)
            {
                this.BackColor = Menu.MenuPanel.BackColor;
            }
        }
        #endregion

        #region Variables
        /// <summary>
        /// The button used to open the menu
        /// </summary>
        public PictureBox ToggleButton;

        /// <summary>
        /// The panel containing the menu options
        /// </summary>
        public Panel MenuPanel;

        /// <summary>
        /// Callback that is executed when the menu is shown
        /// </summary>
        public Action OnShow;

        /// <summary>
        /// The screen displayed to allow the user to log in
        /// </summary>
        public LogInScreen LogIn = new LogInScreen();

        /// <summary>
        /// The screen displayed to allow the user to configure the scripts to run
        /// </summary>
        public AlgorithmScreen Algorithm;
        #endregion

        /// <summary>
        /// Packs the menu based on the current options
        /// </summary>
        private void PackMenu()
        {
            int y = 10;
            foreach(Control c in MenuPanel.Controls)
            {
                c.Location = new Point(c.Location.X, y);
                y += c.Height + 5;
            }
        }

        #region Menu Buttons
        #region Log In
        public class LogInButton : MenuButton
        {
            public LogInButton(MenuBar menu) : base(menu, "Content/GUI/Button_Menu_SignIn.png")
            {
                menu.OnShow += () =>
                {
                    this.ButtonText = (Broker.Instance.IsSignedIn() ? "Log Out" : "Sign In");
                };

                this.MouseUp += (sender, e) =>
                {
                    if(!Broker.Instance.IsSignedIn())
                    {
                        // Show the log in screen, and bring it to the front
                        menu.MenuPanel.Parent.Controls.Add(menu.LogIn.GuiPanel);
                        menu.MenuPanel.Parent.Controls.SetChildIndex(menu.LogIn.GuiPanel, 0);
                        menu.LogIn.GuiPanel.Size = menu.MenuPanel.Parent.Size;
                        menu.Hide();
                    }
                    else
                    {
                        Broker.Instance.SignOut();
                        menu.Hide();
                    }
                };

                // Add logic for when the user interacts with the log-in screen
                menu.LogIn.CancelButton.MouseUp += (sender, e) =>
                {
                    if(menu.LogIn.GuiPanel.Parent != null) menu.LogIn.GuiPanel.Parent.Controls.Remove(menu.LogIn.GuiPanel);
                    menu.Hide();
                };
                menu.LogIn.LogInButton.MouseUp += (sender, e) =>
                {
                    if(Broker.Instance.IsSignedIn())
                    {
                        if(menu.LogIn.GuiPanel.Parent != null) menu.LogIn.GuiPanel.Parent.Controls.Remove(menu.LogIn.GuiPanel);
                        menu.Hide();
                    }
                };
            }
        }
        #endregion

        #region Script
        public class ScriptButton : MenuButton
        {
            public ScriptButton(MenuBar menu) : base(menu, "Content/GUI/Button_Add.png")
            {
                menu.OnShow += () =>
                {
                    this.ButtonText = "Script";
                };

                this.MouseUp += (sender, e) =>
                {
                    // Show the script in screen, and bring it to the front
                    menu.MenuPanel.Parent.Controls.Add(menu.Algorithm.GuiPanel);
                    menu.MenuPanel.Parent.Controls.SetChildIndex(menu.Algorithm.GuiPanel, 0);
                    menu.Algorithm.GuiPanel.Size = menu.MenuPanel.Parent.Size;
                };

                // Add logic for when the user clicks the back button on the script screen
                menu.Algorithm.BackButton.MouseUp += (sender, e) =>
                {
                    menu.MenuPanel.Parent.Controls.Remove(menu.Algorithm.GuiPanel);
                    menu.Hide();
                };
            }
        }

        public class ScriptSessionButton : MenuButton
        {
            public ScriptSessionButton(MenuBar menu) : base(menu, "Content/GUI/Button_Live.png")
            {
                menu.OnShow += () =>
                {
                    this.ButtonText = "Session Script";
                };

                this.MouseUp += (sender, e) =>
                {
                    //new Script.StockSession().RunSession();
                    menu.Hide();
                };
            }
        }
        #endregion
        #endregion

        #region Utility Functions
        /// <summary>
        /// Shows the menu
        /// </summary>
        private void Show()
        {
            OnShow();
            ToggleButton.Parent.Controls.Add(MenuPanel);
            MenuPanel.Parent.Controls.SetChildIndex(MenuPanel, 0);
        }

        /// <summary>
        /// Hides the menu
        /// </summary>
        private void Hide()
        {
            ToggleButton.Parent.Controls.Remove(MenuPanel);
        }
        #endregion
    }
}
