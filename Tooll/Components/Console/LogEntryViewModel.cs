// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Framefield.Core;
using Framefield.Tooll.Helper;

namespace Framefield.Tooll.Components.Console
{
    public class LogEntryViewModel: PropertyChangedBase
    {
        public DateTime DateTime { get; set; }
        public string Message { get; set; }
        public LogEntry.EntryLevel Level { get; set; }
        public Guid Source { get; set; }
        public double TimeSincePredecessor { get; set; }

        public String TimeStampAsString
        {
            get
            {
                if (TimeSincePredecessor > 0)
                {
                    return (String.Format("+{0}", (int)(TimeSincePredecessor)));
                }
                else
                {
                    return DateTime.ToString("hh:mm:ss.fff"); 
                }
            }
        }

        public LogEntryViewModel(LogEntry logEntry)
        {
            _logEnty = logEntry;
            DateTime = _logEnty.TimeStamp;
            Message = Regex.Replace(_logEnty.Message, @"\n$", "");
            Level = logEntry.Level;
            Source = logEntry.Source;
        }

        private LogEntry _logEnty;
    }
}
