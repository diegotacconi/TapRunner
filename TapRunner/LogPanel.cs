﻿using System;
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

        private class LogEvent
        {
            public string Timestamp { get; set; }
            public string Source { get; set; }
            public string Message { get; set; }
        }

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
            _listView.Dispatcher.BeginInvoke(new Action(() =>
            {
                _listView.Items.Clear();
            }));
        }

        public string GetContentOfSelectedItems()
        {
            var content = string.Empty;
            for (int i = 0; i < _listView.Items.Count; i++)
            {
                var item = _listView.Items[i];
                var listViewItem = item as ListViewItem;
                if (listViewItem.IsSelected == true)
                {
                    var logEvent = listViewItem.Content as LogEvent;
                    content += String.Format("{0,-12} ; {1,-14} ; {2}\n",
                        logEvent.Timestamp,
                        logEvent.Source,
                        logEvent.Message);
                }
            }

            return content;
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

                    var logScrollbackLimit = 10000;
                    _listView.Items.Add(listViewItem);
                    if (_listView.Items.Count > logScrollbackLimit)
                    {
                        _listView.Items.RemoveAt(0);
                    }

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