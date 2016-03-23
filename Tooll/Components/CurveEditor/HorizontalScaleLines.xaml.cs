// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using Framefield.Core;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for HorizontalScaleLines.xaml
    /// </summary>
    public partial class HorizontalScaleLines : UserControl, IValueSnapAttractor
    {
        public HorizontalScaleLines()
        {
            InitializeComponent();
            m_DrawingVisual = new DrawingVisual();
            Loaded += OnLoaded;
            //Unloaded += OnUnloaded;
        }

        #region properties

        public static readonly DependencyProperty UScaleProperty = DependencyProperty.Register(
          "UScale",
            typeof(double),
            typeof(HorizontalScaleLines),
            new FrameworkPropertyMetadata(-100.0,
            FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double UScale { get { return (double) GetValue(UScaleProperty); } set { SetValue(UScaleProperty, value); } }


        public static readonly DependencyProperty UOffsetProperty = DependencyProperty.Register("UOffset", typeof(double), typeof(HorizontalScaleLines), new FrameworkPropertyMetadata(100.0,
      FrameworkPropertyMetadataOptions.AffectsRender));
        public double UOffset { get { return (double) GetValue(UOffsetProperty); } set { SetValue(UOffsetProperty, value); } }
        #endregion


        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double pixelsPerU= UScale;
            double offset= UOffset;

            DrawTimeTicks(drawingContext, pixelsPerU, offset);
            //xRenderUpdateViz.InvalidateVisual();
        }

        private SolidColorBrush GetTransparentBrush(double opacity)
        {
            double oClamped = Math.Min(1, Math.Max(0, opacity));
            var br = new SolidColorBrush(Color.FromArgb((byte) ((oClamped * 255)), 0, 0, 0));
            br.Freeze();
            return br;
        }

        private SolidColorBrush GetTransparentLabelBrush(double opacity)
        {
            double oClamped = Math.Min(1, Math.Max(0, opacity));
            var br = new SolidColorBrush(Color.FromArgb((byte) ((oClamped * 255)), 255, 255, 255));
            br.Freeze();
            return br;
        }

        private void DrawTimeTicks(DrawingContext dc, double pixelsPerU, double offset)
        {
            UsedPositions.Clear();
            if (pixelsPerU > Constants.Epsilon) {
                Dictionary<int, bool> usedPositions= new Dictionary<int, bool>();

                const double DENSITY = 0.8;
                double width = ActualWidth;
                double uPerPixel = 1 / pixelsPerU ;
                double logScale= Math.Log10(uPerPixel) + DENSITY;
                double logScaleMod= (logScale + 1000) % 1.0;
                double logScaleFloor= Math.Floor(logScale);

                var lineDefinitions=   new List<LineDefinition>();

                if (logScaleMod < 0.5) {
                    lineDefinitions.Add(new LineDefinition() {
                        Label="N",
                        Spacing=Math.Pow(10, logScaleFloor) * 50,
                        LabelOpacity= 1,
                        LineOpacity=1
                    });
                    lineDefinitions.Add(new LineDefinition() { 
                        Label="N",
                        Spacing=Math.Pow(10, logScaleFloor) * 10,
                        LabelOpacity= 1-logScaleMod * 2,
                        LineOpacity= 1-logScaleMod * 2 
                    });
                }
                else {
                    lineDefinitions.Add(new LineDefinition() {
                        Label="N",
                        Spacing=Math.Pow(10, logScaleFloor) * 100,
                        LabelOpacity=1.0,
                        LineOpacity=1.0
                    });
                    lineDefinitions.Add(new LineDefinition() {
                        Label="N",
                        Spacing=Math.Pow(10, logScaleFloor) * 50,
                        LabelOpacity=1- (logScaleMod - 0.5) * 2,
                        LineOpacity=1- (logScaleMod - 0.5) * 2
                    });
                }

                foreach (LineDefinition linedef in lineDefinitions) {
                    double t =  -offset % linedef.Spacing;

                    while (t / uPerPixel < width) {
                        double u = (t / uPerPixel);
                        double posX= u;
                        int x = (int) posX;

                        if (u > 0 && u < width && !UsedPositions.ContainsKey(x)) {
                            UsedPositions[x]= t+offset;
                            var pen = new Pen(GetTransparentBrush(linedef.LineOpacity * 0.3) , 1);
                            pen.Freeze();
                            dc.DrawLine(pen, new Point(posX,0 ), new Point(posX, ActualHeight));

                            if (linedef.Label != "") {
                                String output= "";
                                foreach (char c in linedef.Label) {
                                    if (c == 'N') {
                                        output +=  (t + offset).ToString("G7", CultureInfo.InvariantCulture);
                                    }
                                    else {

                                        output += c;
                                    }
                                }

                                FormattedText text= new FormattedText(output,
                                                                        CultureInfo.GetCultureInfo("en-us"),
                                                                        FlowDirection.LeftToRight,
                                                                        new Typeface("Verdana"),
                                                                        7,
                                                                        GetTransparentLabelBrush( linedef.LabelOpacity * 0.5)
                                                                        );
                                text.TextAlignment = TextAlignment.Left;
                                dc.DrawText(text, new Point(posX + 5, ActualHeight-12.0));

                            }
                        }
                        t += linedef.Spacing;
                    }
                }
            }
        }


        #region implement snap attractor
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //App.Current.MainWindow.CompositionView.XTimeView.m_SnapHandler.AddSnapAttractor(this);
        }


        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            //App.Current.MainWindow.CompositionView.XTimeView.m_SnapHandler.AddSnapAttractor(this);
        }

        const double SNAP_THRESHOLD = 8;

        public SnapResult CheckForSnap(double time)
        {
            //var TV= App.Current.MainWindow.CompositionView.XTimeView;
            if (this.Visibility == System.Windows.Visibility.Collapsed)
                return null;

            foreach (var beatTime in UsedPositions.Values) {
                double distanceToTime = Math.Abs(time - beatTime) * UScale;
                if (distanceToTime < SNAP_THRESHOLD) {
                    return new SnapResult() { SnapToValue=beatTime, Force=distanceToTime };
                }
            }

            return null;
        }
        #endregion

        private TimeView GetTimeView()
        {
            return UIHelper.FindParent<TimeView>(this);
        }

        private struct LineDefinition
        {
            public String Label { get; set; }
            public double Spacing { get; set; }
            public double LineOpacity { get; set; }
            public double LabelOpacity { get; set; }
        }

        Dictionary<int, double> UsedPositions= new Dictionary<int, double>();
        private List<DrawingVisual> m_Children = new List<DrawingVisual>();
        private DrawingVisual m_DrawingVisual;
    }


    
}
