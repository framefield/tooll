using Framefield.Core;
using Framefield.Core.OperatorPartTraits;
using Framefield.Tooll.Components.SelectionView.ShowScene.CameraInteraction;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framefield.Tooll.Rendering
{
    /** Manages switching between internel ViewerCameraSetup and selected CameraOperators. */
    public class ViewCameraSetupProvider
    {
        public ViewCameraSetupProvider(RenderViewConfiguration renderConfig)
        {
            _renderConfig = renderConfig;
            _setupForACameraOperator.AttributeChangedEvent += CameraAttributeChangedHandler;

            SetSelectedOperator(renderConfig.Operator);     // Initialize
            _renderConfig.CameraSetup = _activeSetup;        // complete renderConfig
        }


        public void SetSelectedOperator(Operator newOperator)
        {
            var opIsCamera = newOperator != null
                && newOperator.InternalParts.Count > 0
                && newOperator.InternalParts[0].Func is ICameraProvider;

            _operatorCameraProvider = opIsCamera ? newOperator.InternalParts[0].Func as ICameraProvider
                                                 : null;

            _activeSetup = opIsCamera ? _setupForACameraOperator
                                      : _setupForView;
            _renderConfig.CameraSetup = _activeSetup;
        }


        /** We need this handler to forward changes to camera setup if
         * a camera-operators is selected */
        private void CameraAttributeChangedHandler(object sender, EventArgs e)
        {
            if (_operatorCameraProvider == null)
                return;

            var t = App.Current.Model.GlobalTime;
            _operatorCameraProvider.SetPosition(t, _setupForACameraOperator.Position);
            _operatorCameraProvider.SetTarget(t, _setupForACameraOperator.Target);
        }

        private RenderViewConfiguration _renderConfig;


        public CameraSetup ActiveCameraSetup { get { return _activeSetup; } }
        CameraSetup _activeSetup;

        ICameraProvider _operatorCameraProvider;
        private CameraSetup _setupForACameraOperator = new CameraSetup();
        private CameraSetup _setupForView = new CameraSetup();

        public bool SelectedOperatorIsCamProvider
        {
            get { return _activeSetup != null && _activeSetup == _setupForACameraOperator; }
        }
    }
}
