// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Threading;

namespace Framefield.Core
{
    public class LogEntry
    {
        [Flags]
        public enum EntryLevel
        {
            DBG = 1,
            INF = 2,
            WRN = 4,
            ERR = 8,
            ALL = DBG | INF | WRN | ERR
        }

        public DateTime TimeStamp { get; private set; }
        public EntryLevel Level { get; private set; }
        public String Message { get; private set; }
        public Guid Source { get; private set; }

        public LogEntry(EntryLevel level, Guid source, String message)
        {
            TimeStamp = DateTime.Now;
            Level = level;
            Source = source;
            Message = message;
        }

        public LogEntry(EntryLevel level, String message)
        {
            TimeStamp = DateTime.Now;
            Level = level;
            Message = message;
        }

        public LogEntry(EntryLevel level, String message, DateTime timeStamp)
        {
            TimeStamp = timeStamp;
            Level = level;
            Message = message;
        }

        /**
         * Special method to clone an existing entry with a new lineMessage.
         * This is required for implementing splitting multiline-messages
         */

        public LogEntry(LogEntry original, String lineMessage)
        {
            TimeStamp = original.TimeStamp;
            Level = original.Level;
            Message = lineMessage;
            Source = original.Source;
        }

        public List<LogEntry> SplitIntoSingleLineEntries()
        {
            var result = new List<LogEntry>();
            foreach (var line in Message.Replace("\r", "").Split('\n'))
            {
                result.Add(new LogEntry(this, line));
            }
            return result;
        }
    }

    public interface ILogWriter : IDisposable
    {
        LogEntry.EntryLevel Filter { get; set; }
        void Process(LogEntry entry);
    }


    public class Logger
    {
        private Logger()
        {
        }

        public static void Initialize(Dispatcher dispatcher)
        {
            _instance._mainThreadDispatcher = dispatcher;
        }

        private Dispatcher _mainThreadDispatcher;

        public static void Dispose()
        {
            foreach (var w in _instance._logWriter)
            {
                w.Dispose();
            }
        }

        public static void AddWriter(ILogWriter writer)
        {
            _instance._logWriter.Add(writer);
        }

        public static void Debug(String message, params object[] args)
        {
            var messageString = FormatMessageWithArguments(message, args);
            Log(new LogEntry(LogEntry.EntryLevel.DBG, messageString));
        }

        public static void Debug(OperatorPart.Function func, String format, params object[] args)
        {
            var idAndName = TryGettingIdAndName(func);
            var messageString = FormatMessageWithArguments(format, args);
            Log(new LogEntry(LogEntry.EntryLevel.DBG, idAndName.Item1, idAndName.Item2 + "» " + messageString));
        }

        public static void Info(String message, params object[] args)
        {
            var messageString = FormatMessageWithArguments(message, args);
            Log(new LogEntry(LogEntry.EntryLevel.INF, messageString));
        }


        public static void Info(OperatorPart.Function func, String format, params object[] args)
        {
            var idAndName = TryGettingIdAndName(func);
            var messageString = FormatMessageWithArguments(format, args);
            Log(new LogEntry(LogEntry.EntryLevel.INF, idAndName.Item1, idAndName.Item2 + "» " + messageString));
        }


        private const int DEFAULT_LINE_LENGTH = 100;
        static StringBuilder _accumulatedInfoLine = new StringBuilder(String.Empty, DEFAULT_LINE_LENGTH);
        public static void AccumulateAsInfoLine(String c, int lineLength = DEFAULT_LINE_LENGTH)
        {
            _accumulatedInfoLine.Append(c);
            if (_accumulatedInfoLine.Length > lineLength)
            {
                Logger.Info(_accumulatedInfoLine.ToString());
                _accumulatedInfoLine.Clear();
            }
        }

        public static void Warn(String message, params object[] args)
        {
            var messageString = FormatMessageWithArguments(message, args);
            Log(new LogEntry(LogEntry.EntryLevel.WRN, messageString));
        }

        public static void Warn(OperatorPart.Function func, String format, params object[] args)
        {
            var idAndName = TryGettingIdAndName(func);
            var messageString = FormatMessageWithArguments(format, args);
            Log(new LogEntry(LogEntry.EntryLevel.WRN, idAndName.Item1, idAndName.Item2 + "» " + messageString));
        }

        public static void Error(String message, params object[] args)
        {
            var messageString = FormatMessageWithArguments(message, args);
            Log(new LogEntry(LogEntry.EntryLevel.ERR, messageString));
        }

        public static void Error(OperatorPart.Function func, String format, params object[] args)
        {
            var idAndName = TryGettingIdAndName(func);
            var messageString = FormatMessageWithArguments(format, args);
            Log(new LogEntry(LogEntry.EntryLevel.ERR, idAndName.Item1, idAndName.Item2 + "» " + messageString));
        }


        private static string FormatMessageWithArguments(string messageString, object[] args)
        {
            try
            {
                messageString = args.Length == 0 ? messageString : String.Format(messageString, args);
            }
            catch (FormatException)
            {
                Log(new LogEntry(LogEntry.EntryLevel.INF, "Ignoring arguments mal-formated debug message. Did you mess with curly braces?"));
            }
            return messageString;
        }


        private static void Log(LogEntry.EntryLevel level, String message)
        {
            Log(new LogEntry(level, Guid.Empty, message));
        }

        private static void Log(LogEntry entry)
        {
            if (_instance._mainThreadDispatcher == null || _instance._mainThreadDispatcher.CheckAccess())
            {
                DoLog(entry);
            }
            else
            {
                Action action = () => DoLog(entry);
                _instance._mainThreadDispatcher.BeginInvoke(action, DispatcherPriority.Background);
            }
        }

        private static void DoLog(LogEntry entry)
        {
            _instance._logWriter.ForEach(writer => writer.Process(entry));
        }

        private static Tuple<Guid, string> TryGettingIdAndName(OperatorPart.Function func)
        {
            var result = new Tuple<Guid, string>(Guid.Empty, String.Empty);
            try
            {
                var parent = func.OperatorPart.Parent;
                result = new Tuple<Guid, string>(parent.ID, parent.ToString());
            }
            catch (Exception)
            {
                result = new Tuple<Guid, string>(Guid.Empty, "NOTFOUND");
            }
            return result;
        }

        private static Logger _instance = new Logger();
        private List<ILogWriter> _logWriter = new List<ILogWriter>();
    }


    public class ConsoleWriter : ILogWriter
    {
        public LogEntry.EntryLevel Filter { get; set; }

        public void Dispose()
        {
        }

        public void Process(LogEntry entry)
        {
            Console.Write("{0}: {1}", entry.Level.ToString(), entry.Message + "\n");
        }
    }

    public class FileWriter : ILogWriter
    {
        public LogEntry.EntryLevel Filter { get; set; }

        public FileWriter(String filename)
        {
            _fileWriter = new StreamWriter(filename);
#if DEBUG
            _fileWriter.AutoFlush = true;
#endif
        }

        public void Dispose()
        {
            _fileWriter.Flush();
            _fileWriter.Close();
            _fileWriter.Dispose();
        }

        public void Process(LogEntry entry)
        {
            _fileWriter.Write("{0} ({1}): {2}", entry.TimeStamp.ToString("HH:mm:ss.fff"), entry.Level.ToString(), entry.Message + "\n");
        }

        private readonly StreamWriter _fileWriter;
    }
}
