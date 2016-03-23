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
    public class TimelineRuler : FrameworkElement
    {
        public TimelineRuler() {
            SetupTimeRulerDefinition();

            m_DrawingVisual = new DrawingVisual();
            m_Children.Add(m_DrawingVisual);

            this.AddVisualChild(m_DrawingVisual);
            this.AddLogicalChild(m_DrawingVisual);
        }

        // Provide m_TimeAnimation required override for the VisualChildrenCount property.
        protected override int VisualChildrenCount {
            get { return m_Children.Count; }
        }


        #region properties

        public static readonly DependencyProperty TimeScaleProperty = DependencyProperty.Register(
          "TimeScale",
            typeof(double),
            typeof(TimelineRuler),
            new FrameworkPropertyMetadata(5.0,
            FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double TimeScale { get { return (double) GetValue(TimeScaleProperty); } set { SetValue(TimeScaleProperty, value); } }

        public static readonly DependencyProperty TimeOffsetProperty = DependencyProperty.Register(
          "TimeOffset",
            typeof(double),
            typeof(TimelineRuler),
            new FrameworkPropertyMetadata(5.0,
            FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double TimeOffset { get { return (double) GetValue(TimeOffsetProperty); } set { SetValue(TimeOffsetProperty, value); } }

        #endregion


        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);

            using (DrawingContext dc = m_DrawingVisual.RenderOpen()) {
                DrawTimeTicks(dc, TimeScale, TimeOffset);
            }
        }

        #region setup
        private void SetupTimeRulerDefinition() {

            _FPSView= (float) App.Current.UserSettings.GetOrSetDefault("TimeLineFPSStyle",0 );
            if (_FPSView == 0) 
            {
                m_Fractions = new List<ScaleFraction> {
                    // 0
                    new ScaleFraction()
                    { 
                        ScaleMin= 1.0 / 60.0,
                        ScaleMax= 1.0 / 20.0,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="M:S:Ff",  Height=10,Spacing=60.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="S:Ff",    Height=5, Spacing= 1.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Ff",      Height=4, Spacing= 1.0 / 30.0, FadeLabels=false, FadeLines=false  },
                                            }
                    },
                    // 1
                    new ScaleFraction()
                    { 
                        ScaleMin= 1.0 / 20.0,
                        ScaleMax= 1.0 / 18.0,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="M:S:Ff",  Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="S:Ff",    Height=5, Spacing=1.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Ff",      Height=4, Spacing=5.0 / 30.0, FadeLabels=false,  FadeLines=false  },
                                                new LineDefinition() { Label="Ff",        Height=4, Spacing=1.0 / 30.0, FadeLabels=true, FadeLines=false  },
                                            }
                    },
                    new ScaleFraction() {
                        ScaleMin= 1.0 / 18.0,
                        ScaleMax= 1.0 / 8.0,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="M:S:Ff",  Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="S:Ff",    Height=5, Spacing=1.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Ff",      Height=4, Spacing=5.0 / 30.0, FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="",        Height=4, Spacing=1.0 / 30.0, FadeLabels=false, FadeLines=true  },
                                            }
                    },
                    new ScaleFraction() {
                        ScaleMin= 1.0 / 8.0,
                        ScaleMax= 1.0 / 6.0,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="M:S:Ff",  Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="S:Ff",    Height=5, Spacing=1.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Ff",      Height=4, Spacing=5.0 / 30.0, FadeLabels=false,  FadeLines=false  },
                                            }
                    },
                    new ScaleFraction() {
                        ScaleMin= 1.0 / 6.0,
                        ScaleMax= 1.0 / 4.0,
                        LineDefinitions=   new List<LineDefinition>  {
                                                new LineDefinition() { Label="M:S:Ff",  Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="S:Ff",    Height=5, Spacing=1.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Ff",      Height=4, Spacing=5.0 / 30.0, FadeLabels=true,  FadeLines=false  },
                                            }
                    },
                    // 5
                    new ScaleFraction() {
                        ScaleMin= 1.0 / 4.0,
                        ScaleMax= 0.8,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="M:S:Ff",  Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Ss",      Height=5, Spacing=1.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="",        Height=4, Spacing=5.0 / 30.0, FadeLabels=false,  FadeLines=true  },
                                            }
                    },
                    // 6
                    new ScaleFraction() {
                        ScaleMin=  0.8,
                        ScaleMax=  1.8,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="M:Ss",    Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="M:Ss",    Height=10,Spacing=5.0,         FadeLabels=false, FadeLines=true  },
                                                new LineDefinition() { Label="Ss",      Height=4, Spacing=5.0,         FadeLabels=false, FadeLines=true  },
                                                new LineDefinition() { Label="Ss",      Height=4, Spacing=1.0,         FadeLabels=true,  FadeLines=false  },
                                            }
                    },
                    // 7
                    new ScaleFraction() {
                        ScaleMin= 1.8,
                        ScaleMax= 5,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="M:Ss",    Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="M:Ss",    Height=5, Spacing=30.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Ss",      Height=5, Spacing=5.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="",        Height=3, Spacing=1.0,         FadeLabels=false,  FadeLines=true  },
                                            }
                    },

                    // 8            
                    new ScaleFraction() {
                        ScaleMin= 5,
                        ScaleMax= 10,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="M:Ss",    Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Ss",      Height=5, Spacing=30.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Ss",      Height=5, Spacing=5.0,         FadeLabels=true, FadeLines=false  },
                                            }
                    },
            
                    // 9
                    new ScaleFraction() {
                        ScaleMin= 10,
                        ScaleMax= 22,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="M:Ss",    Height=11,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="M:Ss",    Height=7, Spacing=30.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="",        Height=4, Spacing=5.0,         FadeLabels=false, FadeLines=true   },
                                            }
                    },

                    // 10
                    new ScaleFraction() {
                        ScaleMin= 22,
                        ScaleMax= 30,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="M:Ss",    Height=11, Spacing= 60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="M:Ss",    Height=7,  Spacing= 30.0,        FadeLabels=true, FadeLines=false  },
                                            }
                    },

                    // 11
                    new ScaleFraction() {
                        ScaleMin= 30,
                        ScaleMax= 60,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="Mm",      Height=11, Spacing= 60.0* 5.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Mm",      Height=11, Spacing= 60.0,             FadeLabels=true, FadeLines=false  },
                                            }
                    },
            
            
                    // 12
                    new ScaleFraction() {
                        ScaleMin= 60,
                        ScaleMax= 300,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="Mm",      Height=11, Spacing= 60.0* 5.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="",      Height=11, Spacing= 60.0,             FadeLabels=false, FadeLines=false  },
                                            }
                    },

            
                    // 13
                    new ScaleFraction() {
                        ScaleMin= 300,
                        ScaleMax= 400,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="Mm",      Height=11, Spacing= 60.0* 5.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="",      Height=11, Spacing= 60.0,             FadeLabels=false, FadeLines=true  },
                                            }
                    },

                    // 14
                    new ScaleFraction() {
                        ScaleMin= 600,
                        ScaleMax= 1000,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="H:Mm",      Height=11, Spacing= 60.0* 60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Mm",      Height=11, Spacing= 60.0* 5.0,        FadeLabels=true, FadeLines=false  },
                                            }
                    },

                    // 15
                    new ScaleFraction() {
                        ScaleMin= 1000,
                        ScaleMax= 1500,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="H:Mm",      Height=11, Spacing= 60.0* 60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="",            Height=11, Spacing= 60.0* 5.0,        FadeLabels=false, FadeLines=false  },
                                            }
                    },
                    // 16
                    new ScaleFraction() {
                        ScaleMin= 1500,
                        ScaleMax= 3000,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="H:Mm",      Height=11, Spacing= 60.0* 60.0,        FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="",            Height=11, Spacing= 60.0* 5.0,        FadeLabels=false, FadeLines=true  },
                                            }
                    },   
                    // 17
                    new ScaleFraction() {
                        ScaleMin= 3000,
                        ScaleMax= 4000,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="Hh",      Height=11, Spacing= 60.0* 60.0*10,     FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Hh",      Height=11, Spacing= 60.0* 60.0,        FadeLabels=true, FadeLines=false  },
                                            }
                    },            
                    // 18
                    new ScaleFraction() {
                        ScaleMin= 4000,
                        ScaleMax= 7000,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="Hh",      Height=11, Spacing= 60.0* 60.0*10,     FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="",      Height=11, Spacing= 60.0* 60.0,        FadeLabels=false, FadeLines=true  },
                                            }
                    },            

                    };
            
            }
            else {
                m_Fractions = new List<ScaleFraction> {
                    // 0
                    new ScaleFraction()
                    { 
                        ScaleMin= 1.0 / 60.0,
                        ScaleMax= 1.0 / 20.0,
                        LineDefinitions=   new List<LineDefinition> {
                                                //new LineDefinition() { Label="M:S:Ff",  Height=10,Spacing=60.0,         FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="S:Ff",    Height=5, Spacing= 1.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="Tf",      Height=4, Spacing= 1.0 / _FPSView, FadeLabels=true, FadeLines=false  },
                                            }
                    },
                    // 1
                    new ScaleFraction()
                    { 
                        ScaleMin= 1.0 / 20.0,
                        ScaleMax= 1.0 / 18.0,
                        LineDefinitions=   new List<LineDefinition> {
                                                //new LineDefinition() { Label="M:S:Ff",  Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="S:Ff",    Height=5, Spacing=1.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="T",        Height=4, Spacing=5.0 / _FPSView, FadeLabels=false,  FadeLines=false  },
                                                new LineDefinition() { Label="T",        Height=4, Spacing=1.0 / _FPSView, FadeLabels=true, FadeLines=false  },
                                            }
                    },
                    new ScaleFraction() {
                        ScaleMin= 1.0 / 18.0,
                        ScaleMax= 1.0 / 8.0,
                        LineDefinitions=   new List<LineDefinition> {
                                                //new LineDefinition() { Label="M:S:Ff",  Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="S:Ff",    Height=5, Spacing=1.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="T",      Height=4, Spacing=  10 /_FPSView   , FadeLabels=false,  FadeLines=false  },
                                                new LineDefinition() { Label="T",      Height=4, Spacing=5.0 / _FPSView, FadeLabels=true, FadeLines=false  },
                                                //new LineDefinition() { Label="",        Height=4, Spacing=1.0 / _FPSView, FadeLabels=false, FadeLines=true  },
                                            }
                    },
                    new ScaleFraction() {
                        ScaleMin= 1.0 / 8.0,
                        ScaleMax= 1.0 / 6.0,
                        LineDefinitions=   new List<LineDefinition> {
                                                //new LineDefinition() { Label="M:S:Ff",  Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="S:Ff",    Height=5, Spacing=1.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="T",      Height=4, Spacing=  10 /_FPSView   , FadeLabels=false,  FadeLines=false  },
                                                new LineDefinition() { Label="",      Height=4, Spacing=5.0 / _FPSView, FadeLabels=false,  FadeLines=false  },
                                            }
                    },
                    new ScaleFraction() {
                        ScaleMin= 1.0 / 6.0,
                        ScaleMax= 1.0 / 4.0,
                        LineDefinitions=   new List<LineDefinition>  {
                                                //new LineDefinition() { Label="M:S:Ff",  Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="S:Ff",    Height=5, Spacing=1.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="T",      Height=4, Spacing=  10 /_FPSView   , FadeLabels=false,  FadeLines=false  },
                                            }
                    },
                    // 5
                    new ScaleFraction() {
                        ScaleMin= 1.0 / 4.0,
                        ScaleMax= 0.6,
                        LineDefinitions=   new List<LineDefinition> {
                                                //new LineDefinition() { Label="M:S:Ff",  Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="Ss",      Height=5, Spacing=1.0,         FadeLabels=false, FadeLines=false  },
                                                new LineDefinition() { Label="T",      Height=4, Spacing=  10 /_FPSView   , FadeLabels=false,  FadeLines=false  },
                                                //new LineDefinition() { Label="",        Height=4, Spacing=5.0 / _FPSView, FadeLabels=false,  FadeLines=true  },
                                            }
                    },
                    // 6
                    new ScaleFraction() {
                        ScaleMin=  0.6,
                        ScaleMax=  1.0,
                        LineDefinitions=   new List<LineDefinition> {
                                                //new LineDefinition() { Label="Ta",        Height=4, Spacing=5.0 / _FPSView, FadeLabels=true,  FadeLines=true  },
                                                new LineDefinition() { Label="T",      Height=4, Spacing= 100 /_FPSView   , FadeLabels=false,  FadeLines=false  },
                                                new LineDefinition() { Label="T",      Height=4, Spacing=  10 /_FPSView   , FadeLabels=true,  FadeLines=false  },
                                                //new LineDefinition() { Label="M:Ss",    Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="M:Ss",    Height=10,Spacing=5.0,         FadeLabels=false, FadeLines=true  },
                                                //new LineDefinition() { Label="Ss",      Height=4, Spacing=5.0,         FadeLabels=false, FadeLines=true  },
                                                //new LineDefinition() { Label="Ss",      Height=4, Spacing=1.0,         FadeLabels=true,  FadeLines=false  },
                                            }
                    },
                    // 7
                    new ScaleFraction() {
                        ScaleMin= 1.0,
                        ScaleMax= 5,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="T",      Height=4, Spacing= 100 /_FPSView   , FadeLabels=true,  FadeLines=false  },
                                                new LineDefinition() { Label="",        Height=4, Spacing=10.0 / _FPSView, FadeLabels=true,  FadeLines=true  },
                                                //new LineDefinition() { Label="M:Ss",    Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="M:Ss",    Height=5, Spacing=30.0,        FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="Ss",      Height=5, Spacing=5.0,         FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="",        Height=3, Spacing=1.0,         FadeLabels=false,  FadeLines=true  },
                                            }
                    },

                    // 8            
                    new ScaleFraction() {
                        ScaleMin= 5,
                        ScaleMax= 10,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="T",      Height=4, Spacing= 1000.0 /_FPSView   , FadeLabels=false,  FadeLines=false  },
                                                new LineDefinition() { Label="",        Height=4, Spacing=100.0  / _FPSView, FadeLabels=true,  FadeLines=true  },
                                                //new LineDefinition() { Label="M:Ss",    Height=10,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="Ss",      Height=5, Spacing=30.0,        FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="Ss",      Height=5, Spacing=5.0,         FadeLabels=true, FadeLines=false  },
                                            }
                    },
            
                    // 9
                    new ScaleFraction() {
                        ScaleMin= 10,
                        ScaleMax= 22,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="T",      Height=4, Spacing= 1000.0 /_FPSView   , FadeLabels=true,  FadeLines=false  },
                                                //new LineDefinition() { Label="M:Ss",    Height=11,Spacing=60.0,        FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="M:Ss",    Height=7, Spacing=30.0,        FadeLabels=false, FadeLines=false  },
                                                //new LineDefinition() { Label="",        Height=4, Spacing=5.0,         FadeLabels=false, FadeLines=true   },
                                            }
                    },

                    // 10
                    new ScaleFraction() {
                        ScaleMin= 22,
                        ScaleMax= 30,
                        LineDefinitions=   new List<LineDefinition> {
                                                new LineDefinition() { Label="",        Height=4, Spacing= 1000.0 /_FPSView   , FadeLabels=true,  FadeLines=true  },
                                            }
                    },

                    // 11
                    new ScaleFraction() {
                        ScaleMin= 30,
                        ScaleMax= 60,
                        LineDefinitions=   new List<LineDefinition> {
                                            }
                    },
            
            
                    // 12
                    new ScaleFraction() {
                        ScaleMin= 60,
                        ScaleMax= 300,
                        LineDefinitions=   new List<LineDefinition> {
                                            }
                    },

            
                    // 13
                    new ScaleFraction() {
                        ScaleMin= 300,
                        ScaleMax= 400,
                        LineDefinitions=   new List<LineDefinition> {
                                            }
                    },

                    // 14
                    new ScaleFraction() {
                        ScaleMin= 600,
                        ScaleMax= 1000,
                        LineDefinitions=   new List<LineDefinition> {
                                            }
                    },

                    // 15
                    new ScaleFraction() {
                        ScaleMin= 1000,
                        ScaleMax= 1500,
                        LineDefinitions=   new List<LineDefinition> {
                                            }
                    },
                    // 16
                    new ScaleFraction() {
                        ScaleMin= 1500,
                        ScaleMax= 3000,
                        LineDefinitions=   new List<LineDefinition> {
                                            }
                    },   
                    // 17
                    new ScaleFraction() {
                        ScaleMin= 3000,
                        ScaleMax= 4000,
                        LineDefinitions=   new List<LineDefinition> {
                                            }
                    },            
                    // 18
                    new ScaleFraction() {
                        ScaleMin= 4000,
                        ScaleMax= 7000,
                        LineDefinitions=   new List<LineDefinition> {
                                            }
                    },            

                    };            
            }


        }
        #endregion


        #region paint
        private void DrawTimeTicks(DrawingContext dc, double scale, double offset) {
            Pen pen = new Pen(new SolidColorBrush(Color.FromArgb((byte) 255, 255, 255, 255)), 1);
            pen.Freeze();

            if (scale > Constants.Epsilon) {

                double m_Width = ActualWidth;
                double m_Height = ActualHeight;

                const float density = 0.02f;
                Dictionary<int, bool> usedPositions= new Dictionary<int, bool>();

                scale = 1 / scale;

                int f=0;
                while (     f < m_Fractions.Count() - 1
                            && scale < m_Fractions[f+1].ScaleMin * density) {
                    f++;
                }
                for (f = 0; f < m_Fractions.Count() - 1 && scale > m_Fractions[f+1].ScaleMin * density; f++) {
                }
                /* DEBUG OUTPUT
                dc.DrawText(new FormattedText("Scale: " + (valuePerPixel/DENSITY).ToString() + "  f=" +  f.ToString() + " ",
                                                        CultureInfo.GetCultureInfo("en-us"),
                                                        FlowDirection.LeftToRight,
                                                        new Typeface("Verdana"),
                                                        10,
                                                        System.Windows.Media.Brushes.White
                                                        ),   new Point(0, 25));

                */
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

                    SolidColorBrush fadebrush = new SolidColorBrush( Color.FromArgb((byte) (255-fadeFactor*255), 255, 255, 255));
                    fadebrush.Freeze();
                    if (linedef.FadeLines) {
                        pen = new Pen(fadebrush, 1);
                    }
                    else {
                        pen = new Pen(Brushes.White,1);
                    }
                    pen.Freeze();


                    while (t / scale < m_Width) {
                        int x = (int) (t / scale);

                        if (x > 0 && x < m_Width && !usedPositions.ContainsKey(x)) {
                            usedPositions[x]= true;
                            dc.DrawLine(pen, new Point(x, m_Height - linedef.Height), new Point(x, m_Height));

                            if (linedef.Label != "") {
                                String output= "";
                                foreach (char c in linedef.Label) {

                                    if (c == 'T')
                                    {
                                        output += Math.Floor((float)(t + offset) * _FPSView ).ToString("00");
                                    }
                                    else if (c == 'F')
                                    {
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
        private float _FPSView;
    }
}
