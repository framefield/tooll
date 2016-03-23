// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Windows.Input;
using Framefield.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Framefield.Tooll
{
    public class ResizeThumb : Thumb
    {
        public ResizeThumb()
        {
            DragStarted += ResizeThumb_DragStarted;
            DragDelta += ResizeThumb_DragDelta;
            DragCompleted += ResizeThumb_DragCompleted;
            this.Opacity = 0.0;
        }


        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {            
            if (VerticalAlignment == VerticalAlignment.Bottom || VerticalAlignment == VerticalAlignment.Top)
            {
                EditBox.StartMoveKeyframeCommand();
            }
            else if (HorizontalAlignment == HorizontalAlignment.Left || HorizontalAlignment == HorizontalAlignment.Right)
            {
                EditBox.StartMoveKeyframeCommand();
            }
        }


        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (EditBox != null)
            {
                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                        EditBox.ScaleAtBottom(e.VerticalChange);
                        break;
                    case VerticalAlignment.Top:
                        EditBox.ScaleAtTop(e.VerticalChange);
                        break;
                    default:
                        break;
                }

                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        EditBox.ScaleAtLeftPosition();
                        break;
                    case HorizontalAlignment.Right:
                        EditBox.ScaleAtRightPosition();
                        break;
                    default:
                        break;
                }
            }
            e.Handled = true;
        }


        private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            EditBox.CompleteMoveKeyframeCommand();
        }


        #region dirty stuff
        private CurveEditBox _EditBox;
        public CurveEditBox EditBox
        {
            get
            {
                if (_EditBox == null)
                    _EditBox = UIHelper.FindParent<CurveEditBox>(this);
                return _EditBox;
            }
        }

        private CurveEditor _curveEditor;
        public CurveEditor CurveEditor
        {
            get
            {
                if (_curveEditor == null)
                    _curveEditor = UIHelper.FindParent<CurveEditor>(this);
                return _curveEditor;
            }
        }
        #endregion

    }
}
