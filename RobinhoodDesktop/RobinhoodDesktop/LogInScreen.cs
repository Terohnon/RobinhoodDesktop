using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobinhoodDesktop
{
    public class LogInScreen
    {
        public LogInScreen()
        {
            GuiPanel = new Panel();

            GuiBox = new Panel();
            GuiBox.AutoSize = true;
            GuiBox.BackColor = GuiStyle.BACKGROUND_COLOR;
            Username = new TextBox();
            Username.Size = new System.Drawing.Size(200, 15);
            Username.Location = new System.Drawing.Point(10, 40);
            GuiBox.Controls.Add(Username);

            Password = new TextBox();
            Password.UseSystemPasswordChar = true;
            Password.Size = Username.Size;
            Password.Location = new System.Drawing.Point(Username.Location.X, Username.Location.Y + 40);
            GuiBox.Controls.Add(Password);

            RememberLogIn = new CheckBox();
            RememberLogIn.Location = new System.Drawing.Point(Password.Location.X + 10, Password.Location.Y + 25);
            RememberLogIn.Size = new Size(200, 20);
            RememberLogIn.Text = "Remember log-in";
            RememberLogIn.Checked = false;
            RememberLogIn.Font = GuiStyle.Font;
            RememberLogIn.ForeColor = GuiStyle.PRICE_COLOR_POSITIVE;
            RememberLogIn.TextAlign = ContentAlignment.BottomLeft;
            GuiBox.Controls.Add(RememberLogIn);

            LogInButton = new GuiButton("Log In");
            LogInButton.Location = new System.Drawing.Point((Password.Location.X + Password.Width) - LogInButton.Width, RememberLogIn.Location.Y + 30);
            LogInButton.MouseUp += LogInButton_MouseUp;
            GuiBox.Controls.Add(LogInButton);

            CancelButton = new GuiButton("Cancel");
            CancelButton.Location = new System.Drawing.Point(Password.Location.X, LogInButton.Location.Y);
            GuiBox.Controls.Add(CancelButton);

            GuiBox.Paint += (sender, e) =>
            {
                SolidBrush brush = new SolidBrush(GuiStyle.PRICE_COLOR_POSITIVE);
                SolidBrush errorBrush = new SolidBrush(GuiStyle.PRICE_COLOR_NEGATIVE);
                e.Graphics.DrawString("Username", GuiStyle.Font, brush, new Point(Username.Location.X, Username.Location.Y - 20), StringFormat.GenericDefault);
                e.Graphics.DrawString("Password", GuiStyle.Font, brush, new Point(Password.Location.X, Password.Location.Y - 20), StringFormat.GenericDefault);
                e.Graphics.DrawString(ErrorText, GuiStyle.Font, errorBrush, new Point(Username.Location.X, Username.Location.Y - 40), StringFormat.GenericDefault);
            };

            GuiPanel.BackColor = GuiStyle.BACKGROUND_COLOR;
            GuiPanel.Controls.Add(GuiBox);
            GuiPanel.Resize += (sender, e) =>
            {
                GuiBox.Location = new System.Drawing.Point((GuiPanel.Width / 2) - (GuiBox.Width / 2), (GuiPanel.Height / 2) - (GuiBox.Height / 2));
            };
        }

        #region Variables
        /// <summary>
        /// The background panel for the login screen
        /// </summary>
        public Panel GuiPanel;

        /// <summary>
        /// A box containing the main GUI elements (to make them easier to re-position)
        /// </summary>
        private Panel GuiBox;

        /// <summary>
        /// The textbox used to input the username
        /// </summary>
        public TextBox Username;

        /// <summary>
        /// The textbox used to input the password
        /// </summary>
        public TextBox Password;

        /// <summary>
        /// Checkbox used to indicate if the log-in information should be saved
        /// </summary>
        public CheckBox RememberLogIn;

        /// <summary>
        /// The button clicked to perform the sign in proceedure
        /// </summary>
        public GuiButton LogInButton;

        /// <summary>
        /// The button clicked to cancel signing in
        /// </summary>
        public GuiButton CancelButton;

        /// <summary>
        /// Text that is displayed to indicate an error signing in
        /// </summary>
        private string ErrorText = "";
        #endregion

        /// <summary>
        /// Function that performs the log-in operation when the button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogInButton_MouseUp(object sender, EventArgs e)
        {
            Broker.Instance.SignIn(Username.Text, Password.Text);
            Password.Text = "";
            if(!Broker.Instance.IsSignedIn())
            {
                ErrorText = "Error: Invalid username or password";
                GuiBox.Refresh();
            }
        }
    }
}
