// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

// GradientPath.cs (c) Charles Petzold, 2009
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GradientPathLib
{
    public class GradientPath : FrameworkElement
    {
        const double outlinePenWidth = 1;

        public static readonly DependencyProperty DataProperty =
            Path.DataProperty.AddOwner(typeof(GradientPath),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure |
                                                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GradientStopsProperty =
            GradientBrush.GradientStopsProperty.AddOwner(typeof(GradientPath),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GradientModeProperty =
            DependencyProperty.Register("GradientMode",
                typeof(GradientMode),
                typeof(GradientPath),
                new FrameworkPropertyMetadata(GradientMode.Perpendicular,
                                              FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ColorInterpolationModeProperty =
            GradientBrush.ColorInterpolationModeProperty.AddOwner(typeof(GradientPath),
                new FrameworkPropertyMetadata(ColorInterpolationMode.SRgbLinearInterpolation,
                                              FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty =
            Shape.StrokeThicknessProperty.AddOwner(typeof(GradientPath),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsMeasure |
                                                   FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeStartLineCapProperty =
            Shape.StrokeStartLineCapProperty.AddOwner(typeof(GradientPath),
                new FrameworkPropertyMetadata(PenLineCap.Flat, FrameworkPropertyMetadataOptions.AffectsMeasure |
                                                               FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeEndLineCapProperty =
            Shape.StrokeEndLineCapProperty.AddOwner(typeof(GradientPath),
                new FrameworkPropertyMetadata(PenLineCap.Flat, FrameworkPropertyMetadataOptions.AffectsMeasure |
                                                               FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ToleranceProperty =
            DependencyProperty.Register("Tolerance",
                typeof(double),
                typeof(GradientPath),
                new FrameworkPropertyMetadata(Geometry.StandardFlatteningTolerance, 
                                              FrameworkPropertyMetadataOptions.AffectsRender));

        public GradientPath()
        {
            GradientStops = new GradientStopCollection();
        }

        public Geometry Data
        {
            set { SetValue(DataProperty, value); }
            get { return (Geometry)GetValue(DataProperty); }
        }

        public GradientStopCollection GradientStops
        {
            set { SetValue(GradientStopsProperty, value); }
            get { return (GradientStopCollection)GetValue(GradientStopsProperty); }
        }

        public GradientMode GradientMode
        {
            set { SetValue(GradientModeProperty, value); }
            get { return (GradientMode)GetValue(GradientModeProperty); }
        }

        public ColorInterpolationMode ColorInterpolationMode
        {
            set { SetValue(ColorInterpolationModeProperty, value); }
            get { return (ColorInterpolationMode)GetValue(ColorInterpolationModeProperty); }
        }

        public double StrokeThickness
        {
            set { SetValue(StrokeThicknessProperty, value); }
            get { return (double)GetValue(StrokeThicknessProperty); }
        }

        public PenLineCap StrokeStartLineCap
        {
            set { SetValue(StrokeStartLineCapProperty, value); }
            get { return (PenLineCap)GetValue(StrokeStartLineCapProperty); }
        }

        public PenLineCap StrokeEndLineCap
        {
            set { SetValue(StrokeEndLineCapProperty, value); }
            get { return (PenLineCap)GetValue(StrokeEndLineCapProperty); }
        }

        public double Tolerance
        {
            set { SetValue(ToleranceProperty, value); }
            get { return (double)GetValue(ToleranceProperty); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Data == null)
                return base.MeasureOverride(availableSize);

            Pen pen = new Pen()
            {
                Brush = Brushes.Black,
                Thickness = StrokeThickness,
                StartLineCap = StrokeStartLineCap,
                EndLineCap = StrokeEndLineCap
            };
            pen.Freeze();

            Rect rect = Data.GetRenderBounds(pen);
            return new Size(rect.Width, rect.Height);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (Data == null)
                return;

            // Flatten the PathGeometry
            PathGeometry pathGeometry = Data.GetFlattenedPathGeometry(Tolerance, ToleranceType.Absolute);

            foreach (PathFigure pathFigure in pathGeometry.Figures)
            {
                if (pathFigure.Segments.Count != 1)
                    throw new NotSupportedException("More than one PathSegment in a flattened PathFigure");

                if (!(pathFigure.Segments[0] is PolyLineSegment))
                    return;
                    //throw new NotSupportedException("Segment is not PolylineSegment in flattened PathFigure");

                PolyLineSegment polylineSegment = pathFigure.Segments[0] as PolyLineSegment;
                PointCollection points = polylineSegment.Points;

                if (points.Count < 1)
                    throw new NotSupportedException("Empty PointCollection in PolylineSegment in flattened PathFigure");

                // Calculate total length for ParallelMode
                double totalLength = 0;
                double accumLength = 0;
                Point ptPrev = pathFigure.StartPoint;
                foreach (Point pt in points)
                {
                    totalLength += (pt - ptPrev).Length;
                    ptPrev = pt;
                }

                // Get the first vector
                Vector vector = points[0] - pathFigure.StartPoint;

                // Use that to draw the start line cap
                DrawLineCap(dc, pathFigure.StartPoint, vector, StrokeStartLineCap, PenLineCap.Flat);

                // Rotate the vector counter-clockwise 90 degrees
                Vector vector90 = new Vector(vector.Y, -vector.X);
                vector90.Normalize();

                // Calculate perpendiculars
                Point ptUpPrev = pathFigure.StartPoint + StrokeThickness / 2 * vector90;
                Point ptDnPrev = pathFigure.StartPoint - StrokeThickness / 2 * vector90;

                // Begin with the StartPoint
                ptPrev = pathFigure.StartPoint;

                // Loop through the PointCollection
                for (int index = 0; index < points.Count; index++)
                {
                    Point ptUp, ptDn;
                    Point point = points[index];
                    Vector vect1 = ptPrev - point;
                    double angle1 = Math.Atan2(vect1.Y, vect1.X);

                    // Treat the last point much like the first
                    if (index == points.Count - 1)
                    {
                        // Rotate clockwise 90 degrees
                        vector90 = new Vector(-vect1.Y, vect1.X);
                        vector90.Normalize();
                        ptUp = point + (StrokeThickness / 2) * vector90;
                        ptDn = point - (StrokeThickness / 2) * vector90;
                    }

                    else
                    {
                        // Get the next vector and average the two
                        Vector vect2 = points[index + 1] - point;
                        double angle2 = Math.Atan2(vect2.Y, vect2.X);
                        double diff = angle2 - angle1;

                        if (diff < 0)
                            diff += 2 * Math.PI;

                        double angle = angle1 + diff / 2;
                        Vector vect = new Vector(Math.Cos(angle), Math.Sin(angle));
                        vect.Normalize();
                        ptUp = point + StrokeThickness / 2 * vect;
                        ptDn = point - StrokeThickness / 2 * vect;
                    }
                    // Transform to horizontalize tetragon constructed of perpendiculars
                    RotateTransform rotate = new RotateTransform(-180 * angle1 / Math.PI, ptPrev.X, ptPrev.Y);

                    // Construct the tetragon
                    PathGeometry tetragonGeo = new PathGeometry();
                    PathFigure tetragonFig = new PathFigure();
                    tetragonFig.StartPoint = rotate.Transform(ptUpPrev);
                    tetragonFig.IsClosed = true;
                    tetragonFig.IsFilled = true;
                    PolyLineSegment tetragonSeg = new PolyLineSegment();
                    tetragonSeg.Points.Add(rotate.Transform(ptUp));
                    tetragonSeg.Points.Add(rotate.Transform(ptDn));
                    tetragonSeg.Points.Add(rotate.Transform(ptDnPrev));
                    tetragonFig.Segments.Add(tetragonSeg);
                    tetragonGeo.Figures.Add(tetragonFig);

                    LinearGradientBrush brush;

                    if (GradientMode == GradientMode.Perpendicular)
                    {
                        brush = new LinearGradientBrush(GradientStops, new Point(0, 1), new Point(0, 0));
                    }
                    else
                    {
                        // Find where we are in the total path
                        double offset1 = accumLength / totalLength;
                        accumLength += vect1.Length;
                        double offset2 = accumLength / totalLength;

                        // Calculate ax + b factors for gradientStop.Offset conversion
                        double a = 1 / (offset2 - offset1);
                        double b = -offset1 * a;

                        // Calculate a new GradientStopCollection based on restricted lenth
                        GradientStopCollection gradientStops = new GradientStopCollection();

                        if (GradientStops != null)
                        {
                            foreach (GradientStop gradientStop in GradientStops)
                                gradientStops.Add(new GradientStop(gradientStop.Color, a * gradientStop.Offset + b));
                        }

                        brush = new LinearGradientBrush(gradientStops, new Point(1, 0), new Point(0, 0));
                    }

                    // Draw the tetragon rotated back into place
                    brush.ColorInterpolationMode = ColorInterpolationMode;
                    Pen pen = new Pen(brush, outlinePenWidth);
                    pen.Freeze();
                    rotate.Angle = 180 * angle1 / Math.PI;
                    dc.PushTransform(rotate);
                    dc.DrawGeometry(brush, pen, tetragonGeo);
                    dc.Pop();

                    // Something special for the last point
                    if (index == points.Count - 1)
                    {
                        DrawLineCap(dc, point, -vect1, PenLineCap.Flat, StrokeEndLineCap);
                    }

                    // Prepare for next iteration
                    ptPrev = point;
                    ptUpPrev = ptUp;
                    ptDnPrev = ptDn;
                }
            }
        }

        void DrawLineCap(DrawingContext dc, Point point, Vector vector,
                         PenLineCap startLineCap, PenLineCap endLineCap)
        {
            if (startLineCap == PenLineCap.Flat && endLineCap == PenLineCap.Flat)
                return;

            // Construct really tiny horizontal line
            vector.Normalize();
            double angle = Math.Atan2(vector.Y, vector.X);
            RotateTransform rotate = new RotateTransform(-180 * angle / Math.PI, point.X, point.Y);
            Point point1 = rotate.Transform(point);
            Point point2 = rotate.Transform(point + 0.25 * vector);

            // Construct pen for that line
            Pen pen = new Pen()
            {
                Thickness = StrokeThickness,
                StartLineCap = startLineCap,
                EndLineCap = endLineCap
            };
            pen.Freeze();

            // Why don't I just call dc.DrawLine at this point? Well, to avoid gaps between 
            //  the tetragons, I had to draw them with an 'outlinePenWidth' pen based on the 
            //  same brush as the fill. If I just called dc.DrawLine here, the caps would 
            //  look a little smaller than the line, so....

            LineGeometry lineGeo = new LineGeometry(point1, point2);
            PathGeometry pathGeo = lineGeo.GetWidenedPathGeometry(pen);
            Brush brush = null;

            if (GradientMode == GradientMode.Perpendicular)
            {
                brush = new LinearGradientBrush(GradientStops, new Point(0, 0), new Point(0, 1));
                (brush as LinearGradientBrush).ColorInterpolationMode = ColorInterpolationMode;
            }
            else
            {
                double offset = endLineCap == PenLineCap.Flat ? 0 : 1;
                brush = new SolidColorBrush(GetColorFromGradientStops(offset));
                brush.Freeze();
            }

            pen = new Pen(brush, outlinePenWidth);
            pen.Freeze();
            rotate.Angle = 180 * angle / Math.PI;
            dc.PushTransform(rotate);
            dc.DrawGeometry(brush, pen, pathGeo);
            dc.Pop();
        }

        Color GetColorFromGradientStops(double offset)
        {
            if (GradientStops == null || GradientStops.Count == 0)
                return Color.FromArgb(0, 0, 0, 0);

            if (GradientStops.Count == 1)
                return GradientStops[0].Color;

            double lowerOffset = Double.MinValue;
            double upperOffset = Double.MaxValue;
            int lowerIndex = -1;
            int upperIndex = -1;

            for (int i = 0; i < GradientStops.Count; i++)
            {
                GradientStop gradientStop = GradientStops[i];

                if (lowerOffset < gradientStop.Offset && gradientStop.Offset <= offset)
                {
                    lowerOffset = gradientStop.Offset;
                    lowerIndex = i;
                }

                if (upperOffset > gradientStop.Offset && gradientStop.Offset >= offset)
                {
                    upperOffset = gradientStop.Offset;
                    upperIndex = i;
                }
            }

            if (lowerIndex == -1)
                return GradientStops[upperIndex].Color;

            else if (upperIndex == -1)
                return GradientStops[lowerIndex].Color;

            if (lowerIndex == upperIndex)
                return GradientStops[lowerIndex].Color;

            Color clr1 = GradientStops[lowerIndex].Color;
            Color clr2 = GradientStops[upperIndex].Color;
            double den = upperOffset - lowerOffset;
            float wt1 = (float)((upperOffset - offset) / den);
            float wt2 = (float)((offset - lowerOffset) / den);
            Color clr = new Color();

            switch (ColorInterpolationMode)
            {
                case ColorInterpolationMode.SRgbLinearInterpolation:
                    clr = Color.FromArgb((byte)(wt1 * clr1.A + wt2 * clr2.A),
                                         (byte)(wt1 * clr1.R + wt2 * clr2.R),
                                         (byte)(wt1 * clr1.G + wt2 * clr2.G),
                                         (byte)(wt1 * clr1.B + wt2 * clr2.B));
                    break;

                case ColorInterpolationMode.ScRgbLinearInterpolation:
                    clr = clr1 * wt1 + clr2 * wt2;
                    break;
            }
            return clr;
        }
    }
}
