// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using AvalonDock;
using Framefield.Core;
using System.ComponentModel;

namespace Framefield.Tooll.Components.Console
{
    /// <summary>
    /// Interaction logic for ConsoleView.xaml
    /// </summary>
    public partial class ConsoleView : DocumentContent
    {
        public ICollectionView EntryCollection;

        private LogEntry.EntryLevel _logLevel = 0;

        public bool ScrollingNeedsUpdate { get; set; }
        public bool ListNeedsRefresh { get; set; }
        public bool NeedsUpdate { get; set; }
        public string FilterString { get; set; }

        public ConsoleView()
        {
            InitializeComponent();
            Title = "Console";
            FilterString = "";
            App.Current.ConsoleViewWriter.LogEntries.CollectionChanged += LogEntries_CollectionChanged;

            EntryCollection = CollectionViewSource.GetDefaultView(App.Current.ConsoleViewWriter.LogEntries);
            DataContext = EntryCollection;


            var binding = new Binding("FilterString");
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = this;
            binding.Path = new PropertyPath("FilterString");
            XFilterStringInput.SetBinding(TextBox.TextProperty, binding);

            EntryCollection.Filter = item =>
                                     {
                                         var entry = item as LogEntryViewModel;
                                         var valid = entry != null && _logLevel.HasFlag(entry.Level);

                                         if (!String.IsNullOrEmpty(FilterString))
                                             valid &= entry.Message.Contains(FilterString);

                                         return valid;
                                     };
            UpdateLogLevel();
        }

        private void UpdateScrolling() 
        {
            if (XScrolling.IsChecked == true)
            {
                if (_scrollViewer == null && XItems != null)
                {
                    _scrollViewer = FindVisualChild<ScrollViewer>(XItems);
                }
                if (_scrollViewer != null)
                    _scrollViewer.ScrollToBottom();
            }            
        }

        private ScrollViewer _scrollViewer;
        void LogEntries_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e != null && e.NewItems != null && e.NewItems.Count > 0)
            {
                UpdateScrolling();
            } 
        }

        #region internal helper method
        private TChildItem FindVisualChild<TChildItem>(DependencyObject obj) where TChildItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is TChildItem)
                {
                    return (TChildItem)child;
                }
                var childOfChild = FindVisualChild<TChildItem>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
        #endregion

        private void ClickHandler(object sender, RoutedEventArgs e)
        {
            UpdateLogLevel();
        }

        private void UpdateLogLevel()
        {
            _logLevel = ((XDBG.IsChecked == true ? LogEntry.EntryLevel.DBG : 0)
                         | (XINF.IsChecked == true ? LogEntry.EntryLevel.INF : 0)
                         | (XWRN.IsChecked == true ? LogEntry.EntryLevel.WRN : 0)
                         | (XERR.IsChecked == true ? LogEntry.EntryLevel.ERR : 0));
            EntryCollection.Refresh();
            UpdateScrolling();
        }

        private void XClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            App.Current.ConsoleViewWriter.LogEntries.Clear();
        }

        private void XFilterStringInput_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            EntryCollection.Refresh();
            UpdateScrolling();
        }
    }


    #region Value converter
    [ValueConversion(typeof(int), typeof(SolidColorBrush))]
    public class ErrorLevelToBrushConverter : IValueConverter
    {
        static  SolidColorBrush debugBrush = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80));
        static  SolidColorBrush infoBrush = new SolidColorBrush(Color.FromArgb(255, 120, 200, 120));
        static SolidColorBrush warnBrush = new SolidColorBrush(Color.FromArgb(255, 245, 169, 56));
        static SolidColorBrush errorBrush = new SolidColorBrush(Color.FromArgb(255, 255, 20, 20));
        
        public ErrorLevelToBrushConverter()
        {
            debugBrush.Freeze();
            infoBrush.Freeze();
            warnBrush.Freeze();
            errorBrush.Freeze();
        }


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var v = (int)value;
            switch (v)
            {
                case (int)LogEntry.EntryLevel.DBG: return debugBrush;
                case (int)LogEntry.EntryLevel.INF: return infoBrush;
                case (int)LogEntry.EntryLevel.WRN: return warnBrush;
                case (int)LogEntry.EntryLevel.ERR: return errorBrush; 
                default: return infoBrush; 
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
