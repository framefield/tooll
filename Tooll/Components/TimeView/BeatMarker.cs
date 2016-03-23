// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using Framefield.Core;

namespace Framefield.Tooll
{
    public class BeatMarker : FrameworkElement, IValueSnapAttractor
    {
        public BeatMarker()
        {
            var bpmString = App.Current.ProjectSettings.GetOrSetDefault("Soundtrack.BPM", "120");
            BPM = Double.Parse(String.Format("{0}",bpmString));

            var bpmOffsetString = App.Current.ProjectSettings.GetOrSetDefault("Soundtrack.BPMOffset", "0.0");
            BPMTimeOffset = Double.Parse(String.Format("{0}", bpmOffsetString));

            //SetupTimeRulerDefinition();

            m_DrawingVisual = new DrawingVisual();
            m_Children.Add(m_DrawingVisual);

            this.AddVisualChild(m_DrawingVisual);
            this.AddLogicalChild(m_DrawingVisual);

            Loaded += OnLoaded;
        }

        // Provide m_TimeAnimation required override for the VisualChildrenCount property.
        protected override int VisualChildrenCount
        {
            get { return m_Children.Count; }
        }


        private double _bpm ;
        private double _bpmTimeOffset;

        public double BPM { get { return _bpm; } set { _bpm = value; SetupTimeRulerDefinition();} }
        public double BPMTimeOffset { get { return _bpmTimeOffset; } set { _bpmTimeOffset = value; SetupTimeRulerDefinition(); } }


        #region properties

        public static readonly DependencyProperty TimeScaleProperty = DependencyProperty.Register(
          "TimeScale",
            typeof(double),
            typeof(BeatMarker),
            new FrameworkPropertyMetadata(5.0,
            FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double TimeScale { get { return (double) GetValue(TimeScaleProperty); } set { SetValue(TimeScaleProperty, value); } }

        public static readonly DependencyProperty TimeOffsetProperty = DependencyProperty.Register(
          "TimeOffset",
            typeof(double),
            typeof(BeatMarker),
            new FrameworkPropertyMetadata(5.0,
            FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double TimeOffset { get { return (double) GetValue(TimeOffsetProperty); } set { SetValue(TimeOffsetProperty, value); } }

        #endregion


        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            using (DrawingContext dc = m_DrawingVisual.RenderOpen()) {
                DrawTimeTicks(dc, TimeScale, TimeOffset-BPMTimeOffset);
            }
        }

        #region setup
        private void SetupTimeRulerDefinition()
        {
            m_Fractions = new List<ScaleFraction> {
            // 0
            new ScaleFraction()
            { 
                ScaleMin= BPM / 1600,
                ScaleMax= BPM / 900,
                LineDefinitions=   new List<LineDefinition> {
                                        new LineDefinition() { Label="",  Height=200, Spacing= 64/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=200, Spacing= 16/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=200, Spacing= 1/BPM*60/4,      FadeLabels=false, FadeLines=false  },
                                    }
            },

            // 1
            new ScaleFraction()
            { 
                ScaleMin= BPM / 900,
                ScaleMax= BPM / 700,
                LineDefinitions=   new List<LineDefinition> {
                                        new LineDefinition() { Label="",  Height=200, Spacing= 64/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=200, Spacing= 16/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=200, Spacing= 4/BPM*60/4,      FadeLabels=false, FadeLines=true  },
                                    }
            },
                
                
            // 2
            new ScaleFraction()
            { 
                ScaleMin= BPM / 700,
                ScaleMax= BPM / 300,
                LineDefinitions=   new List<LineDefinition> {
                                        new LineDefinition() { Label="",  Height=200, Spacing= 64/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=200, Spacing= 16/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=false  },
                                    }
            },
            // 3
            new ScaleFraction()
            { 
                ScaleMin= BPM / 300,
                ScaleMax= BPM / 200,
                LineDefinitions=   new List<LineDefinition> {
                                        new LineDefinition() { Label="",  Height=200, Spacing= 64/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=200, Spacing= 16/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=true  },
                                    }
            },
            // 4
            new ScaleFraction()
            { 
                ScaleMin= BPM / 200,
                ScaleMax=  BPM / 100,
                LineDefinitions=   new List<LineDefinition> {
                                        new LineDefinition() { Label="",  Height=200, Spacing= 64/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=200, Spacing= 16/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=false  },
                                        //new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=true  },
                                    }
            },
            // 5
            new ScaleFraction()
            { 
                ScaleMin= BPM / 100,
                ScaleMax= BPM / 50,
                LineDefinitions=   new List<LineDefinition> {
                                        new LineDefinition() { Label="",  Height=200, Spacing= 64/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=200, Spacing= 16/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=true  },
                                        //new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=true  },
                                    }
            },
            // 6
            new ScaleFraction()
            { 
                ScaleMin= BPM / 50,
                ScaleMax= BPM / 20,
                LineDefinitions=   new List<LineDefinition> {
                                        new LineDefinition() { Label="",  Height=200, Spacing= 64/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=200, Spacing= 16/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        //new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=true  },
                                        //new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=true  },
                                    }
            },        
            // 7
            new ScaleFraction()
            { 
                ScaleMin= BPM / 20,
                ScaleMax= BPM / 8,
                LineDefinitions=   new List<LineDefinition> {
                                        new LineDefinition() { Label="",  Height=200, Spacing= 64/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        new LineDefinition() { Label="",  Height=200, Spacing= 16/BPM*60,      FadeLabels=false, FadeLines=true  },
                                        //new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=true  },
                                        //new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=true  },
                                    }
            },        
            // 8
            new ScaleFraction()
            { 
                ScaleMin= BPM / 8,
                ScaleMax= BPM / 1,
                LineDefinitions=   new List<LineDefinition> {
                                        new LineDefinition() { Label="",  Height=200, Spacing= 64/BPM*60,      FadeLabels=false, FadeLines=false  },
                                        //new LineDefinition() { Label="",  Height=200, Spacing= 16/BPM*60,      FadeLabels=false, FadeLines=true  },
                                        //new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=true  },
                                        //new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=true  },
                                    }
            },        

            };
        }
        #endregion

        Dictionary<int, double> UsedPositions= new Dictionary<int, double>();

        #region paint
        private void DrawTimeTicks(DrawingContext dc, double scale, double offset)
        {
            Pen pen = new Pen(new SolidColorBrush(Color.FromArgb((byte) 0, 0, 0, 0)), 1);
            pen.Freeze();

            if (scale > Constants.Epsilon) {

                double m_Width = ActualWidth;
                double m_Height = ActualHeight;

                const float density = 0.02f;

                UsedPositions.Clear();

                scale = 1 / scale;

                int f=0;
                while (f < m_Fractions.Count() - 1
                            && scale < m_Fractions[f+1].ScaleMin * density) {
                    f++;
                }
                for (f = 0; f < m_Fractions.Count() - 1 && scale > m_Fractions[f+1].ScaleMin * density; f++) {
                }
                /* DEBUG OUTPUT */
                //dc.DrawText(new FormattedText("Scale: " + (scale).ToString() + "  f=" +  f.ToString() + " ",
                //                                        CultureInfo.GetCultureInfo("en-us"),
                //                                        FlowDirection.LeftToRight,
                //                                        new Typeface("Verdana"),
                //                                        10,
                //                                        System.Windows.Media.Brushes.White
                //                                        ), new Point(0, 25));

               
                ScaleFraction frac = m_Fractions[f];


                foreach (LineDefinition linedef in frac.LineDefinitions) {


                    double t =  -offset % linedef.Spacing;
                    double fadeFactor = 0.0f;


                    fadeFactor = (scale - frac.ScaleMin * density) / (frac.ScaleMax  * density - frac.ScaleMin * density);
                    if (fadeFactor < 0) {
                        fadeFactor = 0.0;
                    }
                    else if (fadeFactor > 1) {
                        fadeFactor = 1.0;
                    }

                    SolidColorBrush fadebrush = new SolidColorBrush(Color.FromArgb((byte) (255-fadeFactor*255), 0, 0, 0));
                    fadebrush.Freeze();
                    if (linedef.FadeLines) {
                        pen = new Pen(fadebrush, 0.5);
                    }
                    else {
                        pen = new Pen(Brushes.Black, 0.5);
                    }
                    pen.Freeze();


                    while (t / scale < m_Width) {
                        int x = (int) (t / scale);

                        if (x > 0 && x < m_Width && !UsedPositions.ContainsKey(x)) {
                            UsedPositions[x] = t + offset + BPMTimeOffset;
                            //dc.DrawLine(pen, new Point(x+0.5, m_Height - linedef.Height), new Point(x+0.5, m_Height));
                            dc.DrawLine(pen, new Point(x+0.5, 0), new Point(x+0.5, m_Height));

                            if (linedef.Label != "") {
                                String output= "";
                                foreach (char c in linedef.Label) {

                                    if (c == 'F') {
                                        output += Math.Floor((float) (t + offset) * 30.0f %  30.0f).ToString("00");
                                    }
                                    else if (c == 'S') {
                                        output += Math.Floor((float) (t + offset) % 60.0f).ToString("00");
                                    }
                                    else if (c == 'M') {
                                        output += Math.Floor((float) (t + offset) / 60.0f %  60.0f).ToString("00");
                                    }
                                    else if (c == 'H') {
                                        output += Math.Floor((float) (t + offset) / (60.0*60.0) %  60.0).ToString("00");
                                    }
                                    else if (c == 'D') {
                                        output += Math.Floor((float) (t + offset) / (24*60.0*60.0) %  24).ToString("00");
                                    }
                                    else {
                                        output += c;
                                    }
                                }

                                Brush textbrush;
                                if (linedef.FadeLabels) {
                                    textbrush= fadebrush;
                                }
                                else {
                                    textbrush= Brushes.White;
                                }
                                FormattedText text= new FormattedText(output,
                                                                        CultureInfo.GetCultureInfo("en-us"),
                                                                        FlowDirection.LeftToRight,
                                                                        new Typeface("Verdana"),
                                                                        10,
                                                                        textbrush
                                                                        );
                                text.TextAlignment = TextAlignment.Center;
                                dc.DrawText(text, new Point(x, m_Height - 25));

                            }
                        }
                        t += linedef.Spacing;
                    }
                }
            }
        }
        #endregion


        #region internal stuff
        // Provide m_TimeAnimation required override for the GetVisualChild method.
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= m_Children.Count)
                throw new ArgumentOutOfRangeException();
            return m_Children[index];
        }
        #endregion


        #region implement snap attractor
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.CompositionView.XTimeView.TimeSnapHandler.AddSnapAttractor(this);            
        }


        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.CompositionView.XTimeView.TimeSnapHandler.RemoveSnapAttractor(this);
        }

        const double SNAP_THRESHOLD = 8;

        public SnapResult CheckForSnap(double time)
        {
            var TV= App.Current.MainWindow.CompositionView.XTimeView;

            foreach (var beatTime in UsedPositions.Values) {
                double distanceToTime = Math.Abs(time - beatTime) * TV.TimeScale;
                if (distanceToTime < SNAP_THRESHOLD) {
                    return new SnapResult() { SnapToValue=beatTime, Force=distanceToTime };
                }
            }

            return null;
        }
        #endregion

        private struct LineDefinition
        {
            public String Label { get; set; }
            public int Height { get; set; }
            public double Spacing { get; set; }
            public bool FadeLabels { get; set; }
            public bool FadeLines { get; set; }
        }

        private struct ScaleFraction
        {
            public double ScaleMin { get; set; }
            public double ScaleMax { get; set; }
            public List<LineDefinition> LineDefinitions { get; set; }
        }

        private List<DrawingVisual> m_Children = new List<DrawingVisual>();
        private List<ScaleFraction> m_Fractions = new List<ScaleFraction>();
        private DrawingVisual m_DrawingVisual;
    }
}
