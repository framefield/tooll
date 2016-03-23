// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Collections.ObjectModel;
using Framefield.Core;
using Framefield.Tooll.Helper;


namespace Framefield.Tooll.Components.Console
{
    public class ConsoleViewWriter : PropertyChangedBase, ILogWriter
    {
        public ConsoleViewWriter()
        {
            LogEntries = new ObservableCollection<LogEntryViewModel>();
            Filter = LogEntry.EntryLevel.ALL;
        }

        public LogEntry.EntryLevel Filter { get; set; }
        public ObservableCollection<LogEntryViewModel> LogEntries { get; set; }

        public void Dispose()
        {
            LogEntries.Clear();
        }

        static readonly int TIME_GROUP_THRESHOLD_IN_MS = 8;

        
        public void Process(LogEntry entry)
        {

            //foreach (var lineEntry in entry.SplitIntoSingleLineEntries())
            //{
                var newEntry = new LogEntryViewModel(entry);
                if (_referenceEntry == null)
                {
                    _previousEntry = _referenceEntry = newEntry;
                }
                else
                {
                    var deltaToReference = (newEntry.DateTime - _referenceEntry.DateTime).TotalMilliseconds;
                    var deltaToPrevious = (newEntry.DateTime - _previousEntry.DateTime).TotalMilliseconds;
                    if (deltaToPrevious > TIME_GROUP_THRESHOLD_IN_MS)
                    {
                        _referenceEntry = newEntry;
                        deltaToReference = 0;
                    }
                    newEntry.TimeSincePredecessor = deltaToReference;
                    _previousEntry = newEntry;
                }
                LogEntries.Add(newEntry);                
            //}        
        }

        LogEntryViewModel _previousEntry;
        LogEntryViewModel _referenceEntry;
    }
}
