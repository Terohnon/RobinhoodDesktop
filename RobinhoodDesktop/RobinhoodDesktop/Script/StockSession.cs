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
        public List<string> DataScriptPaths = new List<string>();

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
        /// List of action scripts that have been executed
        /// </summary>
        public Dictionary<object, Assembly> Scripts = new Dictionary<object, Assembly>(); 

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

        /// <summary>
        /// A list of charts associated with this session
        /// </summary>
        public List<DataChartGui> Charts = new List<DataChartGui>();
        #endregion

        /// <summary>
        /// Creates a session based on the specified source data an analysis scripts
        /// </summary>
        /// <param name="sources">The source data files</param>
        /// <param name="sinkScripts">The data analysis scripts</param>
        /// <returns>The session instance</returns>
        public static StockSession LoadData(List<string> sources, List<string> sinkScripts)
        {
            StockSession session = new StockSession();

            session.DataScriptPaths.Clear();
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

            session.SourceFile = StockDataFile.Open(sources.ConvertAll<Stream>((s) => { return System.IO.Stream.Synchronized(new FileStream(s, FileMode.Open)); }));
            session.DataScriptPaths.Add("tmp/" + SOURCE_CLASS + ".cs");
            using(var file = new StreamWriter(new FileStream(session.DataScriptPaths.Last(), FileMode.Create))) file.Write(session.SourceFile.GetSourceCode(SOURCE_CLASS));

            // Put the data set reference script first
            List<string> totalSinkScripts = sinkScripts.ToList();
            totalSinkScripts.Insert(0, "Script\\Data\\DataSetReference.cs");
            session.SinkFile = new StockDataFile(totalSinkScripts.ConvertAll<string>((f) => { return Path.GetFileNameWithoutExtension(f); }), totalSinkScripts.ConvertAll<string>((f) => { return File.ReadAllText(f); }));
            session.SinkFile.Interval = session.SourceFile.Interval;
            session.DataScriptPaths.Add("tmp/" + SINK_CLASS + ".cs");
            using(var file = new StreamWriter(new FileStream(session.DataScriptPaths.Last(), FileMode.Create))) file.Write(session.SinkFile.GenStockDataSink());
            session.DataScriptPaths.AddRange(totalSinkScripts);

            // Create the evaluator file (needs to be compiled in the script since it references StockDataSource)
            string[] embeddedFiles = new string[]
            {
                    "RobinhoodDesktop.Script.StockEvaluator.cs",
                    "RobinhoodDesktop.Script.StockProcessor.cs"
            };
            foreach(var f in embeddedFiles)
            {
                session.DataScriptPaths.Add(string.Format("tmp/{0}.cs", f.Substring(24, f.Length - 27)));
                StringBuilder analyzerCode = new StringBuilder();
                analyzerCode.Append(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(f)).ReadToEnd());
                using(var file = new StreamWriter(new FileStream(session.DataScriptPaths.Last(), FileMode.Create))) file.Write(StockDataFile.FormatSource(analyzerCode.ToString()));
            }

            // Add the user defined analyzers
            foreach(string path in Directory.GetFiles(@"Script/Decision", "*.cs", SearchOption.AllDirectories)) session.DataScriptPaths.Add(path);
            foreach(string path in Directory.GetFiles(@"Script/Action", "*.cs", SearchOption.AllDirectories)) session.DataScriptPaths.Add(path);

            // Build the data
            session.Reload();
            if(session.Data != null)
            {
                StockSession.Instance = session;
            }
            else
            {
                session.SourceFile.Close();
            }
            return StockSession.Instance;
        }

        /// <summary>
        /// Creates a chart instance within a data script
        /// </summary>
        /// <param name="sources">The data sources to load</param>
        /// <param name="sinkScripts">The data processors to apply</param>
        public static DataChartGui AddChart(List<string> sources, List<string> sinkScripts)
        {
            var session = (Instance != null) ? Instance : LoadData(sources, sinkScripts);
            DataChartGui chart = null;
            if(session != null) chart = session.AddChart();
            return chart;
        }

        /// <summary>
        /// Creates a new chart and adds it to the session
        /// </summary>
        /// <returns>The created chart</returns>
        public DataChartGui AddChart()
        {
            DataChartGui chart = null;
            if(this.Data != null)
            {
                try
                {
                    chart = new DataChartGui(this.Data, this);
                    this.Charts.Add(chart);
                    var ctrl = (System.Windows.Forms.Control)(chart.GuiPanel);
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

            return chart;
        }

        /// <summary>
        /// Reloads the scripts and executes them
        /// </summary>
        public void Reload()
        {
            Data = null;
            SourceFile.Reload();
            SinkFile.Reload();

            // Re-load the data scripts, pulling in any recent changes
            Run(this, DataScriptPaths);

            // Create and get the StockProcessor instance, which also populates the Data field in the session
            Assembly dataScript;
            if(Scripts.TryGetValue(this, out dataScript))
            {
                var getProcessor = dataScript.GetStaticMethod("RobinhoodDesktop.Script.StockProcessor.GetInstance", this);
                var processor = getProcessor(this);

                // Execute the reload callback
                if(OnReload != null) OnReload();
            }
        }

        /// <summary>
        /// Loads a script instance
        /// </summary>
        public void Run(object owner, List<string> scripts)
        {
#if DEBUG
            var isDebug = true;
#else
            var isDebug = false;
#endif
            Assembly oldScript;
            if(Scripts.TryGetValue(owner, out oldScript))
            {
                oldScript.UnloadOwnerDomain();
                Scripts.Remove(owner);
            }

            try
            {
                CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Mono;
                CSScript.MonoEvaluator.CompilerSettings.Platform = Mono.CSharp.Platform.X64;
                List<string> references = new List<string>()
                {
                    "TensorFlow.NET.dll",
                    "Google.Protobuf.dll",
                    "Newtonsoft.Json",
                    "NumSharp.Lite",
                    "netstandard",
                    "System.Memory",
                    "System.Numerics"
                };
                foreach(var s in Scripts.Values) references.Add(s.Location);
                Scripts[owner] = CSScript.LoadFiles(scripts.ToArray(), null, isDebug, references.ToArray());

                // Check if a "Run" method should be executed
                MethodDelegate runFunc = null;
                try { runFunc = Scripts[owner].GetStaticMethod("*.Run", this); } catch(Exception ex) { };
                if(runFunc != null)
                {
                    System.Threading.Tasks.Task.Run(() => { runFunc(this); });
                }
            }
            catch(Exception ex)
            {
                string err = ex.ToString();
                System.Text.RegularExpressions.Regex.Replace(err, "\r\n.*?warning.*?\r\n", "\r\n");
                Console.WriteLine(err);
                System.Windows.Forms.MessageBox.Show(err);
            }
        }
    }
}
