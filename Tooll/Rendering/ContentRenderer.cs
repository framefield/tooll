using Framefield.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framefield.Tooll.Rendering
{
    /** Provides a united interface to render different content types like Scenes, Meshes and Images.  */
    class ContentRenderer
    {
        public ContentRenderer(ContentRendererConfiguration renderConfiguration)
        {
            _renderConfiguration = renderConfiguration;
        }

        public void SetupRendering()
        {
            if (_D3DImageContainer == null)
                _D3DImageContainer = new D3DImageSharpDX();

            _renderSetup = new D3DRenderSetup(_renderConfiguration);
            _D3DImageContainer.SetBackBufferSharpDX(_renderSetup.SharedTexture);

            var contextSettings = new ContextSettings();
            contextSettings.DisplayMode = new SharpDX.Direct3D9.DisplayMode()
            {
                Width = _renderConfiguration.Width,
                Height = _renderConfiguration.Height,
                RefreshRate = 60,
                Format = D3DImageSharpDX.TranslateFormat(_renderSetup.SharedTexture)
            };
            contextSettings.AspectRatio = contextSettings.DisplayMode.AspectRatio;
            _defaultContext = OperatorPartContext.createDefault(contextSettings);
        }

        public void Reinitialize()
        {
            if (_renderSetup == null)
                return;

            _renderSetup.Resize(_renderConfiguration.Width, _renderConfiguration.Height);
            _D3DImageContainer.SetBackBufferSharpDX(_renderSetup.SharedTexture);

            var contextSettings = new ContextSettings();
            contextSettings.DisplayMode = new SharpDX.Direct3D9.DisplayMode()
            {
                Width = _renderConfiguration.Width,
                Height = _renderConfiguration.Height,
                RefreshRate = 60,
                Format = D3DImageSharpDX.TranslateFormat(_renderSetup.SharedTexture)
            };
            contextSettings.AspectRatio = contextSettings.DisplayMode.AspectRatio;

            if (_renderConfiguration.Operator != null && _renderConfiguration.Operator.Outputs.Count > 0)
            {
                var invalidator = new OperatorPart.InvalidateVariableAccessors("AspectRatio");
                _renderConfiguration.Operator.Outputs[0].TraverseWithFunction(null, invalidator);
            }

            RenderContent();
        }



        public void RenderContent()
        {
            if (_renderConfiguration.Operator == null || _renderConfiguration.Operator.Outputs.Count <= 0)
                return;

            D3DDevice.BeginFrame();

            try
            {
                var context = new OperatorPartContext(_defaultContext, (float)App.Current.Model.GlobalTime);

                var invalidator = new OperatorPart.InvalidateInvalidatables();
                _renderConfiguration.Operator.Outputs[_renderConfiguration.ShownOutputIndex].TraverseWithFunctionUseSpecificBehavior(null, invalidator);

                _renderSetup.RenderedOperator = _renderConfiguration.Operator;

                var evaluationType = _renderConfiguration.Operator.Outputs[_renderConfiguration.ShownOutputIndex].Type;

                switch (evaluationType)
                {
                    case FunctionType.Scene:
                        Action<OperatorPartContext, int> lambdaForScenes = (OperatorPartContext context2, int outputIdx) =>
                        {
                            _renderConfiguration.Operator.Outputs[outputIdx].Eval(context);
                        };
                        _renderSetup.RenderGeometry(
                            context,
                            lambdaForScenes);

                        break;

                    case FunctionType.Mesh:
                        {
                            Action<OperatorPartContext, int> lambdaForMeshes = (OperatorPartContext context2, int outputIdx) =>
                            {
                                var mesh = _renderConfiguration.Operator.Outputs[outputIdx].Eval(context2).Mesh;
                                context2.Renderer.SetupEffect(context2);
                                context2.Renderer.Render(mesh, context2);
                            };
                            _renderSetup.RenderGeometry(
                                context,
                                lambdaForMeshes);
                            break;
                        }

                    case FunctionType.Image:
                        _renderSetup.SetupContextForRenderingImage(
                            context,
                            _renderConfiguration.RenderWithGammaCorrection);

                        var image = _renderConfiguration.Operator.Outputs[_renderConfiguration.ShownOutputIndex].Eval(new OperatorPartContext(context)).Image;
                        if (image == null)
                            break;

                        RenderedImageIsACubemap = image.Description.ArraySize > 1;
                        var cubeMapSide = RenderedImageIsACubemap ? _renderConfiguration.PreferredCubeMapSideIndex : -1;
                        if (cubeMapSide == 6)
                        {
                            _renderSetup.RenderCubemapAsSphere(image, context);
                        }
                        else
                        {
                            _renderSetup.RenderImage(image, context);
                        }
                        break;
                }
                _D3DImageContainer.InvalidateD3DImage();

            }
            catch (Exception exception)
            {
                Logger.Error(exception.ToString());
            }
        }


        public void CleanUp()
        {
            Utilities.DisposeObj(ref _D3DImageContainer);
            Utilities.DisposeObj(ref _renderSetup);
        }

        /** After rendering a image this flag can be used to display UI-elements relevant for CubeMaps */
        public bool RenderedImageIsACubemap { get; private set; }

        private ContentRendererConfiguration _renderConfiguration;

        public D3DImageSharpDX D3DImageContainer { get { return _D3DImageContainer; } }
        private D3DImageSharpDX _D3DImageContainer;

        public OperatorPartContext DefaultContext { get { return _defaultContext; } }
        private OperatorPartContext _defaultContext;

        public D3DRenderSetup RenderSetup { get { return _renderSetup; } set { _renderSetup = value; } }  // make D3DRenderSetup available for Fullscreen-View
        private D3DRenderSetup _renderSetup;
    }
}
