using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

using CSScriptLibrary;

namespace RobinhoodDesktop.Script
{
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
        /// The path to the source data file
        /// </summary>
        public string SourceFilePath;

        /// <summary>
        /// The list of scripts that should be loaded to process the stock data
        /// </summary>
        public List<string> StockDataScripts = new List<string>();

        /// <summary>
        /// The data file the source stock data is being pulled from
        /// </summary>
        [NonSerialized]
        public static StockDataFile SourceFile;

        /// <summary>
        /// The data file representing the analyzed stock data
        /// </summary>
        [NonSerialized]
        public static StockDataFile SinkFile;

        /// <summary>
        /// The dynamically loaded functionality
        /// </summary>
        [NonSerialized]
        public static Assembly ScriptInstance;
        #endregion

        public static StockSession Start(List<string> sources, List<string> sinkScripts, string executeScript)
        {
            StockSession session = new StockSession();

            List<string> script = new List<string>();
            Directory.CreateDirectory("tmp");

            // Convert any legacy files before further processing
            var legacyFiles = sources.Where((s) => { return s.EndsWith(".csv"); }).ToList();
            if(legacyFiles.Count() > 0)
            {
                System.Windows.Forms.SaveFileDialog saveDiag = new System.Windows.Forms.SaveFileDialog();
                saveDiag.Title = "Save converted data file as...";
                if(saveDiag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var convertedFile = StockDataFile.Convert(legacyFiles, new FileStream(saveDiag.FileName, FileMode.Create));
                    convertedFile.Close();
                    sources.Add(saveDiag.FileName);
                }
                else
                {
                    // Cancel running the script
                    return null;
                }
                foreach(var l in legacyFiles) sources.Remove(l);
            }

            SourceFile = StockDataFile.Open(sources.ConvertAll<Stream>((s) => { return new FileStream(s, FileMode.Open); }));
            script.Add("tmp/" + SOURCE_CLASS + ".cs");
            using(var file = new StreamWriter(new FileStream(script.Last(), FileMode.Create))) file.Write(SourceFile.GetSourceCode(SOURCE_CLASS));

            SinkFile = new StockDataFile(sinkScripts.ConvertAll<string>((f) => { return Path.GetFileNameWithoutExtension(f); }), sinkScripts.ConvertAll<string>((f) => { return File.ReadAllText(f); }));
            script.Add("tmp/" + SINK_CLASS + ".cs");
            using(var file = new StreamWriter(new FileStream(script.Last(), FileMode.Create))) file.Write(SinkFile.GetSourceCode(SINK_CLASS));

            // Create the evaluator file (needs to be compiled in the script since it references StockDataSource)
            string[] embeddedFiles = new string[]
            {
                    "RobinhoodDesktop.Script.StockEvaluator.cs",
                    "RobinhoodDesktop.Script.StockProcessor.cs"
            };
            foreach(var f in embeddedFiles)
            {
                script.Add(string.Format("tmp/{0}.cs", f.Substring(24, f.Length - 27)));
                StringBuilder analyzerCode = new StringBuilder();
                analyzerCode.Append(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(f)).ReadToEnd());
                using(var file = new StreamWriter(new FileStream(script.Last(), FileMode.Create))) file.Write(StockDataFile.FormatSource(analyzerCode.ToString()));
            }

            // Add the user defined analyzers
            foreach(string path in Directory.GetFiles(@"Script/Decision", "*.cs", SearchOption.AllDirectories)) script.Add(path);
            foreach(string path in Directory.GetFiles(@"Script/Action", "*.cs", SearchOption.AllDirectories)) script.Add(path);

            // Get the code that will actually run the session
            script.Add(executeScript);

            // Build and run the session
#if DEBUG
            var isDebug = true;
#else
                var isDebug = false;
#endif
            try
            {
                if(!string.IsNullOrEmpty(executeScript))
                {
                    ScriptInstance = CSScript.LoadFiles(script.ToArray(), null, isDebug);
                    var run = ScriptInstance.GetStaticMethod("*.Run", session);
                    run(session);
                }
            }
            catch(Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }

            // Cleanup
            SourceFile.Close();

            return session;
        }

        public void Run()
        {
            System.Windows.Forms.OpenFileDialog diag = new System.Windows.Forms.OpenFileDialog();
            diag.Multiselect = true;
            diag.Title = "Open Stock Data File...";
            if(diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<string> script = new List<string>();
                Directory.CreateDirectory("tmp");

                // Get the source file
                if(diag.FileName.EndsWith(".csv"))
                {
                    System.Windows.Forms.SaveFileDialog saveDiag = new System.Windows.Forms.SaveFileDialog();
                    if(saveDiag.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                    SourceFile = StockDataFile.Convert(diag.FileNames.ToList(), new FileStream(saveDiag.FileName, FileMode.Create));
                }
                else
                {
                    SourceFile = StockDataFile.Open(new FileStream(diag.FileName, FileMode.Open));
                }
                script.Add("tmp/" + SOURCE_CLASS + ".cs");
                using(var file = new StreamWriter(new FileStream(script.Last(), FileMode.Create))) file.Write(SourceFile.GetSourceCode(SOURCE_CLASS));

                // Create the sink file
                //SinkFile = new StockDataFile(new List<string>() { }, new List<string>() {  });
                SinkFile = new StockDataFile(new List<string>() { "MovingAverage" }, new List<string>() { File.ReadAllText(@"Script/Data/MovingAverage.cs") });
                script.Add("tmp/" + SINK_CLASS + ".cs");
                using(var file = new StreamWriter(new FileStream(script.Last(), FileMode.Create))) file.Write(SinkFile.GetSourceCode(SINK_CLASS));

                // Create the analyzer file (needs to be compiled in the script since it references StockDataSource)
                var analyzerFilename = "RobinhoodDesktop.Script.StockAnalyzer.cs";
                script.Add("tmp/StockAnalyzer.cs");
                StringBuilder analyzerCode = new StringBuilder();
                analyzerCode.Append(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(analyzerFilename)).ReadToEnd());
                using(var file = new StreamWriter(new FileStream(script.Last(), FileMode.Create))) file.Write(StockDataFile.FormatSource(analyzerCode.ToString()));

                // Add the user defined analyzers
                string[] analyzerPaths = Directory.GetFiles(@"Script/Decision", "*.cs", SearchOption.AllDirectories);
                foreach(string path in analyzerPaths) script.Add(path);


                // Get the code that will actually run the session
                script.Add("tmp/StockSessionScript.cs");
                var sessionFilename = "RobinhoodDesktop.Script.StockSessionScript.cs";
                using(var file = new StreamWriter(new FileStream(script.Last(), FileMode.Create))) file.Write(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(sessionFilename)).ReadToEnd());

                // Build and run the session
#if DEBUG
                var isDebug = true;
#else
                var isDebug = false;
#endif
#if true
                try
                {
                    var scriptInstance = CSScript.LoadFiles(script.ToArray(), null, isDebug);
                    var run = scriptInstance.GetStaticMethod("RobinhoodDesktop.Script.StockSessionScript.Run", this);
                    run(this);
                }
                catch(Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
#else
                // Set up the derived data sink
                var sourceData = SourceFile.GetSegments<StockDataBase>();
                var sinkData = StockDataSetDerived<StockDataBase, StockDataBase>.Derive(sourceData, SinkFile, (data, idx) =>
                {
                    var point = new StockDataBase();
                    point.Update(data, idx);
                    return point;
                });
                SinkFile.SetSegments(sinkData);

                // Load the first set of data
                foreach(var pair in sinkData)
                {
                    foreach(var set in pair.Value)
                    {
                        set.Load();
                    }
                }
#endif

                // Cleanup
                SourceFile.Close();
            }

        }


        public void RunSession()
        {
            System.Windows.Forms.OpenFileDialog diag = new System.Windows.Forms.OpenFileDialog();
            diag.Multiselect = false;
            diag.Title = "Run Session Script...";
            if(diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<string> script = new List<string>();
                Directory.CreateDirectory("tmp");

                SourceFile = new StockDataFile(new List<string>() { }, new List<string>() { });
                script.Add("tmp/" + SOURCE_CLASS + ".cs");
                using(var file = new StreamWriter(new FileStream(script.Last(), FileMode.Create))) file.Write(SourceFile.GetSourceCode(SOURCE_CLASS));

                SinkFile = new StockDataFile(new List<string>() { "MovingAverage" }, new List<string>() { File.ReadAllText(@"Script/Data/MovingAverage.cs") });
                script.Add("tmp/" + SINK_CLASS + ".cs");
                using(var file = new StreamWriter(new FileStream(script.Last(), FileMode.Create))) file.Write(SinkFile.GetSourceCode(SINK_CLASS));

                // Create the evaluator file (needs to be compiled in the script since it references StockDataSource)
                string[] embeddedFiles = new string[]
                {
                    "RobinhoodDesktop.Script.StockEvaluator.cs",
                    "RobinhoodDesktop.Script.StockProcessor.cs"
                };
                foreach(var f in embeddedFiles)
                {
                    script.Add(string.Format("tmp/{0}.cs", f.Substring(24, f.Length - 27)));
                    StringBuilder analyzerCode = new StringBuilder();
                    analyzerCode.Append(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(f)).ReadToEnd());
                    using(var file = new StreamWriter(new FileStream(script.Last(), FileMode.Create))) file.Write(StockDataFile.FormatSource(analyzerCode.ToString()));
                }

                // Add the user defined analyzers
                foreach(string path in Directory.GetFiles(@"Script/Decision", "*.cs", SearchOption.AllDirectories)) script.Add(path);
                foreach(string path in Directory.GetFiles(@"Script/Action", "*.cs", SearchOption.AllDirectories)) script.Add(path);

                // Get the code that will actually run the session
                script.Add(diag.FileName);

                // Build and run the session
#if DEBUG
                var isDebug = true;
#else
                var isDebug = false;
#endif
                try
                {
                    var scriptInstance = CSScript.LoadFiles(script.ToArray(), null, isDebug);
                    var run = scriptInstance.GetStaticMethod("*.Run", this);
                    run(this);
                }
                catch(Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
            }
        }
    }
}
