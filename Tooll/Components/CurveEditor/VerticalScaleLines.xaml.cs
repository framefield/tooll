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
    /// Interaction logic for VerticalScaleLines.xaml
    /// </summary>
    public partial class VerticalScaleLines : UserControl, IValueSnapAttractor
    {
        public VerticalScaleLines()
        {
            InitializeComponent();
            m_DrawingVisual = new DrawingVisual();
        }

        #region properties

        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
          "MinValue",
            typeof(double),
            typeof(VerticalScaleLines),
            new FrameworkPropertyMetadata(-100.0,
            FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double MinValue { get { return (double) GetValue(MinValueProperty); } set { SetValue(MinValueProperty, value); } }


        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(double), typeof(VerticalScaleLines), new FrameworkPropertyMetadata(100.0,
      FrameworkPropertyMetadataOptions.AffectsRender));
        public double MaxValue { get { return (double) GetValue(MaxValueProperty); } set { SetValue(MaxValueProperty, value); } }
        #endregion

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double pixelsPerValue= ActualHeight/(MaxValue-MinValue);
            double offset= MinValue;

            DrawValueTicks(drawingContext, pixelsPerValue, offset);
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

        private void DrawValueTicks(DrawingContext dc, double pixelsPerValue, double offset)
        {
            if (pixelsPerValue > Constants.Epsilon) {
                

                const double DENSITY = 0.8;
                double height = ActualHeight;
                double valuePerPixel = 1 / pixelsPerValue ;
                double logScale= Math.Log10(valuePerPixel) + DENSITY;
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

                _pixelRowsWithLines.Clear();

                foreach (LineDefinition linedef in lineDefinitions) {
                    double t =  -offset % linedef.Spacing;
                    
                    while (t / valuePerPixel < height) {
                        double value = (t / valuePerPixel);
                        double posY= height - value;
                        int v = (int) posY;

                        if (value > 0 && value < height && !_pixelRowsWithLines.ContainsKey(v)) {
                            _pixelRowsWithLines[v] = new VAndOpacity() { V = t + offset, Opacity = linedef.LineOpacity };
                            var pen = new Pen(GetTransparentBrush(linedef.LineOpacity * 0.3) , 1);
                            pen.Freeze();
                            dc.DrawLine(pen, new Point(0, posY), new Point(ActualWidth, posY));

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
                                dc.DrawText(text, new Point(2.0, posY - 5));
                            }
                        }
                        t += linedef.Spacing;
                    }
                }
            }
        }


        #region implement snap attractor
        const double SNAP_THRESHOLD = 5;

        public SnapResult CheckForSnap(double v)
        {
            foreach (var vAndOpacity in _pixelRowsWithLines.Values) {

                var lockValue = vAndOpacity.V;
                if (vAndOpacity.Opacity < 0.7)
                    continue;

                double distanceToValue = Math.Abs(v - lockValue) / ((MaxValue-MinValue)/ActualHeight); 
                if (distanceToValue < SNAP_THRESHOLD) {
                    return new SnapResult() { SnapToValue=lockValue, Force=distanceToValue };
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

        private struct VAndOpacity
        {
            public double V { get; set; }
            public double Opacity { get; set; }
        }

        readonly Dictionary<int, VAndOpacity> _pixelRowsWithLines = new Dictionary<int, VAndOpacity>();

        private List<DrawingVisual> m_Children = new List<DrawingVisual>();
        private DrawingVisual m_DrawingVisual;
    }
    
    
}
