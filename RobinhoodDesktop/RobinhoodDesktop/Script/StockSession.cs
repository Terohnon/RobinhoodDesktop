using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

using CSScriptLibrary;
using System.Windows.Controls;
using Mono.CSharp;

namespace RobinhoodDesktop.Script
{
    /// <summary>
    /// Specifies the options for keeping processed data in memory. Ideally all processed data would be kept,
    /// but if there is not enough memory for that then some may be discarded once processing has completed.
    /// </summary>
    public enum MemoryScheme
    {
        MEM_KEEP_NONE,
        MEM_KEEP_SOURCE,
        MEM_KEEP_DERIVED
    }

    [Serializable]
    public class StockSession
    {
        #region Constants
        /// <summary>
        /// The designator for the stock data source (the data read from the file/live interface)
        /// </summary>
        public const string SOURCE_CLASS = "StockDataSource";

        /// <summary>
        /// The designator for the stock data sink (the analyzed data)
        /// </summary>
        public const string SINK_CLASS = "StockDataSink";
        #endregion

        #region Variables
        /// <summary>
        /// Stores a reference to the most recent session that was created (probably the only session)
        /// </summary>
        public static StockSession Instance;

        /// <summary>
        /// The path to the source data file
        /// </summary>
        public string SourceFilePath;

        /// <summary>
        /// The list of scripts that should be loaded to process the stock data
        /// </summary>
        public List<string> Scripts = new List<string>();

        /// <summary>
        /// The data file the source stock data is being pulled from
        /// </summary>
        [NonSerialized]
        public StockDataFile SourceFile;

        /// <summary>
        /// The data file representing the analyzed stock data
        /// </summary>
        [NonSerialized]
        public StockDataFile SinkFile;

        /// <summary>
        /// The stock data associated with the session
        /// </summary>
        [NonSerialized]
        public Dictionary<string, List<StockDataInterface>> Data;

        /// <summary>
        /// The dynamically loaded functionality
        /// </summary>
        [NonSerialized]
        public Assembly ScriptInstance;

        /// <summary>
        /// Callback that can be used to add an element to the GUI
        /// </summary>
        [NonSerialized]
        public static AddGuiFunc AddToGui = null;
        public delegate void AddGuiFunc(System.Windows.Forms.Control c);

        /// <summary>
        /// The container object that other GUI elements should be added to
        /// </summary>
        [NonSerialized]
        public static System.Windows.Forms.Control GuiContainer = null;

        /// <summary>
        /// Callback that can be executed when the session is reloaded
        /// </summary>
        /// [NonSerialized]
        public Action OnReload;
        #endregion

        public static StockSession Start(List<string> sources, List<string> sinkScripts, string executeScript)
        {
            StockSession session = new StockSession();

            session.Scripts.Clear();
            Directory.CreateDirectory("tmp");

            // Convert any legacy files before further processing
            var legacyFiles = sources.Where((s) => { return s.EndsWith(".csv"); }).ToList();
            if(legacyFiles.Count() > 0)
            {
                System.Windows.Forms.SaveFileDialog saveDiag = new System.Windows.Forms.SaveFileDialog();
                saveDiag.Title = "Save converted data file as...";
                saveDiag.CheckFileExists = false;
                if (saveDiag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    List<string> convertedFileNames;
                    var convertedFiles = StockDataFile.ConvertByMonth(legacyFiles, Path.GetDirectoryName(saveDiag.FileName), out convertedFileNames);
                    foreach (var cf in convertedFileNames) sources.Add(cf);
                }
                else
                {
                    // Cancel running the script
                    return null;
                }
                foreach(var l in legacyFiles) sources.Remove(l);
            }

            session.SourceFile = StockDataFile.Open(sources.ConvertAll<Stream>((s) => { return new FileStream(s, FileMode.Open); }));
            session.Scripts.Add("tmp/" + SOURCE_CLASS + ".cs");
            using(var file = new StreamWriter(new FileStream(session.Scripts.Last(), FileMode.Create))) file.Write(session.SourceFile.GetSourceCode(SOURCE_CLASS));

            session.SinkFile = new StockDataFile(sinkScripts.ConvertAll<string>((f) => { return Path.GetFileNameWithoutExtension(f); }), sinkScripts.ConvertAll<string>((f) => { return File.ReadAllText(f); }));
            session.SinkFile.Interval = session.SourceFile.Interval;
            session.Scripts.Add("tmp/" + SINK_CLASS + ".cs");
            using(var file = new StreamWriter(new FileStream(session.Scripts.Last(), FileMode.Create))) file.Write(session.SinkFile.GenStockDataSink());
            session.Scripts.AddRange(sinkScripts);

            // Create the evaluator file (needs to be compiled in the script since it references StockDataSource)
            string[] embeddedFiles = new string[]
            {
                    "RobinhoodDesktop.Script.StockEvaluator.cs",
                    "RobinhoodDesktop.Script.StockProcessor.cs"
            };
            foreach(var f in embeddedFiles)
            {
                session.Scripts.Add(string.Format("tmp/{0}.cs", f.Substring(24, f.Length - 27)));
                StringBuilder analyzerCode = new StringBuilder();
                analyzerCode.Append(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(f)).ReadToEnd());
                using(var file = new StreamWriter(new FileStream(session.Scripts.Last(), FileMode.Create))) file.Write(StockDataFile.FormatSource(analyzerCode.ToString()));
            }

            // Add the user defined analyzers
            foreach(string path in Directory.GetFiles(@"Script/Decision", "*.cs", SearchOption.AllDirectories)) session.Scripts.Add(path);
            foreach(string path in Directory.GetFiles(@"Script/Action", "*.cs", SearchOption.AllDirectories)) session.Scripts.Add(path);

            // Get the code that will actually run the session
            if(!string.IsNullOrEmpty(executeScript)) session.Scripts.Add(executeScript);

            // Build and run the session
            session.LoadScripts(true);

            StockSession.Instance = session;
            return session;
        }

        /// <summary>
        /// Creates a chart instance within a data script
        /// </summary>
        /// <param name="sources">The data sources to load</param>
        /// <param name="sinkScripts">The data processors to apply</param>
        public static void AddChart(List<string> sources, List<string> sinkScripts)
        {
            var session = (Instance != null) ? Instance : Start(sources, sinkScripts, "");
            try
            {
                var ctrl = (System.Windows.Forms.Control)(new DataChartGui(session.Data, session).GuiPanel);
                if((ctrl != null) && (AddToGui != null))
                {
                    AddToGui(ctrl);
                }
            }
            catch(Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// Reloads the scripts and executes them
        /// </summary>
        public void Reload()
        {
            // Close any previously open script
            if(ScriptInstance != null)
            {
                ScriptInstance.UnloadOwnerDomain();
                ScriptInstance = null;
            }
            Data = null;
            SourceFile.Reload();
            SinkFile.Reload();

            // Re-load the scripts, pulling in any recent changes
            LoadScripts();

            // Execute the reload callback
            OnReload();
        }

        /// <summary>
        /// Loads a script instance
        /// </summary>
        /// <param name="run">If true, searches for a "run" method in the script and executes it</param>
        private void LoadScripts(bool run = false)
        {
#if DEBUG
            var isDebug = true;
#else
            var isDebug = false;
#endif

            try
            {
                CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Mono;
                CSScript.MonoEvaluator.CompilerSettings.Platform = Mono.CSharp.Platform.X64;
                ScriptInstance = CSScript.LoadFiles(Scripts.ToArray(), null, isDebug, "TensorFlow.NET.dll",
                                                                                                "Google.Protobuf.dll",
                                                                                                "Newtonsoft.Json",
                                                                                                "NumSharp.Lite",
                                                                                                "netstandard",
                                                                                                "System.Memory",
                                                                                                "System.Numerics");

                // Create and get the StockProcessor instance, which also populates the Data field in the session
                var getProcessor = ScriptInstance.GetStaticMethod("RobinhoodDesktop.Script.StockProcessor.GetInstance", this);
                var processor = getProcessor(this);

                // Check if a "Run" method should be executed
                if(run)
                {

                    MethodDelegate runFunc = null;
                    try { runFunc = ScriptInstance.GetStaticMethod("*.Run", this); } catch(Exception ex) { };
                    if(runFunc != null)
                    {
                        System.Threading.Tasks.Task.Run(() => { runFunc(this); });
                    }
                }
            }
            catch(Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
                SourceFile.Close();
                SinkFile.Close();
            }
        }
    }
}
