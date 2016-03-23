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

namespace Still.Core
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
        public double PrivateMemory { get; set; }
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

        public static List<FrameData> LogData { get; private set; }
        public static double CurrentFrameTime { get; private set; }
        public static bool IsWithinFrame { get; private set; }
        public static List<float> FPSHistogram { get; private set; }
        public static int FPSOverflows { get; private set; }

        static TimeLogger()
        {
            _queryOcclusion = new Query(D3DDevice.Device, new QueryDescription() { Type = QueryType.Occlusion });
            _queryPipelineStats = new Query(D3DDevice.Device, new QueryDescription() { Type = QueryType.PipelineStatistics });
            _queryTimeStampDisjoint = new Query(D3DDevice.Device, new QueryDescription() { Type = QueryType.TimestampDisjoint });
            _queryTimeStampFrameBegin = new Query(D3DDevice.Device, new QueryDescription() { Type = QueryType.Timestamp });
            _queryTimeStampFrameEnd = new Query(D3DDevice.Device, new QueryDescription() { Type = QueryType.Timestamp });

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += HandleTimedEvent;
            _timer.AutoReset = true;

            Clear();
            _timer.Start();
        }

        public static void Clear()
        {
            LogData = new List<FrameData>();
            CurrentFrameTime = 0.0;
            IsWithinFrame = false;

            FPSHistogram = new List<float>();
            for (int i = 0; i < 80; ++i)
                FPSHistogram.Add(0.0f);
            FPSOverflows = 0;
        }

        public static void BeginFrame(double frameTime)
        {
            if (frameTime <= CurrentFrameTime)
                Clear();

            int oneHourDurationIdx = 0;
            foreach (FrameData frame in LogData)
            {
                if (frame.StartTime < frameTime - 3600)
                    ++oneHourDurationIdx;
                else
                    break;
            }
            if (oneHourDurationIdx > 0)
                LogData.RemoveRange(0, oneHourDurationIdx);

            D3DDevice.Device.ImmediateContext.Begin(_queryOcclusion);
            D3DDevice.Device.ImmediateContext.Begin(_queryPipelineStats);
            D3DDevice.Device.ImmediateContext.Begin(_queryTimeStampDisjoint);
            D3DDevice.Device.ImmediateContext.End(_queryTimeStampFrameBegin);

            CurrentFrameTime = frameTime;

            long memoryUsed = _currentProc.PrivateMemorySize64;

            var frameData = new FrameData() { StartTime = CurrentFrameTime, TimeBlocks = new List<DataEntry>(), PrivateMemory = (double)memoryUsed };
            LogData.Add(frameData);

            IsWithinFrame = true;
        }

        public static void EndFrame()
        {
            IsWithinFrame = false;

            var context = D3DDevice.Device.ImmediateContext;
            context.End(_queryTimeStampFrameEnd);
            context.End(_queryTimeStampDisjoint);
            context.End(_queryPipelineStats);
            context.End(_queryOcclusion);

            QueryDataTimestampDisjoint disjointData;
            long timeStampframeBegin;
            long timeStampframeEnd;
            QueryDataPipelineStatistics pipelineStatsData;
            UInt64 occlusionCount;
            while (!context.GetData(_queryTimeStampFrameBegin, AsynchronousFlags.None, out timeStampframeBegin)) ;
            while (!context.GetData(_queryTimeStampFrameEnd, AsynchronousFlags.None, out timeStampframeEnd)) ;
            while (!context.GetData(_queryTimeStampDisjoint, AsynchronousFlags.None, out disjointData)) ;
            while (!context.GetData(_queryPipelineStats, AsynchronousFlags.None, out pipelineStatsData)) ;
            while (!context.GetData(_queryOcclusion, AsynchronousFlags.None, out occlusionCount)) ;

            if (LogData.Count == 0 || disjointData.Disjoint)
                return;

            DataEntry entry = new DataEntry()
                                  {
                                      ID = _queryTimeStampDisjoint.GetHashCode().ToString(),
                                      Name = "TotalFrameTime",
                                      Color = System.Drawing.Color.FromArgb(100, 255, 255, 255),
                                      Duration = (double)(timeStampframeEnd - timeStampframeBegin)/disjointData.Frequency,
                                      FrameTimeOffset = 0
                                  };
            FrameData frame = LogData[LogData.Count - 1];
            frame.TimeBlocks.Add(entry);
            frame.RenderedPrimitives = pipelineStatsData.CPrimitiveCount;
            frame.PrimitivesSentToRasterizer = pipelineStatsData.CInvocationCount;
            frame.OcclusionCount = occlusionCount;
            LogData[LogData.Count - 1] = frame;

            float fps = (float)(1.0/entry.Duration);
            if (fps < (float)FPSHistogram.Count)
                FPSHistogram[(int)fps]++;
            else
                FPSOverflows++;

            if (_logNextEndFrameEnabled)
            {
                Logger.Debug("fps: {0:0000.00}, mem: {1:0000}kb", fps, frame.PrivateMemory/1024);
                _logNextEndFrameEnabled = false;
            }

            if (ChangedEvent != null)
                ChangedEvent(null, EventArgs.Empty);
        }

        public static void Add(DataEntry entry)
        {
            if (IsWithinFrame)
                LogData.Last().TimeBlocks.Add(entry);
        }

        private static void HandleTimedEvent(object source, ElapsedEventArgs e)
        {
            _logNextEndFrameEnabled = true;
        }

        private static Query _queryOcclusion;
        private static Query _queryPipelineStats;
        private static Query _queryTimeStampDisjoint;
        private static Query _queryTimeStampFrameBegin;
        private static Query _queryTimeStampFrameEnd;
        private static Timer _timer = new Timer();
        private static bool _logNextEndFrameEnabled = false;
        private static Process _currentProc = Process.GetCurrentProcess();
    }
}
