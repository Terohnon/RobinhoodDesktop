using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

using CSScriptLibrary;

namespace RobinhoodDesktop
{
    public class AlgorithmScreen
    {
        [Serializable]
        public class AlgorithmScreenConfig
        {
            /// <summary>
            /// A list of paths to source data files, separated with \n
            /// </summary>
            public string SourceFiles = "";

            /// <summary>
            /// The most recent path used to open the source files
            /// </summary>
            public string SourceFilePath = "";

            /// <summary>
            /// List of scripts that analyze the stock point data
            /// </summary>
            public List<string> DataScripts = new List<string>();

            /// <summary>
            /// The list of scripts for performing various actions
            /// </summary>
            public List<string> ActionScripts = new List<string>();

            /// <summary>
            /// The most recent path used to open the data script files
            /// </summary>
            public string DataScriptPath = "";

            /// <summary>
            /// The path to the session script to run
            /// </summary>
            public string SessionScript = "";

            /// <summary>
            /// The most recent path used to open the session script
            /// </summary>
            public string SessionScriptPath = "";
        }

        public AlgorithmScreen(AlgorithmScreenConfig config = null)
        {
            this.Cfg = ((config != null) ? config : new AlgorithmScreenConfig());
            GuiPanel = new Panel();

            GuiBox = new Panel();
            GuiBox.AutoSize = true;
            GuiBox.BackColor = GuiStyle.BACKGROUND_COLOR;

            // Add the GUI elements for configurating the script
            DataFileButton = new GuiButton("Open...");
            DataFileButton.Location = new Point(5, 30);
            DataFileButton.MouseUp += (sender, e) =>
            {
                System.Windows.Forms.OpenFileDialog diag = new System.Windows.Forms.OpenFileDialog();
                diag.Multiselect = true;
                diag.Title = "Open Stock Data File...";
                diag.InitialDirectory = Cfg.SourceFilePath;
                if(diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if((DataFileTextbox.Text.Length > 0) && (!DataFileTextbox.Text.EndsWith("\n"))) DataFileTextbox.Text += "\r\n";
                    foreach(string dataPath in diag.FileNames)
                    {
                        DataFileTextbox.Text += dataPath + "\r\n";
                        Cfg.SourceFilePath = Path.GetDirectoryName(dataPath);
                    }
                    DataFileTextbox.Text = DataFileTextbox.Text.Remove(DataFileTextbox.Text.Length - 2, 2);
                    DataFileTextbox.SelectionStart = DataFileTextbox.Text.Length;
                    DataFileTextbox.ScrollToCaret();
                    DataFileButton.SetImage(GuiButton.ButtonImage.GREEN_WHITE);
                    DataLiveButton.SetImage(GuiButton.ButtonImage.GREEN_TRANSPARENT);
                }
            };
            GuiBox.Controls.Add(DataFileButton);
            GuiButton clearButton = new GuiButton("Clear");
            clearButton.Location = new Point(DataFileButton.Bounds.Right + 5, DataFileButton.Location.Y);
            clearButton.SetImage(GuiButton.ButtonImage.GREEN_TRANSPARENT);
            clearButton.MouseUp += (sender, e) =>
            {
                DataFileTextbox.Text = "";
            };
            GuiBox.Controls.Add(clearButton);
            DataLiveButton = new GuiButton("Live");
            DataLiveButton.Location = new Point(clearButton.Bounds.Right + 5, clearButton.Location.Y);
            DataLiveButton.SetImage(GuiButton.ButtonImage.GREEN_WHITE);
            DataLiveButton.MouseUp += (sender, e) =>
            {
                DataFileTextbox.Text = "";
                DataLiveButton.SetImage(GuiButton.ButtonImage.GREEN_WHITE);
                DataFileButton.SetImage(GuiButton.ButtonImage.GREEN_TRANSPARENT);
            };
            GuiBox.Controls.Add(DataLiveButton);
            Label sourceDataLabel = new Label();
            sourceDataLabel.Text = "Source Data";
            sourceDataLabel.ForeColor = GuiStyle.PRICE_COLOR_POSITIVE;
            sourceDataLabel.Font = GuiStyle.Font;
            sourceDataLabel.Location = new Point(DataFileButton.Bounds.Left, (DataFileButton.Bounds.Top - sourceDataLabel.Height) + 5);
            GuiBox.Controls.Add(sourceDataLabel);
            DataFileTextbox = new TextBox();
            DataFileTextbox.Location = new Point(DataFileButton.Bounds.Left, DataFileButton.Bounds.Bottom + 5);
            DataFileTextbox.Size = new Size(300, 300);
            DataFileTextbox.Multiline = true;
            DataFileTextbox.WordWrap = false;
            DataFileTextbox.BackColor = GuiStyle.DARK_GREY;
            DataFileTextbox.ForeColor = GuiStyle.TEXT_COLOR;
            DataFileTextbox.Font = GuiStyle.Font;
            DataFileTextbox.Text = Cfg.SourceFiles.Replace("\n", "\r\n");
            if(!string.IsNullOrEmpty(DataFileTextbox.Text))
            {
                DataFileButton.SetImage(GuiButton.ButtonImage.GREEN_WHITE);
                DataLiveButton.SetImage(GuiButton.ButtonImage.GREEN_TRANSPARENT);
            }
            DataFileTextbox.TextChanged += (sender, e) =>
            {
                Cfg.SourceFiles = DataFileTextbox.Text;
            };
            GuiBox.Controls.Add(DataFileTextbox);

            // Add the GUI for selecting the data scripts
            DataScriptListPanel = new Panel();
            DataScriptListPanel.Location = new Point(DataFileTextbox.Bounds.Right + 25, DataFileTextbox.Location.Y);
            DataScriptListPanel.Size = new Size(150, 150);
            DataScriptListPanel.BorderStyle = BorderStyle.FixedSingle;
            DataScriptListPanel.ForeColor = GuiStyle.PRICE_COLOR_POSITIVE;
            GuiBox.Controls.Add(DataScriptListPanel);
            Label dataScriptsLabel = new Label();
            dataScriptsLabel.Text = "Data Analysis Scripts";
            dataScriptsLabel.ForeColor = GuiStyle.PRICE_COLOR_POSITIVE;
            dataScriptsLabel.Font = GuiStyle.Font;
            dataScriptsLabel.Location = new Point(DataScriptListPanel.Bounds.Left, (DataScriptListPanel.Bounds.Top - dataScriptsLabel.Height) + 5);
            GuiBox.Controls.Add(dataScriptsLabel);
            DataScriptListScrollbar = new CustomControls.CustomScrollbar();
            DataScriptListScrollbar.Minimum = 0;
            DataScriptListScrollbar.Maximum = DataScriptListPanel.Height;
            DataScriptListScrollbar.LargeChange = DataScriptListScrollbar.Maximum / DataScriptListScrollbar.Height;
            DataScriptListScrollbar.SmallChange = 15;
            DataScriptListScrollbar.Value = 0;
            DataScriptListScrollbar.Attach(DataScriptListPanel);
            GuiBox.Controls.Add(DataScriptListScrollbar);
            DataScriptAddButton = new GuiButton("Add...");
            DataScriptAddButton.Location = new Point(DataScriptListPanel.Location.X + 5, DataScriptListPanel.Bounds.Bottom + 5);
            DataScriptAddButton.MouseUp += (sender, e) =>
            {
                System.Windows.Forms.OpenFileDialog diag = new System.Windows.Forms.OpenFileDialog();
                diag.Multiselect = true;
                diag.Filter = "C# Files|*.cs";
                diag.Title = "Open stock data script(s)...";
                string prevPath = diag.InitialDirectory;
                diag.InitialDirectory = Cfg.DataScriptPath;
                diag.RestoreDirectory = false;
                if(diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach(string scriptPath in diag.FileNames)
                    {
                        Cfg.DataScriptPath = Path.GetDirectoryName(scriptPath);

                        // Skip this item if it has already been loaded
                        var script = scriptPath;
                        if(Cfg.DataScripts.Contains(script)) continue;

                        AddDataScript(script);
                        Cfg.DataScripts.Add(script);
                    }
                }
            };
            foreach(var script in Cfg.DataScripts) AddDataScript(script);
            GuiBox.Controls.Add(DataScriptAddButton);

            // Add the GUI for selecting the data scripts
            DecisionScriptListPanel = new Panel();
            DecisionScriptListPanel.Location = new Point(DataScriptListPanel.Bounds.Right + 25, DataScriptListPanel.Location.Y);
            DecisionScriptListPanel.Size = new Size(450, 150);
            DecisionScriptListPanel.BorderStyle = BorderStyle.FixedSingle;
            DecisionScriptListPanel.ForeColor = GuiStyle.PRICE_COLOR_POSITIVE;
            GuiBox.Controls.Add(DecisionScriptListPanel);
            Label decisionScriptsLabel = new Label();
            decisionScriptsLabel.Text = "Action Scripts";
            decisionScriptsLabel.ForeColor = GuiStyle.PRICE_COLOR_POSITIVE;
            decisionScriptsLabel.Font = GuiStyle.Font;
            decisionScriptsLabel.Location = new Point(DecisionScriptListPanel.Bounds.Left, (DecisionScriptListPanel.Bounds.Top - decisionScriptsLabel.Height) + 5);
            GuiBox.Controls.Add(decisionScriptsLabel);
            DecisionScriptListScrollbar = new CustomControls.CustomScrollbar();
            DecisionScriptListScrollbar.Minimum = 0;
            DecisionScriptListScrollbar.Maximum = DecisionScriptListPanel.Height;
            DecisionScriptListScrollbar.LargeChange = DecisionScriptListScrollbar.Maximum / DecisionScriptListScrollbar.Height;
            DecisionScriptListScrollbar.SmallChange = 15;
            DecisionScriptListScrollbar.Value = 0;
            DecisionScriptListScrollbar.Attach(DecisionScriptListPanel);
            GuiBox.Controls.Add(DecisionScriptListScrollbar);
            DecisionScriptAddButton = new GuiButton("Add...");
            DecisionScriptAddButton.Location = new Point(DecisionScriptListPanel.Location.X + 5, DecisionScriptListPanel.Bounds.Bottom + 5);
            DecisionScriptAddButton.MouseUp += (sender, e) =>
            {
                DecisionScriptListPanel.Controls.Add(new ScriptTextBox(this, ""));
            };
            GuiBox.Controls.Add(DecisionScriptAddButton);
            DecisionScriptBrowseButton = new GuiButton("Browse...");
            DecisionScriptBrowseButton.Location = new Point(DecisionScriptAddButton.Right + 5, DecisionScriptAddButton.Bounds.Top);
            DecisionScriptBrowseButton.MouseUp += (sender, e) =>
            {
                System.Windows.Forms.OpenFileDialog diag = new System.Windows.Forms.OpenFileDialog();
                diag.Multiselect = true;
                diag.Filter = "C# Files|*.cs";
                diag.Title = "Open stock data script(s)...";
                string prevPath = diag.InitialDirectory;
                diag.InitialDirectory = Cfg.DataScriptPath;
                diag.RestoreDirectory = false;
                if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (string scriptPath in diag.FileNames)
                    {
                        Cfg.DataScriptPath = Path.GetDirectoryName(scriptPath);

                        // Skip this item if it has already been loaded
                        var script = scriptPath;
                        if (Cfg.DataScripts.Contains(script)) continue;

                        DecisionScriptListPanel.Controls.Add(new ScriptTextBox(this, scriptPath));
                    }
                }
            };
            GuiBox.Controls.Add(DecisionScriptBrowseButton);
            
            foreach (var script in Cfg.ActionScripts) DecisionScriptListPanel.Controls.Add(new ScriptTextBox(this, script));


            // Create the start button
            var RunAllButton = new GuiButton("Start");
            RunAllButton.Location = new Point(DecisionScriptListPanel.Bounds.Right - RunAllButton.Width - 5, DecisionScriptListPanel.Bounds.Top - RunAllButton.Height - 5);
            RunAllButton.MouseUp += (sender, e) =>
            {
                foreach(var s in Scripts)
                {
                    s.Run();
                }
            };
            GuiPanel.Controls.Add(RunAllButton);


            // Create the back button to leave the screen
            BackButton = new PictureBox();
            BackButton.Image = Bitmap.FromFile("Content/GUI/Back.png");
            BackButton.Size = BackButton.Image.Size;
            BackButton.Location = new Point(5, 0);
            GuiPanel.Controls.Add(BackButton);             

            // Define the overall panel that everything else is contained inside
            GuiPanel.BackColor = GuiStyle.BACKGROUND_COLOR;
            GuiPanel.Controls.Add(GuiBox);
            GuiPanel.Resize += (sender, e) =>
            {
                GuiBox.Size = new Size(GuiPanel.Width - BackButton.Width, 300);
                GuiBox.Location = new System.Drawing.Point(BackButton.Width + 10, 10);
            };

            // Specify the interface that should be used to add a chart to the screen
            Script.StockSession.AddToGui += (control) =>
            {
                if (control != null)
                {
                    control.Location = new Point(ChartButton.Location.X, ChartButton.Bottom + 5);
                    GuiPanel.Controls.Add(control);
                }
            };
            Script.StockSession.GuiContainer = (System.Windows.Forms.Control)GuiPanel;

            // Create a button which adds a chart to the screen
            ChartButton = new GuiButton("Add Chart");
            ChartButton.Location = new Point(5, DataFileTextbox.Bottom + 25);
            ChartButton.MouseUp += (sender, e) =>
            {
                Script.StockSession.AddChart(Cfg.SourceFiles.Replace("\r", "").Split('\n').ToList(), Cfg.DataScripts);
            };
            GuiPanel.Controls.Add(ChartButton);
        }

        #region Variables
        /// <summary>
        /// The configurable values for the screen
        /// </summary>
        public AlgorithmScreenConfig Cfg;

        /// <summary>
        /// Stores the configuration used to generate the session
        /// </summary>
        public Script.StockSession Session;

        /// <summary>
        /// The background panel for the login screen
        /// </summary>
        public Panel GuiPanel;

        /// <summary>
        /// Button that exits this screen
        /// </summary>
        public PictureBox BackButton;

        /// <summary>
        /// The list of scripts
        /// </summary>
        public List<ScriptTextBox> Scripts = new List<ScriptTextBox>();

        /// <summary>
        /// A box containing the main GUI elements (to make them easier to re-position)
        /// </summary>
        private Panel GuiBox;

        private GuiButton DataFileButton;
        private GuiButton DataLiveButton;
        private TextBox DataFileTextbox;

        private GuiButton DataScriptAddButton;
        private Panel DataScriptListPanel;
        private CustomControls.CustomScrollbar DataScriptListScrollbar;

        private GuiButton DecisionScriptAddButton;
        private GuiButton DecisionScriptBrowseButton;
        private Panel DecisionScriptListPanel;
        private CustomControls.CustomScrollbar DecisionScriptListScrollbar;

        private GuiButton ChartButton;
        #endregion

        #region Types
        public class ScriptTextBox : TextBox
        {
            public PictureBox CloseButton;
            private Control PrevParent;
            private AlgorithmScreen Screen;
            public ScriptTextBox(AlgorithmScreen screen, string script = "")
                : base()
            {
                this.Screen = screen;
                this.Text = script;
                this.BackColor = GuiStyle.DARK_GREY;
                this.ForeColor = GuiStyle.TEXT_COLOR;
                this.Font = GuiStyle.Font;


                // Add a remove button for the script
                var CloseButton = new PictureBox();
                CloseButton.Image = Bitmap.FromFile("Content/GUI/Button_Close.png");
                CloseButton.Size = new Size(CloseButton.Image.Width, CloseButton.Image.Height);
                CloseButton.BringToFront();
                CloseButton.MouseUp += (subSender, subE) =>
                {
                    Remove();
                };

                this.LocationChanged += (sScript, eScript) =>
                {
                    CloseButton.Location = new Point(this.Right - 5, this.Top - ((CloseButton.Height - this.Height) / 2));
                };
                Resize += (sender, e) =>
                {
                    CloseButton.Location = new Point(this.Right, this.Top);
                };
                ParentChanged += (sender, e) =>
                {
                    if (this.Parent != null)
                    {
                        this.Parent.Controls.Add(CloseButton);
                        this.PrevParent = Parent;
                        this.Screen.Scripts.Add(this);
                        Pack();
                    }
                    else
                    {
                        Remove();
                    }
                };
                KeyDown += (s, eventArgs) =>
                {
                     if (eventArgs.KeyCode == Keys.Enter)
                     {
                         try
                         {
                             if(!string.IsNullOrEmpty(this.Text))
                             {
                                // Save the script to the configuration
                                var idx = Screen.Scripts.IndexOf(this);
                                if (idx >= Screen.Cfg.ActionScripts.Count) Screen.Cfg.ActionScripts.Insert(idx, this.Text);
                                else Screen.Cfg.ActionScripts[idx] = this.Text;

                                 // Run the script
                                 Run();
                             }
                             else
                             {
                                 Remove();
                             }
                         }
                         catch (Exception ex)
                         {
                            System.Windows.Forms.MessageBox.Show(ex.ToString());
                         }
                     }
                };
            }

            public void Run()
            {
                // Ensure the data is loaded
                if((Script.StockSession.Instance == null) || !Script.StockSession.Instance.Scripts.ContainsKey(Script.StockSession.Instance))
                {
                    Script.StockSession.LoadData(Screen.Cfg.SourceFiles.Replace("\r", "").Split('\n').ToList(), Screen.Cfg.DataScripts);
                }

                // Check if the specified script is a path to a file
                if(Script.StockSession.Instance != null)
                {
                    if(File.Exists(this.Text))
                    {
                        Script.StockSession.Instance.Run(this, new List<string>() { this.Text });
                    }
                    else
                    {
                        // Run the sepcified script and print the results
                        var compiler = CSScript.MonoEvaluator.ReferenceAssemblyOf(this);
                        foreach(var s in Script.StockSession.Instance.Scripts.Values) compiler = compiler.ReferenceAssembly(s.Location);
                        var accessor = compiler.LoadDelegate<Func<object>>(@"object GetValue() { return " + this.Text + ";}");
                        object result = accessor();
                        if(result != null) Console.WriteLine(result.ToString());
                    }
                }
            }



            public void Remove()
            {
                if (Parent != null)
                {
                    PrevParent = Parent;
                    Parent.Controls.Remove(this);
                }
                if (PrevParent != null)
                {
                    PrevParent.Controls.Remove(CloseButton);
                    int idx = this.Screen.Scripts.IndexOf(this);
                    if(idx < this.Screen.Cfg.ActionScripts.Count) this.Screen.Cfg.ActionScripts.RemoveAt(idx);
                    this.Screen.Scripts.Remove(this);
                    Pack();
                }
            }

            public void Pack()
            {
                // Pack the list to fill the gaps
                int packPos = 5;
                foreach (Control c in Screen.DecisionScriptListPanel.Controls)
                {
                    if (c.GetType().Equals(typeof(ScriptTextBox)))
                    {
                        c.Location = new Point(c.Location.X, packPos);
                        c.Width = (Screen.DecisionScriptListPanel.Width - c.Left) - 5;
                        packPos = c.Bounds.Bottom + 5;
                    }
                }
            }
        }
        #endregion

        private void RefreshDataListPanel()
        {

        }

        /// <summary>
        /// Adds the GUI elements for a data script
        /// </summary>
        /// <param name="script"></param>
        private void AddDataScript(string script)
        {
            // Add a label for the script
            int yPos = (DataScriptListPanel.Controls.Count > 0) ? DataScriptListPanel.Controls[DataScriptListPanel.Controls.Count - 1].Bounds.Bottom + 5 : 5;
            var scriptLabel = new Label();
            scriptLabel.Text = script.Substring(script.Replace('\\', '/').LastIndexOf("/") + 1);
            scriptLabel.Width = (DataScriptListPanel.Width - 5) - scriptLabel.Location.X;
            scriptLabel.ForeColor = GuiStyle.PRICE_COLOR_POSITIVE;
            scriptLabel.Width = (DataScriptListPanel.Width - scriptLabel.Location.X - 30);
            DataScriptListPanel.Controls.Add(scriptLabel);

            // Add a remove button for the script
            var removeButton = new PictureBox();
            removeButton.Image = Bitmap.FromFile("Content/GUI/Button_Close.png");
            removeButton.Size = new Size(removeButton.Image.Width, removeButton.Image.Height);
            scriptLabel.LocationChanged += (sScript, eScript) =>
            {
                removeButton.Location = new Point(DataScriptListPanel.Width - removeButton.Width - 5, scriptLabel.Location.Y - ((removeButton.Height - scriptLabel.Height) / 2));
            };
            removeButton.MouseUp += (subSender, subE) =>
            {
                if(DataScriptListPanel.Controls.Contains(scriptLabel))
                {
                    DataScriptListPanel.Controls.Remove(scriptLabel);
                    DataScriptListPanel.Controls.Remove(removeButton);

                    // Pack the list to fill the gaps
                    int packPos = 5;
                    foreach(Control c in DataScriptListPanel.Controls)
                    {
                        if(c.GetType().Equals(typeof(Label)))
                        {
                            c.Location = new Point(c.Location.X, packPos);
                            packPos = c.Bounds.Bottom + 5;
                        }
                    }
                }
                var pathIdx = Cfg.DataScripts.IndexOf(script);
                if(pathIdx >= 0) Cfg.DataScripts.RemoveAt(pathIdx);
            };
            DataScriptListPanel.Controls.Add(removeButton);

            scriptLabel.Location = new Point(5, yPos);
        }
    }
}
