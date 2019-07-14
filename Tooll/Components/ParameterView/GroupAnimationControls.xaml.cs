// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using Framefield.Core.Commands;
using Framefield.Core.Curve;
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
using ICommand = Framefield.Core.ICommand;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for OperatorParameterViewRow.xaml
    /// </summary>
    public partial class GroupAnimationControls : UserControl
    {
        public GroupAnimationControls(List<OperatorPart> opParts)
        {
            m_OperatorParts = opParts;
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            /** NOTE: it not really clear, how this case can happen, because the
             * constructor is always aclled with a valid list. However, when
             * changing the layout a shadow copy without proper construction
             * might receive an OnLoaded-event
            */
            if (m_OperatorParts == null)
                return;

            ConnectEventHandler();
            RebuiltAnimationContainer();
            UpdateControls();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // See note above. WPF sucks.
            if (m_OperatorParts == null)
                return;

            if (App.Current == null || App.Current.Model == null)
                return;

            App.Current.Model.GlobalTimeChangedEvent -= GlobalTimeChangedHandler;

            foreach (var el in m_Animations)
            {
                el.Value.ChangedEvent -= CurveChangedHandler;
            }

            foreach (var opPart in m_OperatorParts)
            {
                opPart.ManipulatedEvent -= OperatorPartModifiedHandler;
            }
            m_OperatorParts = null;
        }

        private void ConnectEventHandler()
        {
            App.Current.Model.GlobalTimeChangedEvent += GlobalTimeChangedHandler;
            foreach (var opPart in m_OperatorParts)
            {
                opPart.ManipulatedEvent += OperatorPartModifiedHandler;
            }
        }

        private void ClickedPreviousKey(object sender, RoutedEventArgs e)
        {
            double? largestPreviousKey = null;
            foreach (var el in m_Animations)
            {
                double? previousKey = el.Value.GetPreviousU(App.Current.Model.GlobalTime);
                if (previousKey.HasValue && !largestPreviousKey.HasValue)
                    largestPreviousKey = previousKey;
                else if (previousKey.HasValue && largestPreviousKey.HasValue)
                    largestPreviousKey = Math.Max(largestPreviousKey.Value, previousKey.Value);
            }

            if (largestPreviousKey.HasValue)
                App.Current.Model.GlobalTime = largestPreviousKey.Value;
        }

        private void ClickedCurrentKey(object sender, RoutedEventArgs e)
        {
            var animations = new Dictionary<OperatorPart, ICurve>(m_Animations);

            bool hasVAtCurrentTime = false;
            foreach (var el in animations)
                hasVAtCurrentTime |= el.Value.HasVAt(App.Current.Model.GlobalTime);

            MakeCommandsForClickedCurrentKey(animations, hasVAtCurrentTime);
        }

        private static void MakeCommandsForClickedCurrentKey(Dictionary<OperatorPart, ICurve> animations, bool hasVAtCurrentTime)
        {
            var commandList = new List<ICommand>();
            foreach (var el in animations)
            {
                if (hasVAtCurrentTime)
                {
                    commandList.Add(new RemoveKeyframeCommand(new Tuple<double, ICurve>(App.Current.Model.GlobalTime, el.Value), App.Current.Model.GlobalTime));
                }
                else
                {
                    double time = App.Current.Model.GlobalTime;
                    double value = Core.Curve.Utils.GetCurrentValueAtTime(el.Key, time);
                    commandList.Add(new AddOrUpdateKeyframeCommand(time, value, el.Key));
                }
            }
            if (commandList.Any())
                App.Current.UndoRedoStack.AddAndExecute(new MacroCommand("ClickedCurrentKeyCommand", commandList));
        }

        private void ClickedNextKey(object sender, RoutedEventArgs e)
        {
            double? smalestNextKey = null;
            foreach (var el in m_Animations)
            {
                double? nextKey = el.Value.GetNextU(App.Current.Model.GlobalTime);
                if (nextKey.HasValue && !smalestNextKey.HasValue)
                    smalestNextKey = nextKey;
                else if (nextKey.HasValue && smalestNextKey.HasValue)
                    smalestNextKey = Math.Min(smalestNextKey.Value, nextKey.Value);
            }

            if (smalestNextKey.HasValue)
                App.Current.Model.GlobalTime = smalestNextKey.Value;
        }

        private void GlobalTimeChangedHandler(object o, EventArgs e)
        {
            UpdateControls();
        }

        private void OperatorPartModifiedHandler(object o, EventArgs e)
        {
            RebuiltAnimationContainer();
            UpdateControls();
        }

        private void CurveChangedHandler(object o, EventArgs e)
        {
            UpdateControls();
        }

        private void UpdateControls()
        {
            bool existVBefore = false;
            bool existVAfter = false;
            bool hasVAt = false;
            foreach (var el in m_Animations)
            {
                existVBefore |= el.Value.ExistVBefore(App.Current.Model.GlobalTime);
                existVAfter |= el.Value.ExistVAfter(App.Current.Model.GlobalTime);
                hasVAt |= el.Value.HasVAt(App.Current.Model.GlobalTime);
            }

            if (existVBefore)
            {
                PreviousKeyframeImage.Source = m_PreviousKeyOnImage;
                PreviousKeyframe.IsEnabled = true;
            }
            else
            {
                PreviousKeyframeImage.Source = m_PreviousKeyOffImage;
                PreviousKeyframe.IsEnabled = false;
            }

            if (existVAfter)
            {
                NextKeyframeImage.Source = m_NextKeyOnImage;
                NextKeyframe.IsEnabled = true;
            }
            else
            {
                NextKeyframeImage.Source = m_NextKeyOffImage;
                NextKeyframe.IsEnabled = false;
            }

            if (hasVAt)
                CurrentKeyframeImage.Source = m_CurrentKeyOnImage;
            else
                CurrentKeyframeImage.Source = m_CurrentKeyOffImage;
        }

        private void ClickedNoAnimation(object sender, RoutedEventArgs e)
        {
            var animations = new Dictionary<OperatorPart, ICurve>(m_Animations);
            // todo: make this ONE command for all animations
            foreach (var el in animations)
            {
                var lastValue = Core.Curve.Utils.GetCurrentValueAtTime(el.Key, App.Current.Model.GlobalTime);
                App.Current.UndoRedoStack.AddAndExecute(new RemoveAnimationCommand(el.Key, lastValue));
            }
        }

        private void RebuiltAnimationContainer()
        {
            foreach (var el in m_Animations)
                el.Value.ChangedEvent -= CurveChangedHandler;

            m_Animations.Clear();
            foreach (var opPart in m_OperatorParts)
            {
                OperatorPart animationOpPart = Animation.GetRegardingAnimationOpPart(opPart);
                if (animationOpPart == null)
                    continue;

                var curve = animationOpPart.Func as ICurve;
                if (curve != null)
                    m_Animations[opPart] = curve;
            }

            foreach (var el in m_Animations)
                el.Value.ChangedEvent += CurveChangedHandler;
        }

        private List<OperatorPart> m_OperatorParts = new List<OperatorPart>();
        private Dictionary<OperatorPart, ICurve> m_Animations = new Dictionary<OperatorPart, ICurve>();

        static private BitmapImage m_CurrentKeyOnImage = new BitmapImage(new Uri("/Images/icon-key-on.png", UriKind.Relative)) { DecodePixelHeight = 32, DecodePixelWidth = 32, CacheOption = BitmapCacheOption.OnLoad };
        static private BitmapImage m_CurrentKeyOffImage = new BitmapImage(new Uri("/Images/icon-key-off.png", UriKind.Relative)) { DecodePixelHeight = 32, DecodePixelWidth = 32, CacheOption = BitmapCacheOption.OnLoad };
        static private BitmapImage m_PreviousKeyOnImage = new BitmapImage(new Uri("/Images/icon-previous-on.png", UriKind.Relative)) { DecodePixelHeight = 32, DecodePixelWidth = 32, CacheOption = BitmapCacheOption.OnLoad };
        static private BitmapImage m_PreviousKeyOffImage = new BitmapImage(new Uri("/Images/icon-previous-off.png", UriKind.Relative)) { DecodePixelHeight = 32, DecodePixelWidth = 32, CacheOption = BitmapCacheOption.OnLoad };
        static private BitmapImage m_NextKeyOnImage = new BitmapImage(new Uri("/Images/icon-next-on.png", UriKind.Relative)) { DecodePixelHeight = 32, DecodePixelWidth = 32, CacheOption = BitmapCacheOption.OnLoad };
        static private BitmapImage m_NextKeyOffImage = new BitmapImage(new Uri("/Images/icon-next-off.png", UriKind.Relative)) { DecodePixelHeight = 32, DecodePixelWidth = 32, CacheOption = BitmapCacheOption.OnLoad };

    }
}
