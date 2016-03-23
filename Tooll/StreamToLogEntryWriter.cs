// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Framefield.Core;

namespace Framefield.Tooll
{
    public class StreamToLogEntryWriter : TextWriter
    {
        public override Encoding Encoding
        {
            get { return new UTF8Encoding(); }
        }

        public StreamToLogEntryWriter(LogEntry.EntryLevel level)
        {
            if (level == LogEntry.EntryLevel.INF)
                _logAction = (s) => Logger.Info(s);
            else if (level == LogEntry.EntryLevel.ERR)
                _logAction = (s) => Logger.Error(s);
        }

        public override void Write(string s)
        {
            _buffer += s;
            if (s.Contains('\n'))
            {
                _logAction(_buffer.Replace('\n', ' '));
                _buffer = String.Empty;
            }
        }

        public override void WriteLine(string s)
        {
            _buffer = String.Empty;
            _logAction(s);
        }

        Action<string> _logAction;
        string _buffer;
    }
}
