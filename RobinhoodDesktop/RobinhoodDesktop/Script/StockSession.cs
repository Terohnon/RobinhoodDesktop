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
        public StockDataFile SourceFile;
        public StockDataFile SinkFile;
        #endregion

        public void Run()
        {
            System.Windows.Forms.OpenFileDialog diag = new System.Windows.Forms.OpenFileDialog();
            diag.Multiselect = true;
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
                SinkFile = new StockDataFile(new List<string>() { "MovingAverage" }, new List<string>() { File.ReadAllText(@"Script/MovingAverage.cs") });
                script.Add("tmp/" + SINK_CLASS + ".cs");
                using(var file = new StreamWriter(new FileStream(script.Last(), FileMode.Create))) file.Write(SinkFile.GetSourceCode(SINK_CLASS));

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
                var scriptInstance = CSScript.LoadFiles(script.ToArray(), null, isDebug);
                var run = scriptInstance.GetStaticMethod("RobinhoodDesktop.Script.StockSessionScript.Run", this);
                run(this);
#else
                // Set up the derived data sink
                var sourceData = SourceFile.GetSegments<StockDataBase>();
                var sinkData = StockDataSetDerived<StockDataBase, StockDataBase>.Derive(sourceData, SinkFile);
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
                SourceFile.File.Close();
            }
            
        }
    }
}
