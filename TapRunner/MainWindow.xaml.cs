using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using OpenTap;

namespace TapRunner
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _planPath;

        public string PlanPath
        {
            get { return _planPath; }
            set
            {
                if (value != _planPath)
                {
                    _planPath = value;
                    OnPropertyChanged(nameof(PlanPath));
                }
            }
        }

        public TestPlan Plan { get; set; } = new TestPlan();
        public Verdict PlanVerdict { get; set; }

        //private bool _testPlanRunning;
        private TapThread _testPlanThread;
        private LogPanel _logPanel;
        private ResultsPanel _resultsPanel;

        public MainWindow()
        {
            InitializeComponent();

            // Default values
            PlanPath = @"..\..\TapPlans\Example1.TapPlan";
            //TestPlanTextBox.DataContext = this;
            this.DataContext = this;

            PlanVerdict = Verdict.Inconclusive;

            var start = DateTime.Now;

            // Log Panel
            LogPanel.SetStartupTime(start);
            _logPanel = new LogPanel(LogListView);
            Log.AddListener(_logPanel);

            // Results Panel
            _resultsPanel = new ResultsPanel(ResultsListView);
        }

        private void OnOpenMenu(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dialog = new OpenFileDialog
            {
                DefaultExt = @".TapPlan",
                FileName = "",
                Filter = @"Test Plan File (*.TapPlan)|*.TapPlan",
                InitialDirectory = AssemblyDirectory,
                Title = "Open File",
            };

            // Show open file dialog box
            var okResult = dialog.ShowDialog();

            // Process open file dialog box results
            if (okResult == true)
            {
                // Open document
                PlanPath = dialog.FileName;
                LoadTestPlan(PlanPath);
            }
        }

        public class PlanItem
        {
            public string Name { get; set; }
            public string Verdict { get; set; }
        }

        private void LoadTestPlan(string path)
        {
            Plan = TestPlan.Load(path);

            PlanListView.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Populate list
                try
                {
                    PlanListView.Items.Clear();
                }
                catch
                {
                    // ignored
                }

                foreach (var item in Plan.Steps)
                {
                    try
                    {
                        PlanListView.Items.Add(new PlanItem { Name = item.Name });
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }));
        }

        private void OnCloseMenu(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not Implemented", MethodBase.GetCurrentMethod()?.Name,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnExitMenu(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OnRunMenu(object sender, RoutedEventArgs e)
        {
            _testPlanThread = TapThread.Start(() =>
            {
                // Start finding plugins.
                // PluginManager.DirectoriesToSearch.AddRange(Search);
                PluginManager.SearchAsync();

                // Load the Test Plan.
                if (Plan == null || Plan.Path == null)
                    LoadTestPlan(PlanPath);

                // Plan.PrintTestPlanRunSummary = true;
                PlanVerdict = Verdict.Inconclusive;

                // Clear log and results panels in the GUI
                //LogListView.Items.Clear();
                //ResultsListView.Items.Clear();

                // Configure Result Listeners
                var resultListeners = new List<ResultListener>
                {
                    // new LogResultListener(),
                    // new ResultsPanel(ResultsListView)
                    _resultsPanel
                };

                // Specify a bench settings profile from which to load
                // the bench settings. The parameter given here should correspond
                // to the name of a subdirectory of %TAP_PATH%/Settings/Bench.
                // If not specified, %TAP_PATH%/Settings/Bench/Default is used.
                const string settings = @"";
                if (!string.IsNullOrWhiteSpace(settings))
                {
                    TestPlanRunner.SetSettingsDir(settings);
                }

                // Execute the Test Plan.
                //_testPlanRunning = true;

                // PlanVerdict = Plan.Execute(resultListeners).Verdict;
                PlanVerdict = TestPlanRunner.RunPlanForDut(Plan, resultListeners, CancellationToken.None);

                //_testPlanRunning = false;

                // This forces test plan to be loaded again at every execution
                Plan = null;
            });
        }

        private void OnAbortMenu(object sender, RoutedEventArgs e)
        {
            if (Plan != null && Plan.IsRunning)
                _testPlanThread.Abort();
        }

        private void OnAboutMenu(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not Implemented", "About",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        private void OnClearLogPanel(object sender, RoutedEventArgs e)
        {
            _logPanel.Flush();
        }

        private void OnClearResultsPanel(object sender, RoutedEventArgs e)
        {
            _resultsPanel.Flush();
        }

        private void OnCopyLogPanel(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_logPanel.GetContentOfSelectedItems());
        }

        private void OnCopyResultsPanel(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_resultsPanel.GetContentOfSelectedItems());
        }
    }
}