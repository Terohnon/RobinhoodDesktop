using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobinhoodDesktop
{
    public class AlgorithmScreen
    {
        public AlgorithmScreen()
        {
            Background = new Panel();

            GuiBox = new Panel();
            GuiBox.AutoSize = true;
            GuiBox.BackColor = GuiStyle.BACKGROUND_COLOR;


            Background.BackColor = GuiStyle.BACKGROUND_COLOR;
            Background.Controls.Add(GuiBox);
            Background.Resize += (sender, e) =>
            {
                GuiBox.Location = new System.Drawing.Point((Background.Width / 2) - (GuiBox.Width / 2), (Background.Height / 2) - (GuiBox.Height / 2));
            };
        }

        #region Variables
            /// <summary>
            /// The background panel for the login screen
            /// </summary>
        public Panel Background;

        /// <summary>
        /// A box containing the main GUI elements (to make them easier to re-position)
        /// </summary>
        private Panel GuiBox;
        #endregion
    }
}
