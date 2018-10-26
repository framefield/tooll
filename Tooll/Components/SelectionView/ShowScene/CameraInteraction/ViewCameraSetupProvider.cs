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

            OperatorCameraProvider = opIsCamera ? newOperator.InternalParts[0].Func as ICameraProvider
                                                 : null;

            _activeSetup = opIsCamera ? _setupForACameraOperator
                                      : _setupForView;
            _renderConfig.CameraSetup = _activeSetup;
        }


        /** We need this handler to forward changes to camera setup if
         * a camera-operators is selected */
        private void CameraAttributeChangedHandler(object sender, EventArgs e)
        {
            if (OperatorCameraProvider == null)
                return;

            var t = App.Current.Model.GlobalTime;

            OperatorCameraProvider.SetPosition(t, _setupForACameraOperator.Position);
            OperatorCameraProvider.SetTarget(t, _setupForACameraOperator.Target);
        }



        public CameraSetup ActiveCameraSetup { get { return _activeSetup; } }
        CameraSetup _activeSetup;

        public ICameraProvider OperatorCameraProvider { get; set; }

        public bool SelectedOperatorIsCamProvider
        {
            get { return _activeSetup != null && _activeSetup == _setupForACameraOperator; }
        }


        private RenderViewConfiguration _renderConfig;
        private CameraSetup _setupForACameraOperator = new CameraSetup(isViewCamera: false);
        private CameraSetup _setupForView = new CameraSetup(isViewCamera: true);
    }
}
