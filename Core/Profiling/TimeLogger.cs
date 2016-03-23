// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Timers;
using SharpDX;
using SharpDX.Direct3D11;

namespace Framefield.Core.Profiling
{
    public struct DataEntry
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public double Duration { get; set; }
        public double FrameTimeOffset { get; set; }
        public System.Drawing.Color Color { get; set; }
    }

    public struct FrameData
    {
        public double StartTime { get; set; }
        public List<DataEntry> TimeBlocks { get; set; }
        public Int64 PrivateMemory { get; set; }
        public Int64 RenderedPrimitives { get; set; }
        public Int64 PrimitivesSentToRasterizer { get; set; }
        public UInt64 OcclusionCount { get; set; }
    }

    /**
     * Collects Log messages in a ring buffer which can later be visualized by TimeLogView
     */
    public static class TimeLogger
    {
        public static event EventHandler<System.EventArgs> ChangedEvent;

        public static bool Enabled { get; set; }

        public static Dictionary<Int64, FrameData> LogData 
        {
            get
            {
                Dictionary<Int64, FrameData> copiedLogData;
                lock (_lock)
                {
                    copiedLogData = new Dictionary<Int64, FrameData>(_logData);
                }
                return copiedLogData;
            }
        }

        public static int FrameCount
        {
            get
            {
                int count;
                lock (_lock)
                {
                    count = _logData.Count;
                }
                return count;
            }
        }

        public static KeyValuePair<Int64, FrameData> FirstFrame
        {
            get
            {
                KeyValuePair<Int64, FrameData> frame;
                lock (_lock)
                {
                    frame = _logData.FirstOrDefault();
                }
                return frame;
            }
        }

        public static KeyValuePair<Int64, FrameData> LastFrame
        {
            get
            {
                KeyValuePair<Int64, FrameData> frame;
                lock (_lock)
                {
                    frame = _logData.LastOrDefault();
                }
                return frame;
            }
        }

        public static Int64 FrameID { get; private set; }
        public static double CurrentFrameTime { get; private set; }
        public static bool IsWithinFrame { get; private set; }
        public static List<float> FPSHistogram { get; private set; }
        public static int FPSOverflows { get; private set; }

        static TimeLogger()
        {
            Enabled = false;
            _measureElements = new MeasureElement[_maxQueryFrames];
            for (int i = 0; i < _maxQueryFrames; ++i)
            {
                _measureElements[i] = new MeasureElement
                                      {
                                          FrameID = 0,
                                          QueryOcclusion = new GPUQuery(D3DDevice.Device, new QueryDescription() { Type = QueryType.Occlusion }),
                                          QueryPipelineStats = new GPUQuery(D3DDevice.Device, new QueryDescription() { Type = QueryType.PipelineStatistics }),
                                          QueryTimeStampDisjoint = new GPUQuery(D3DDevice.Device, new QueryDescription() { Type = QueryType.TimestampDisjoint }),
                                          QueryTimeStampFrameBegin = new GPUQuery(D3DDevice.Device, new QueryDescription() { Type = QueryType.Timestamp }),
                                          QueryTimeStampFrameEnd = new GPUQuery(D3DDevice.Device, new QueryDescription() { Type = QueryType.Timestamp })
                                      };
            }

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += HandleTimedEvent;
            _timer.AutoReset = true;

            Clear();
            _timer.Start();
        }

        public static KeyValuePair<Int64, FrameData> GetLastNthFrame(int n)
        {
            int i = 0;
            KeyValuePair<Int64, FrameData> frame = new KeyValuePair<Int64, FrameData>();
            lock (_lock)
            {
                foreach (var e in _logData)
                {
                    if (i >= _logData.Count - 1 - n)
                    {
                        frame = e;
                        break;
                    }
                    ++i;
                }
            }
            return frame;
        }

        public static void Clear()
        {
            IsWithinFrame = false;

            lock (_lock)
            {
                _logData.Clear();
            }
            FrameID = 0;
            CurrentFrameTime = 0.0;

            _currentQueryFrame = 0;
            _allQueriesValid = false;

            FPSHistogram = new List<float>();
            for (int i = 0; i < 80; ++i)
                FPSHistogram.Add(0.0f);
            FPSOverflows = 0;
        }

        public static void BeginFrame(double frameTime)
        {
            if (!Enabled)
                return;

            if (frameTime < CurrentFrameTime - Constants.Epsilon)
                Clear();

            CurrentFrameTime = frameTime;

            lock (_lock)
            {
                _logData = _logData.Where(f => f.Value.StartTime > frameTime - 3600).ToDictionary(f => f.Key, f => f.Value);
                _logData[FrameID] = new FrameData()
                                    {
                                        StartTime = CurrentFrameTime,
                                        TimeBlocks = new List<DataEntry>(),
                                        PrivateMemory = _currentProc.PrivateMemorySize64
                                    };
            }

            var context = D3DDevice.Device.ImmediateContext;
            var measureElement = _measureElements[_currentQueryFrame];
            measureElement.FrameID = FrameID;
            measureElement.QueryOcclusion.Begin(context);
            measureElement.QueryPipelineStats.Begin(context);
            measureElement.QueryTimeStampDisjoint.Begin(context);
            measureElement.QueryTimeStampFrameBegin.End(context);

            IsWithinFrame = true;
        }

        public static void EndFrame()
        {
            if (!Enabled)
                return;

            IsWithinFrame = false;

            var context = D3DDevice.Device.ImmediateContext;
            var measureElement = _measureElements[_currentQueryFrame];
            measureElement.QueryTimeStampFrameEnd.End(context);
            measureElement.QueryTimeStampDisjoint.End(context);
            measureElement.QueryPipelineStats.End(context);
            measureElement.QueryOcclusion.End(context);

            int oldestQueryFrame = (_currentQueryFrame + 1) % _maxQueryFrames;
            ++_currentQueryFrame;
            if (_currentQueryFrame == _maxQueryFrames)
                _allQueriesValid = true;

            _currentQueryFrame %= _maxQueryFrames;

            if (_allQueriesValid)
            {
                var measureElementToFetch = _measureElements[oldestQueryFrame];
                QueryDataTimestampDisjoint disjointData;
                long timeStampframeBegin;
                long timeStampframeEnd;
                QueryDataPipelineStatistics pipelineStatsData;
                UInt64 occlusionCount;
                bool dataFetched = true;
                dataFetched &= measureElementToFetch.QueryTimeStampDisjoint.GetData(context, AsynchronousFlags.None, out disjointData);
                dataFetched &= measureElementToFetch.QueryTimeStampFrameBegin.GetData(context, AsynchronousFlags.None, out timeStampframeBegin);
                dataFetched &= measureElementToFetch.QueryTimeStampFrameEnd.GetData(context, AsynchronousFlags.None, out timeStampframeEnd);
                dataFetched &= measureElementToFetch.QueryPipelineStats.GetData(context, AsynchronousFlags.None, out pipelineStatsData);
                dataFetched &= measureElementToFetch.QueryOcclusion.GetData(context, AsynchronousFlags.None, out occlusionCount);

                if (dataFetched && !disjointData.Disjoint)
                {
                    DataEntry entry = new DataEntry()
                                          {
                                              ID = _lock.GetHashCode().ToString(),
                                              Name = "TotalFrameTime",
                                              Color = System.Drawing.Color.FromArgb(100, 255, 255, 255),
                                              Duration = (double)(timeStampframeEnd - timeStampframeBegin)/disjointData.Frequency,
                                              FrameTimeOffset = 0
                                          };

                    float fps = (float)(1.0/entry.Duration);
                    Int64 privateMem = 0;
                    lock (_lock)
                    {
                        FrameData frame;
                        if (_logData.TryGetValue(measureElementToFetch.FrameID, out frame))
                        {
                            frame.TimeBlocks.Add(entry);
                            frame.RenderedPrimitives = pipelineStatsData.CPrimitiveCount;
                            frame.PrimitivesSentToRasterizer = pipelineStatsData.CInvocationCount;
                            frame.OcclusionCount = occlusionCount;
                            privateMem = frame.PrivateMemory;
                            _logData[measureElementToFetch.FrameID] = frame;                            
                        }
                    }

                    if (fps < (float)FPSHistogram.Count)
                        FPSHistogram[(int)fps]++;
                    else
                        FPSOverflows++;

                    if (_logNextEndFrameEnabled)
                    {
                        Logger.Debug("fps: {0:0000.00}, mem: {1:0000}kb", fps, privateMem/1024);
                        _logNextEndFrameEnabled = false;
                    }
                }
            }

            FrameID++;
            if (ChangedEvent != null)
                ChangedEvent(null, new EventArgs());
        }

        public static void Add(DataEntry entry)
        {
            if (IsWithinFrame)
                Add(FrameID, entry);
        }

        public static void Add(Int64 frameID, DataEntry entry)
        {
            if (!Enabled)
                return;

            lock (_lock)
            {
                FrameData frameData;
                if (_logData.TryGetValue(frameID, out frameData))
                {
                    frameData.TimeBlocks.Add(entry);
                    _logData[frameID] = frameData;
                }
            }
        }

        static void HandleTimedEvent(object source, ElapsedEventArgs e)
        {
            _logNextEndFrameEnabled = true;
        }

        public class MeasureElement : IDisposable
        {
            public Int64 FrameID { get; set; }
            public GPUQuery QueryOcclusion { get; set; }
            public GPUQuery QueryPipelineStats { get; set; }
            public GPUQuery QueryTimeStampDisjoint { get; set; }
            public GPUQuery QueryTimeStampFrameBegin { get; set; }
            public GPUQuery QueryTimeStampFrameEnd { get; set; }

            public void Dispose()
            {
                FrameID = 0;
                QueryOcclusion.Dispose();
                QueryPipelineStats.Dispose();
                QueryTimeStampDisjoint.Dispose();
                QueryTimeStampFrameBegin.Dispose();
                QueryTimeStampFrameEnd.Dispose();
            }
        }


        static Object _lock = new Object();
        static Dictionary<Int64, FrameData> _logData = new Dictionary<Int64, FrameData>();
        static int _currentQueryFrame;
        static int _maxQueryFrames = 5;
        static bool _allQueriesValid = false;
        static MeasureElement[] _measureElements;
        static Timer _timer = new Timer();
        static bool _logNextEndFrameEnabled = false;
        static Process _currentProc = Process.GetCurrentProcess();
    }
}
