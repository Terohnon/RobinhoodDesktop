using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop
{
    public class GuiStyle
    {
        #region Constants
        /// <summary>
        /// The color of the price information when it is positive
        /// </summary>
        public static readonly System.Drawing.Color PRICE_COLOR_POSITIVE = System.Drawing.Color.FromArgb(255, 0, 173, 145);

        /// <summary>
        /// The color of the price information when it is negative
        /// </summary>
        public static readonly System.Drawing.Color PRICE_COLOR_NEGATIVE = System.Drawing.Color.FromArgb(255, 173, 45, 25);

        /// <summary>
        /// The color of text
        /// </summary>
        public static readonly System.Drawing.Color TEXT_COLOR = System.Drawing.Color.FromArgb(255, 255, 255, 255);

        /// <summary>
        /// The color of guidelines and text
        /// </summary>
        public static readonly System.Drawing.Color GUIDE_COLOR = System.Drawing.Color.FromArgb(255, 56, 66, 71);

        /// <summary>
        /// The background color
        /// </summary>
        public static readonly System.Drawing.Color BACKGROUND_COLOR = System.Drawing.Color.FromArgb(255, 17, 27, 32);

        /// <summary>
        /// The color of a dark grey background
        /// </summary>
        public static readonly System.Drawing.Color DARK_GREY = System.Drawing.Color.FromArgb(255, 27, 27, 29);

        /// <summary>
        /// The color of a notification background
        /// </summary>
        public static readonly System.Drawing.Color NOTIFICATION_COLOR = System.Drawing.Color.FromArgb(255, 255, 105, 0);

        /// <summary>
        /// The name of the font to use
        /// </summary>
        public const string FONT_NAME = "monospace";

        /// <summary>
        /// The main font to use for drawing text in the GUI
        /// </summary>
        public readonly static Font Font = new System.Drawing.Font(FONT_NAME, 8.0f, System.Drawing.FontStyle.Bold);
        #endregion
    }
}
