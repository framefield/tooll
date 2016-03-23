// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using FlowDirection = System.Windows.FlowDirection;
using Point = System.Windows.Point;

namespace Framefield.Tooll.Components.CompositionView
{
    public class ConnectionDragAdorner : Adorner
    {
        private const double EMSize = 13;
        private const double RectEdgeRadius = 5;

        private static readonly Vector MarginVector = new Vector(12, 12);
        private static readonly Point Position = new Point(45, -45);
        private static readonly SolidColorBrush RectColor = new SolidColorBrush(Color.FromArgb(150, 80, 80, 80));
        private static readonly Pen RectBorder = new Pen(Brushes.Gray, 1);

        private readonly Rect _rect;
        private readonly FormattedText _text;

        public ConnectionDragAdorner(UIElement adornedElement, string infoText)
            : base(adornedElement)
        {
            _text = new FormattedText(infoText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Verdanda"), EMSize, Brushes.Gray);
            _rect = new Rect(Position - MarginVector, Position + MarginVector + new Vector(_text.Width, _text.Height));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRoundedRectangle(RectColor, RectBorder, _rect, RectEdgeRadius, RectEdgeRadius);
            drawingContext.DrawText(_text, Position);
            base.OnRender(drawingContext);
        }
    }
}