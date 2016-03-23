// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Framefield.Core;
using Framefield.Core.OperatorPartTraits;
using Newtonsoft.Json;

namespace Framefield.Tooll.Components.SelectionView
{
    public partial class ShowAsTextControl
    {

        public ShowAsTextControl()
        {
            Loaded += OnLoadedHandler;
            Unloaded += OnUnloadedHandler;
            InitializeComponent();
        }

        public void SetOperatorAndOutput(Operator op, int outputIndex = 0)
        {
            _operator = op;
            _shownOutputIndex = outputIndex;
            if (IsLoaded)
                RenderContent();
        }



        private void OnLoadedHandler(object sender, RoutedEventArgs e)
        {
            App.Current.UpdateAfterUserInteractionEvent += App_UpdateAfterUserInteractionHandler;
            App.Current.UpdateRequiredAfterUserInteraction = true;

            var contextSettings = new ContextSettings();
            _defaultContext = OperatorPartContext.createDefault(contextSettings);

        }

        private void OnUnloadedHandler(object sender, RoutedEventArgs e)
        {
            if (App.Current == null) 
                return;

            App.Current.UpdateAfterUserInteractionEvent -= App_UpdateAfterUserInteractionHandler;
        }

        void App_UpdateAfterUserInteractionHandler(object sender, EventArgs e)
        {
            RenderContent();
        }


        private void RenderContent()
        {
            if (!IsVisible)
                return;

            if (_operator == null || _operator.Outputs.Count <= 0)
                return;

            try
            {
                var context = new OperatorPartContext(_defaultContext, (float)App.Current.Model.GlobalTime);

                // FIXME: the following lines are commented out to enable different values for debugOverlay-Variable
                //if (context.Time != _previousTime)
                //{
                var invalidator = new OperatorPart.InvalidateInvalidatables();
                _operator.Outputs[_shownOutputIndex].TraverseWithFunctionUseSpecificBehavior(null, invalidator);
                //_previousTime = context.Time;
                //}

                var evaluationType = _operator.Outputs[_shownOutputIndex].Type;
                switch (evaluationType)
                {
                    case FunctionType.Float:
                        XValueLabel.Text = _operator.Outputs[_shownOutputIndex].Eval(context).Value.ToString(CultureInfo.InvariantCulture);
                        break;
                    case FunctionType.Text:
                        XValueLabel.Text = _operator.Outputs[_shownOutputIndex].Eval(context).Text;
                        break;
                    case FunctionType.Dynamic:
                        var result = _operator.Outputs[_shownOutputIndex].Eval(context).Dynamic;
                        using (var stringWriter = new StringWriter())
                        using (var jsonTextWriter = new JsonTextWriter(stringWriter)
                                                        {
                                                            QuoteName = false,
                                                            Formatting = Formatting.Indented
                                                        })
                        {
                            _serializer.Serialize(jsonTextWriter, result);
                            string s = stringWriter.ToString();
                            if (s.Length > 1024)
                                s = s.Substring(0, 1024) + "..";
                            XValueLabel.Text = s;
                        }
                        break;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception.ToString());
            }
        }


        private Operator _operator;
        private int _shownOutputIndex;
        
        private OperatorPartContext _defaultContext;
        private readonly JsonSerializer _serializer = new JsonSerializer();
    }
}