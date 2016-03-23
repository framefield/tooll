// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Framefield.Core;
using Microsoft.Win32;

namespace Framefield.Tooll
{
    internal static class UIHelper
    {
        public static Vector ToVector(this Point p) { return new Vector(p.X, p.Y); }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = LogicalTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                return FindParent<T>(parentObject);
            }
        }

        public static T FindVisualParent<T>(DependencyObject obj) where T : DependencyObject
        {
            DependencyObject tmp = VisualTreeHelper.GetParent(obj);
            while (tmp != null && !(tmp is T))
            {
                tmp = VisualTreeHelper.GetParent(tmp);
            }
            return tmp as T;
        }


        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }


        public static List<HitTestResult> HitTestFor<T>(Visual visual, Point pt, double radius) { return HitTestFor<T>(visual, new EllipseGeometry(pt, radius, radius)); }

        public static List<HitTestResult> HitTestFor<T>(Visual visual, Geometry hitTestArea)
        {
            m_HitResultsList.Clear();
            VisualTreeHelper.HitTest(visual, HitTestFilterInvisible, new HitTestResultCallback(MyHitTestResultCallback<T>),
                                     new GeometryHitTestParameters(hitTestArea));
            return m_HitResultsList;
        }

        public static HitTestFilterBehavior HitTestFilterInvisible(DependencyObject potentialHitTestTarget)
        {
            bool isVisible = false;
            bool isHitTestVisible = false;

            var uiElement = potentialHitTestTarget as UIElement;
            if (uiElement != null)
            {
                isVisible = uiElement.IsVisible;
                if (isVisible)
                {
                    isHitTestVisible = uiElement.IsHitTestVisible;
                }
            }
            else
            {
                UIElement3D uiElement3D = potentialHitTestTarget as UIElement3D;
                if (uiElement3D != null)
                {
                    isVisible = uiElement3D.IsVisible;
                    if (isVisible)
                    {
                        isHitTestVisible = uiElement3D.IsHitTestVisible;
                    }
                }
            }

            if (isVisible)
            {
                return isHitTestVisible ? HitTestFilterBehavior.Continue : HitTestFilterBehavior.ContinueSkipSelf;
            }

            return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
        }

        private static HitTestResultBehavior MyHitTestResultCallback<T>(HitTestResult result)
        {
            if (result.VisualHit is T)
                m_HitResultsList.Add(result);
            return HitTestResultBehavior.Continue;
        }

        private static List<HitTestResult> m_HitResultsList = new List<HitTestResult>();


        public static string PickFileWithDialog(string defaultPath, string startPath, string dialogTitle, string filter = "")
        {
            var path = defaultPath;
            if (startPath != string.Empty)
            {
                var parts = startPath.Split('/').ToList();
                parts.RemoveAt(parts.Count - 1);
                path = string.Join("/", parts);
            }

            var dlg = new OpenFileDialog
                          {
                              FileName = "Document",
                              Title = dialogTitle,
                              DereferenceLinks = false
                          };

            if (filter != string.Empty)
            {
                dlg.Filter = filter;
            }

            try
            {
                dlg.InitialDirectory = System.IO.Path.GetFullPath(path);
            }
            catch (ArgumentException)
            {
                dlg.InitialDirectory = System.IO.Path.GetFullPath(@".");
            }

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                var pickedFilePath = dlg.FileName;
                return ConvertToRelativeFilepath(pickedFilePath);
            }

            return string.Empty;
        }

        public static string ConvertToRelativeFilepath(string absoluteFilePath)
        {
            var currentApplicationPath = System.IO.Path.GetFullPath(".");            
            var firstCharUppercase = currentApplicationPath.Substring(0,1).ToUpper();
            currentApplicationPath = firstCharUppercase + currentApplicationPath.Substring(1, currentApplicationPath.Length-1) + "\\";           
            var relativeFilePath = absoluteFilePath.Replace(currentApplicationPath, "").Replace("\\", "/");
            return relativeFilePath;
        }

        public static MetaOperator DuplicateOperatorTypeWithDialog(MetaOperator orgMetaOp)
        {
            var dialog = new Components.Dialogs.NewOperatorDialog();
            dialog.Title = "Duplicate as new type";
            dialog.XText.Text = String.Format("Do you really want to create a new Operator type derived from {0}?", orgMetaOp.Name);
            dialog.XNamespace.Text = orgMetaOp.Namespace;
            dialog.XName.Text = Utilities.GetDuplicatedTitle(orgMetaOp.Name);
            dialog.XName.SelectAll();
            dialog.XDescription.Text = orgMetaOp.Description;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
                return null;

            var copiedMetaOp = orgMetaOp.Clone(dialog.XName.Text);
            copiedMetaOp.Description = dialog.XDescription.Text;
            copiedMetaOp.Namespace = dialog.XNamespace.Text;

            App.Current.Model.MetaOpManager.AddMetaOperator(copiedMetaOp.ID, copiedMetaOp);

            // copy presets to new type
            App.Current.OperatorPresetManager.CopyPresetsOfOpToAnother(orgMetaOp, copiedMetaOp);
            return copiedMetaOp;
        }

        public static void ShowErrorMessageBox(string errorText, string caption = "Something terrible happened")
        {
            var messageBoxText = errorText;
            var button = MessageBoxButton.OK;
            var icon = MessageBoxImage.Error;
            MessageBox.Show(messageBoxText, caption, button, icon);
        }


        public static double SubScaleFromKeyboardModifiers()
        {
            double subScale = 1.0;
            if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                subScale = 0.1;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                subScale = 10.0;
            }
            return subScale;
        }


        /**
        * Predefined type colors 
        * Also see http://streber.pixtur.de/4982
        **/
        public static Color ColorFromType(FunctionType type)
        {
            switch (type)
            {
                case FunctionType.Scene: return Color.FromRgb(0x34, 0x4E, 0x51);
                case FunctionType.Text: return Color.FromRgb(0x3c, 0x4a, 0x30);
                case FunctionType.Image: return Color.FromRgb(0x4C, 0x34, 0x51);
                case FunctionType.Generic: return Color.FromRgb(0x4a, 0x47, 0x30);
                default: return Color.FromRgb(0x4f, 0x4b, 0x4b);
            }
        }

        public static Color BrightColorFromType(FunctionType type)
        {
            switch (type)
            {
                case FunctionType.Scene: return Color.FromRgb(0xA8, 0xE0, 0xe2);
                case FunctionType.Text: return Color.FromRgb(0xc4, 0xdd, 0x96);
                case FunctionType.Image: return Color.FromRgb(0xDF, 0xA8, 0xE2);
                case FunctionType.Generic: return Color.FromRgb(0xdd, 0xd9, 0x96);
                default: return Color.FromRgb(0xc6, 0xbb, 0xbb);
            }
        }

        public static Color DarkColorFromType(FunctionType type)
        {
            switch (type)
            {
                case FunctionType.Scene: return Color.FromRgb(0x0e, 0x0b, 0x0b);
                case FunctionType.Text: return Color.FromRgb(0x17, 0x1f, 0x11);
                case FunctionType.Image: return Color.FromRgb(0x21, 0x13, 0x24);
                case FunctionType.Generic: return Color.FromRgb(0x1f, 0x1e, 0x11);
                default: return Color.FromRgb(0x0e, 0x0b, 0x0b);
            }
        }

        public static Color ColorFromFloatRGBA(float r, float g, float b, float a = 1.0f)
        {
            return Color.FromArgb(
                (byte) (Utilities.Clamp(a, 0, 1)*255),
                (byte) (Utilities.Clamp(r, 0, 1)*255),
                (byte) (Utilities.Clamp(g, 0, 1)*255),
                (byte) (Utilities.Clamp(b, 0, 1)*255));
        }
    }

    /**
    * Due to the fucked up mouse event handling in WPF
    * we have to directly access the raw input to provide
    * interactive mouse interactions of complex 3d-views.
    * 
    * The follow marsheling provides access for the ShowSceneControl
    * interaction update.
    * 
    * also refer to
    * https://streber.framefield.com/5381
    */
    internal static class Win32RawInput
    {
        public static Point MousePointInUiElement(UIElement uiElement)
        {
            POINT screenSpacePoint;
            GetCursorPos(out screenSpacePoint);
            var currentMousePosition = new Point(screenSpacePoint.X,
                                                                screenSpacePoint.Y);
            return uiElement.PointFromScreen(currentMousePosition);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern short GetAsyncKeyState(int vkey);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }        
    }



    /**
     * Throw this exception to shut down tooll, e.g. after showing a fatal-failure message box.
     */
    public class ShutDownException : SystemException
    {
        public string Title { get; set; }

        public ShutDownException(string messsage, string title = "Something terrible happened...")
        : base(messsage)
        {
            Title = title;
        }
    }

    public class ShutDownSilentException : SystemException
    {
        public ShutDownSilentException()
        : base("Shutdown silent")
        {
        }
    }

    public static class CustomCursorProvider
    {
        public enum Cursors {
            SlideNormal = 1,
            StartConnection,
            Remove,
            Save,
        }

        private static System.Windows.Input.Cursor LoadCursor(String filename)
        {
            var stream = Application.GetResourceStream(new Uri("Images/cursors/" + filename, UriKind.Relative)).Stream;
            var cursor = new System.Windows.Input.Cursor( stream );
            return cursor;
        }

        private static void Initialize()
        {
            _stremsById = new Dictionary<CustomCursorProvider.Cursors, System.Windows.Input.Cursor>();

            // Iterate over cursor-enumeration and load its image files
            for (var i = 1;; i++)
            {
                var cursorEnum = (Cursors) i;
                var enumAsString = cursorEnum.ToString();
                if (enumAsString == i.ToString())   // Completed
                    break;

                _stremsById[cursorEnum] = LoadCursor("Cursor"+enumAsString+".cur");    
            }
        }

        public static System.Windows.Input.Cursor GetCursorStream( CustomCursorProvider.Cursors cursor) 
        {
            if( _stremsById == null) 
                Initialize();

            return _stremsById[cursor];
        }
        static Dictionary<CustomCursorProvider.Cursors, System.Windows.Input.Cursor> _stremsById;
    }

}
