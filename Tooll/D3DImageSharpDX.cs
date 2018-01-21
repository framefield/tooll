// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using SharpDX.Direct3D9;

namespace Framefield.Tooll
{
    public class D3DImageSharpDX : D3DImage, IDisposable
    {
        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        static int NumActiveImages = 0;
        static Direct3DEx D3DContext;
        static DeviceEx D3DDevice;

        public Texture SharedTexture;

        public D3DImageSharpDX()
        {
            InitD3D9();
            NumActiveImages++;
        }

        public void Dispose()
        {
            SetBackBufferSharpDX(null);
            if (SharedTexture != null)
            {
                SharedTexture.Dispose();
                SharedTexture = null;
            }

            NumActiveImages--;
            ShutdownD3D9();
        }

        public void InvalidateD3DImage()
        {
            if (SharedTexture != null)
            {
                Lock();
                AddDirtyRect(new Int32Rect(0, 0, PixelWidth, PixelHeight));
                Unlock();
            }
        }

        public void SetBackBufferSharpDX(SharpDX.Direct3D11.Texture2D Texture)
        {
            if (SharedTexture != null)
            {
                SharedTexture.Dispose();
                SharedTexture = null;
            }

            if (Texture == null)
            {
                if (SharedTexture != null)
                {
                    SharedTexture = null;
                    Lock();
                    SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                    Unlock();
                }
            }
            else if (IsShareable(Texture))
            {
                Format format = TranslateFormat(Texture);
                if (format == Format.Unknown)
                    throw new ArgumentException("Texture format is not compatible with OpenSharedResource");

                IntPtr Handle = GetSharedHandle(Texture);
                if (Handle == IntPtr.Zero)
                    throw new ArgumentNullException("Handle");

                SharedTexture = new Texture(D3DDevice, Texture.Description.Width, Texture.Description.Height, 1, Usage.RenderTarget, format, Pool.Default, ref Handle);
                using (Surface Surface = SharedTexture.GetSurfaceLevel(0))
                {
                    Lock();
                    SetBackBuffer(D3DResourceType.IDirect3DSurface9, Surface.NativePointer);
                    Unlock();
                }
            }
            else
                throw new ArgumentException("Texture must be created with ResourceOptionFlags.Shared");
        }

        void InitD3D9()
        {
            if (NumActiveImages == 0)
            {
                D3DContext = new Direct3DEx();

                PresentParameters presentparams = new PresentParameters();
                presentparams.Windowed = true;
                presentparams.SwapEffect = SwapEffect.Discard;
                presentparams.DeviceWindowHandle = GetDesktopWindow();
                presentparams.PresentationInterval = PresentInterval.Immediate;
                presentparams.MultiSampleQuality = 0;
                presentparams.MultiSampleType = 0;

                D3DDevice = new DeviceEx(D3DContext, 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve, presentparams);
            }
        }

        void ShutdownD3D9()
        {
            if (NumActiveImages == 0)
            {
                if (SharedTexture != null)
                {
                    SharedTexture.Dispose();
                    SharedTexture = null;
                }

                if (D3DDevice != null)
                {
                    D3DDevice.Dispose();
                    D3DDevice = null;
                }

                if (D3DContext != null)
                {
                    D3DContext.Dispose();
                    D3DContext = null;
                }
            }
        }

        IntPtr GetSharedHandle(SharpDX.Direct3D11.Texture2D Texture)
        {
            SharpDX.DXGI.Resource resource = Texture.QueryInterface<SharpDX.DXGI.Resource>();
            IntPtr result = resource.SharedHandle;

            resource.Dispose();

            return result;
        }

        public static Format TranslateFormat(SharpDX.Direct3D11.Texture2D Texture)
        {
            if (Texture == null)
                return SharpDX.Direct3D9.Format.Unknown;

            switch (Texture.Description.Format)
            {
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
                    return SharpDX.Direct3D9.Format.A2B10G10R10;

                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                    return SharpDX.Direct3D9.Format.A16B16G16R16F;

                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                    return SharpDX.Direct3D9.Format.A8R8G8B8;

                default:
                    return SharpDX.Direct3D9.Format.Unknown;
            }
        }

        bool IsShareable(SharpDX.Direct3D11.Texture2D Texture)
        {
            return (Texture.Description.OptionFlags & SharpDX.Direct3D11.ResourceOptionFlags.Shared) != 0;
        }
    }
}
