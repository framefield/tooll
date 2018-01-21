// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.IO;
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
using Framefield.Tooll;
using Framefield.Core;
using Framefield.Tooll.Components.CompositionView;
using System.Windows.Controls.Primitives;


namespace Framefield.Tooll.Components.ParameterView.OperatorPresets
{

    public partial class PresetThumb : UserControl
    {
        public PresetThumb()
        {
            InitializeComponent();
            Loaded += PresetThumb_Loaded;

            PresetManager = App.Current.OperatorPresetManager;
        }

        private OperatorPresetManager PresetManager;

        void PresetThumb_Loaded(object sender, RoutedEventArgs e)
        {
            var applicationWindow = Window.GetWindow(this);
            if (applicationWindow != null)
            {
                applicationWindow.PreviewKeyDown += XControl_KeyDown;
                applicationWindow.PreviewKeyUp += XControl_KeyUp;
            }

            var preset = DataContext as OperatorPreset;
            if (preset == null)
                return;

            if (PresetManager.LivePreviewEnabled)
            {
                PresetManager.PreviewPreset(preset);
                PresetManager.PresetImageManager.RenderAndSaveThumbnail(preset);
                PresetManager.RestorePreviewPreset();
            }
            else
            {
                SetImage(preset);
            }
        }


        private void SetImage(OperatorPreset preset, bool useCache = true)
        {
            XImage.Source = PresetManager.PresetImageManager.GetImageForPreset(preset);
        }


        #region internaction
        private bool _previewed = false;
        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            var o = sender as FrameworkElement;
            var preset = o.DataContext as OperatorPreset;
            if (preset != null)
            {
                _previewed = PresetManager.PreviewPreset(preset);
            }
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            var o = sender as FrameworkElement;
            var preset = o.DataContext as OperatorPreset;
            if (preset != null && _previewed)
            {
                PresetManager.RestorePreviewPreset();
                _previewed = false;
            }
        }

        private Point _startDragPos;
        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _startDragPos = new Point(e.HorizontalOffset, e.VerticalOffset);
            XBlendInfoText.Visibility = Visibility.Visible;
            XBlendInfoText.Text = "100%";

            PresetManager.StartBlending();

        }

        private const double VIRTUAL_SLIDER_WIDTH = 200;

        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var o = sender as FrameworkElement;
            var preset = o.DataContext as OperatorPreset;
            if (preset == null)
                return;

            var factor = (float)((VIRTUAL_SLIDER_WIDTH + e.HorizontalChange) / VIRTUAL_SLIDER_WIDTH);
            PresetManager.BlendPreset(preset, factor);
            XBlendInfoText.Text = String.Format("{0}%", Math.Floor(factor * 100));
        }

        private void Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            var o = sender as FrameworkElement;
            var preset = o.DataContext as OperatorPreset;
            if (preset != null)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    PresetManager.DeletePreset(preset);
                }
                else
                {
                    var thumbnailUpdateRequested = Keyboard.Modifiers == ModifierKeys.Shift;
                    if (thumbnailUpdateRequested)
                    {
                        PresetManager.PresetImageManager.RenderAndSaveThumbnail(preset);
                        XImage.Source = PresetManager.PresetImageManager.GetImageForPreset(preset);
                    }
                    else
                    {
                        if (Math.Abs(e.HorizontalChange) < 3)
                        {
                            PresetManager.ApplyPreset(preset);
                        }
                        else
                        {
                            var factor = (float)((VIRTUAL_SLIDER_WIDTH + e.HorizontalChange) / VIRTUAL_SLIDER_WIDTH);
                            PresetManager.BlendPreset(preset, factor);
                            PresetManager.CompleteBlendPreset(preset);
                        }
                    }
                }
            }

            XBlendInfoText.Visibility = Visibility.Collapsed;
            e.Handled = true;
        }



        private void XControl_KeyDown(object sender, KeyEventArgs e)
        {
            UpdateCursorShape();
        }


        private void XControl_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateCursorShape();
        }


        private void UpdateCursorShape()
        {
            if (!IsMouseOver)
                return;

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                Cursor = CustomCursorProvider.GetCursorStream(CustomCursorProvider.Cursors.Remove);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                Cursor = CustomCursorProvider.GetCursorStream(CustomCursorProvider.Cursors.Save);
            }
            else
            {
                Cursor = Cursors.Arrow;
            }
        }
        #endregion
    }
}
