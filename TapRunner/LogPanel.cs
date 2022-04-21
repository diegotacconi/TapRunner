using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenTap;
using OpenTap.Diagnostic;

namespace TapRunner
{
    public class LogPanel : TraceListener
    {
        private static long _globalTimer = DateTime.Now.Ticks;
        private readonly ListView _listView;

        public static void SetStartupTime(DateTime time)
        {
            _globalTimer = time.Ticks;
        }

        public LogPanel(ListView listView)
        {
            _listView = listView;
        }

        public override void Flush()
        {
            base.Flush();
            _listView.Items.Clear();
        }

        private static Brush GetColorForTraceLevel(LogEventType eventType)
        {
            switch (eventType)
            {
                case LogEventType.Debug:
                    return Brushes.Gray; // return (SolidColorBrush)(new BrushConverter().ConvertFrom("#99999B"));
                case LogEventType.Information:
                    return Brushes.Black; // return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFFFF"));
                case LogEventType.Warning:
                    return Brushes.DarkOrange; // return (SolidColorBrush)(new BrushConverter().ConvertFrom("#E67E22"));
                case LogEventType.Error:
                    return Brushes.DarkRed; // return (SolidColorBrush)(new BrushConverter().ConvertFrom("#E74C3C"));
                default:
                    return Brushes.Red; // return (SolidColorBrush)(new BrushConverter().ConvertFrom("#E74C3C"));
            }
        }

        private void InternalTraceEvent(LogEventType eventType, long timestamp, string source, string message)
        {
            // var time = new TimeSpan(timestamp);
            var time = new TimeSpan(timestamp - _globalTimer);
            _listView.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var listViewItem = new ListViewItem
                    {
                        Foreground = GetColorForTraceLevel(eventType),
                        Content = new LogEvent
                        {
                            Timestamp = $"{time:hh\\:mm\\:ss\\.fff}",
                            Source = source,
                            Message = message.TrimEnd('\r', '\n')
                        }
                    };

                    _listView.Items.Add(listViewItem);

                    // Automatic scroll if last item is in view
                    var scrollViewer = FindVisualChild<ScrollViewer>(_listView);

                    if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                    {
                        scrollViewer.ScrollToBottom();
                    }

                    /*
                    // Scroll ListView to end
                    _listView.Items.MoveCurrentToLast();
                    _listView.ScrollIntoView(_listView.Items.CurrentItem);
                    */
                })
            );
        }

        private class LogEvent
        {
            public string Timestamp { get; set; }
            public string Source { get; set; }
            public string Message { get; set; }
        }

        public override void TraceEvents(IEnumerable<Event> events)
        {
            foreach (var evt in events)
                InternalTraceEvent((LogEventType) evt.EventType, evt.Timestamp, evt.Source, evt.Message);
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