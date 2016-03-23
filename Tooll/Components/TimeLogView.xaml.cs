// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Framefield.Core.Profiling;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for TimeLogView.xaml
    /// </summary>
    public partial class TimeLogView : UserControl
    {
        public TimeLogView() {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            TimeLogger.ChangedEvent += TimeLoggerChangedHandler;
            InvalidateVisual();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            TimeLogger.ChangedEvent -= TimeLoggerChangedHandler;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e) {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                _scaleHeight *= e.Delta > 0 ? 1.1 : 1.0/1.1;
            else
                _scaleWidth *= e.Delta > 0 ? 1.1 : 1.0/1.1;

            InvalidateVisual();
        }

        private void TimeLoggerChangedHandler(object o, EventArgs e) {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);

            int baseHight = 15;

            double pixelWidthDuration = 1.0/_scaleWidth;
            double pixelHeightDuration = 1.0/_scaleHeight;

            Matrix m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
            double dpiFactor = 1/m.M11;
            double penWidth = 1.0/dpiFactor;
            double halfPenWidth = penWidth*0.5;

            GuidelineSet guidelines = new GuidelineSet();

            guidelines.GuidelinesX.Add(0 - halfPenWidth);
            guidelines.GuidelinesX.Add(ActualWidth + halfPenWidth);
            guidelines.GuidelinesY.Add(0 - halfPenWidth);
            guidelines.GuidelinesY.Add(ActualHeight + halfPenWidth);
            dc.PushGuidelineSet(guidelines);

            var logData = TimeLogger.LogData.Values.ToList();

            if (logData.Count > 1) {
                int currentLogDataIndex = logData.Count - 1;

                Point point = new Point();
                Dictionary<string, KeyValuePair<Pen, Pen>> cachedPens = new Dictionary<string, KeyValuePair<Pen, Pen>>();

                for (int i = 0; i < ActualWidth; ++i) {
                    double timeBegin = TimeLogger.CurrentFrameTime - i*pixelWidthDuration;
                    double timeEnd = timeBegin - pixelWidthDuration;

                    if (timeEnd < logData.First().StartTime)
                        break;

                    while (logData[currentLogDataIndex - 1].StartTime > timeEnd &&
                           currentLogDataIndex > 1) {
                        --currentLogDataIndex;
                    }
                    int x = (int)(ActualWidth - (i + 1));

                    var frameData = logData[currentLogDataIndex];

                    point.X = x;

                    //display num rendered primities
                    point.Y = ActualHeight - baseHight - (ActualHeight - baseHight)*(double)frameData.RenderedPrimitives/1000000;
                    dc.DrawRectangle(Brushes.White, null, new Rect(point.X, point.Y, penWidth, 1.0));
                    dc.DrawRectangle(Brushes.Black, null, new Rect(point.X, point.Y + 1, penWidth, 1.0));

                    //display num occlusions
                    point.Y = ActualHeight - baseHight - (ActualHeight - baseHight)*(double)frameData.OcclusionCount/100000000;
                    dc.DrawRectangle(Brushes.GreenYellow, null, new Rect(point.X, point.Y, penWidth, 1.0));
                    dc.DrawRectangle(Brushes.Black, null, new Rect(point.X, point.Y + 1, penWidth, 1.0));

                    //display num sample times
                    var sampleSet = frameData.TimeBlocks;
                    for (int j = 0; j < sampleSet.Count; ++j) {
                        var sample = sampleSet[sampleSet.Count - 1 - j];
                        int startHeight = (int)(sample.FrameTimeOffset/pixelHeightDuration);
                        int endHeight = (int)((sample.FrameTimeOffset + sample.Duration)/pixelHeightDuration);

                        if (endHeight - startHeight < 1)
                            continue;

                        KeyValuePair<Pen, Pen> pens;
                        if (!cachedPens.TryGetValue(sample.ID, out pens)) {
                            SolidColorBrush br1 = new SolidColorBrush(Color.FromArgb((byte)(sample.Color.A*0.4), sample.Color.R, sample.Color.G, sample.Color.B));
                            SolidColorBrush br2 = new SolidColorBrush(Color.FromArgb(sample.Color.A, sample.Color.R, sample.Color.G, sample.Color.B));
                            br1.Freeze();
                            br2.Freeze();
                            pens = new KeyValuePair<Pen,Pen>(new Pen(br1, penWidth),
                                                             new Pen(br2, penWidth));
                            pens.Key.Freeze();
                            pens.Value.Freeze();
                            cachedPens.Add(sample.ID, pens);
                        }

                        point.Y = ActualHeight - baseHight - endHeight - 1;
                        dc.DrawRectangle(pens.Key.Brush, null, new Rect(point.X, point.Y, penWidth, endHeight - startHeight - 1));

                        point.Y = ActualHeight - baseHight - endHeight;
                        dc.DrawRectangle(pens.Value.Brush, null, new Rect(point.X, point.Y, penWidth, 0.5));
                    }
                }
            }


            Pen gridPen = new Pen(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), penWidth);
            gridPen.Freeze();

            for (int i = 0; i < ActualWidth; i += 100) {
                dc.DrawLine(gridPen, new Point(ActualWidth - i, ActualHeight - baseHight), new Point(ActualWidth - i, 0));

                double time = TimeLogger.CurrentFrameTime - i*pixelWidthDuration;
                FormattedText text = new FormattedText(String.Format("{0:0.0}s", time),
                                                       CultureInfo.GetCultureInfo("en-us"),
                                                       FlowDirection.LeftToRight,
                                                       new Typeface("Verdana"),
                                                       10,
                                                       Brushes.White);
                text.TextAlignment = TextAlignment.Right;
                if (i < ActualWidth - 70)
                    dc.DrawText(text, new Point(ActualWidth - i, ActualHeight - 10));
            }

            for (int i = 0; i < ActualHeight; i += 100) {
                dc.DrawLine(gridPen, new Point(0, ActualHeight - baseHight - i), new Point(ActualWidth, ActualHeight - baseHight - i));

                double time = i*pixelHeightDuration;
                FormattedText text = new FormattedText(String.Format("{0:0.0}ms", time*1000),
                                                       CultureInfo.GetCultureInfo("en-us"),
                                                       FlowDirection.LeftToRight,
                                                       new Typeface("Verdana"),
                                                       10,
                                                       Brushes.White);
                text.TextAlignment = TextAlignment.Left;
                dc.DrawText(text, new Point(0, ActualHeight - baseHight - i));
            }

            dc.Pop();
        }

        double _scaleWidth = 20.0; //horizontal pixel per second
        double _scaleHeight = 2000.0; //vertical pixel per second
    }
}
