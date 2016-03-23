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

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for PresetThumb.xaml
    /// </summary>
    public partial class PresetThumb : UserControl
    {
        static Dictionary<String, BitmapImage> _cache = new Dictionary<string, BitmapImage>();

        public PresetThumb()
        {
            InitializeComponent();
            Loaded += PresetThumb_Loaded;
        }

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

            if (App.Current.OperatorPresetManager.LivePreviewEnabled)
            {
                App.Current.OperatorPresetManager.PreviewPreset(preset);
                App.Current.OperatorPresetManager.RenderAndSaveThumbnail(preset);
                App.Current.OperatorPresetManager.RestorePreviewPreset();
            }
            LoadThumbnail(preset);
        }

        private void LoadThumbnail(OperatorPreset preset, bool useCache = true)
        {
            var imagePath = preset.BuildImagePath();
            if (useCache && _cache.ContainsKey(imagePath))
            {
                XImage.Source = _cache[imagePath];
                return;
            }                

            if (File.Exists(imagePath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                //bitmap.CacheOption = BitmapCacheOption.OnLoad;
                //bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();
                bitmap.Freeze();
                XImage.Source = bitmap;
                _cache[imagePath] = bitmap;
            }
            else
            {
                XImage.Source = null;
                Logger.Info("Failed to load thumbnail {0}", imagePath);
                _cache[imagePath] = null;
            }
        }

        private bool _previewed = false;
        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            var o = sender as FrameworkElement;
            var preset = o.DataContext as OperatorPreset;
            if (preset != null)
            {
                _previewed = App.Current.OperatorPresetManager.PreviewPreset(preset);
            }
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            var o = sender as FrameworkElement;
            var preset = o.DataContext as OperatorPreset;
            if (preset != null && _previewed)
            {                
                App.Current.OperatorPresetManager.RestorePreviewPreset();
                _previewed = false;
            }
        }

        private Point _startDragPos;
        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _startDragPos = new Point(e.HorizontalOffset, e.VerticalOffset);
            XBlendInfoText.Visibility= Visibility.Visible;
            XBlendInfoText.Text = "100%";
            
            App.Current.OperatorPresetManager.StartBlending();

        }

        private const double VIRTUAL_SLIDER_WIDTH = 200;

        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var o = sender as FrameworkElement;
            var preset = o.DataContext as OperatorPreset;
            if (preset == null) 
                return;

            var factor = (float) ((VIRTUAL_SLIDER_WIDTH + e.HorizontalChange)/VIRTUAL_SLIDER_WIDTH);
            App.Current.OperatorPresetManager.BlendPreset(preset, factor);
            XBlendInfoText.Text = String.Format("{0}%", Math.Floor(factor*100));
        }

        private void Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            var o = sender as FrameworkElement;
            var preset = o.DataContext as OperatorPreset;
            if (preset != null)
            {
                if (Keyboard.Modifiers ==ModifierKeys.Control)
                {
                    App.Current.OperatorPresetManager.DeletePreset(preset);
                }
                else
                {
                    if (Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        App.Current.OperatorPresetManager.RenderAndSaveThumbnail(preset);
                        LoadThumbnail(preset, false);
                    }
                    else
                    {
                        if (Math.Abs(e.HorizontalChange) < 3)
                        {
                            App.Current.OperatorPresetManager.ApplyPreset(preset);
                        }
                        else
                        {
                            var factor = (float) ((VIRTUAL_SLIDER_WIDTH + e.HorizontalChange)/VIRTUAL_SLIDER_WIDTH);
                            App.Current.OperatorPresetManager.BlendPreset(preset,factor);                                
                            App.Current.OperatorPresetManager.CompleteBlendPreset(preset);
                        }
                    }
                }
            }

            XBlendInfoText.Visibility= Visibility.Collapsed;            
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
    }
}
