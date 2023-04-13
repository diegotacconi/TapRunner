using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenTap;

namespace TapRunner
{
    public class ResultsPanel : ResultListener
    {
        private readonly ListView _listView;

        private class MeasResult
        {
            public string Name { get; set; }
            public string Verdict { get; set; }
            public string Value { get; set; }
            public string Unit { get; set; }
            public string LowLimit { get; set; }
            public string HighLimit { get; set; }
            public string Comment { get; set; }
        }

        public ResultsPanel(ListView listView)
        {
            Name = nameof(ResultsPanel);
            _listView = listView;
        }

        public void Flush()
        {
            _listView.Dispatcher.BeginInvoke(new Action(() =>
            {
                _listView.Items.Clear();
            }));
        }

        public string GetContentOfSelectedItems()
        {
            var content = string.Empty;

            try
            {
                for (int i = 0; i < _listView.Items.Count; i++)
                {
                    var item = _listView.Items[i];
                    var listViewItem = item as ListViewItem;
                    if (listViewItem.IsSelected == true)
                    {
                        var measResult = listViewItem.Content as MeasResult;
                        content += String.Format("{0,-40} ; {1,-12} ; {2,-12} ;  {3,-12} ; {4,-12} ; {5,-12} ; {6,-12}\n",
                            measResult.Name,
                            measResult.Verdict,
                            measResult.Value,
                            measResult.Unit,
                            measResult.LowLimit,
                            measResult.HighLimit,
                            measResult.Comment);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex.ToString());
            }

            return content;
        }

        public override void OnTestPlanRunStart(TestPlanRun planRun)
        {
            Flush();
        }

        public override void OnResultPublished(Guid stepRun, ResultTable resultTable)
        {
            try
            {
                for (var resIndex = 0; resIndex < resultTable.Rows; resIndex++)
                {
                    // Check for formatted results
                    if (resultTable.Columns.Length == 7)
                    {
                        var verdict = resultTable.Columns[0].Data.GetValue(resIndex).ToString().ToUpper();
                        var name = resultTable.Columns[1].Data.GetValue(resIndex).ToString();
                        var unit = resultTable.Columns[2].Data.GetValue(resIndex).ToString();
                        var value = resultTable.Columns[3].Data.GetValue(resIndex).ToString();
                        var lowLimit = resultTable.Columns[4].Data.GetValue(resIndex).ToString();
                        var highLimit = resultTable.Columns[5].Data.GetValue(resIndex).ToString();
                        var comment = resultTable.Columns[6].Data.GetValue(resIndex).ToString();

                        _listView.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var listViewItem = new ListViewItem
                            {
                                Content = new MeasResult
                                {
                                    Name = name,
                                    Verdict = verdict,
                                    Value = value,
                                    Unit = unit,
                                    LowLimit = lowLimit,
                                    HighLimit = highLimit,
                                    Comment = comment,
                                }
                            };

                            var logScrollbackLimit = 10000;
                            _listView.Items.Add(listViewItem);
                            if (_listView.Items.Count > logScrollbackLimit)
                            {
                                _listView.Items.RemoveAt(0);
                            }

                            // Automatic scroll if last item is in view
                            var scrollViewer = FindVisualChild<ScrollViewer>(_listView);
                            if (Math.Abs(scrollViewer.VerticalOffset - scrollViewer.ScrollableHeight) < 1)
                                scrollViewer.ScrollToBottom();

                        }));

                        OnActivity();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex.ToString());
            }
        }

        private static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            // Search immediate children first (breadth-first search)
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is childItem)
                    return (childItem)child;

                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);

                    if (childOfChild != null)
                        return childOfChild;
                }
            }

            return null;
        }
    }
}