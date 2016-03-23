// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Framefield.Core;
using Framefield.Core.Curve;


namespace Framefield.Tooll.Components.SelectionView
{
    public partial class ShowCurveControl
    {
        public ShowCurveControl()
        {
            Loaded += Window_Loaded;
            Unloaded += Window_Unloaded;
            InitializeComponent();
        }

        public ICurve Curve
        {
            get { return _curve; }
            set
            {
                if (value is OperatorPart.Function)
                {
                    _curve = value;
                    _curveOp = (_curve as OperatorPart.Function).OperatorPart.Parent;
                }
                else
                {
                    _curve = null;
                    _curveOp = null;
                }

                var curves = new List<ICurve>();
                if (_curve != null)
                    curves.Add(_curve);

                XCurveEditor.SetCurveOperators(curves);

                if (IsLoaded)
                    UpdateShownContent();
            }
        }

        private void RefreshUiEventHandler(object sender, EventArgs e)
        {
            UpdateShownContent();
        }

        private void UpdateShownContent()
        {
            if (!IsVisible || _curveOp == null)
                return;

            var context = new OperatorPartContext(_defaultContext, (float) App.Current.Model.GlobalTime);

            if (Math.Abs(context.Time - _previousTime) > Constants.Epsilon)
            {
                var invalidator = new OperatorPart.InvalidateInvalidatables();
                _curveOp.Outputs[0].TraverseWithFunctionUseSpecificBehavior(null, invalidator);
                _previousTime = context.Time;
            }

            _curveOp.Outputs[0].Eval(context);

            XCurveEditor.UpdateEditBox();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _defaultContext = OperatorPartContext.createDefault(new ContextSettings());

            App.Current.UpdateAfterUserInteractionEvent += RefreshUiEventHandler;
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Current.UpdateAfterUserInteractionEvent -= RefreshUiEventHandler;
        }

        private ICurve _curve;
        private Operator _curveOp;
        private float _previousTime;
        private OperatorPartContext _defaultContext;
    }
}
