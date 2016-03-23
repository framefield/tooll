// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using SharpDX;
using SharpDX.Direct3D11;
using System.Xml.Linq;
using System.Globalization;


namespace Framefield.Core
{

    public abstract class VertexAttribute
    {
        public static VertexAttribute Create(XElement element, string name, SharpDX.DXGI.Format type)
        {
            switch (type)
            {
                case SharpDX.DXGI.Format.R32G32B32_Float:
                    return new Vector3VertexAttribute(element, name, type);
                case SharpDX.DXGI.Format.R32G32_Float:
                    return new Vector2VertexAttribute(element, name, type);
                case SharpDX.DXGI.Format.R32G32B32A32_Float:
                    return new Vector4VertexAttribute(element, name, type);
                case SharpDX.DXGI.Format.R8G8B8A8_UInt:
                    return new ColorVertexAttribute(element, name, type);
            }

            return null;
        }

        protected VertexAttribute() { }

        protected VertexAttribute(string name, SharpDX.DXGI.Format type)
        {
            this.name = name;
            this.type = type;
        }
        public string Name { get { return name; } }
        public SharpDX.DXGI.Format Type { get { return type; } }

        public abstract InputElement GetInputElement(ref int offset);
        public abstract void WriteToStream(DataStream stream, int index);
        public abstract int Size { get; }

        private string name = "";
        private SharpDX.DXGI.Format type = SharpDX.DXGI.Format.Unknown;
    }


    class Vector2VertexAttribute : VertexAttribute
    {
        public Vector2VertexAttribute() { }

        public Vector2VertexAttribute(Vector2[] values, string name, SharpDX.DXGI.Format type)
            : base(name, type)
        {
            data = (Vector2[]) values.Clone();
        }

        public Vector2VertexAttribute(XElement element, string name, SharpDX.DXGI.Format type)
            : base(name, type)
        {
            var attributes = element.Value.Replace('\n', ' ').Split(new char[] { ',' });
            data = new Vector2[attributes.Length];
            for (int i = 0; i < attributes.Length; ++i)
            {
                var attributeValues = attributes[i].Trim().Split(new char[] { ' ' });
                // cynic: the '1.0f -' is a hack, i think to get the right correction value we've to
                //        scan for the max y value and use this as complement point
                //        also this correction is now done for all 2 float type, as this is currently
                //        only the texcoord it's ok for now...
                data[i] = new Vector2(float.Parse(attributeValues[0], CultureInfo.InvariantCulture.NumberFormat),
                                      1.0f - float.Parse(attributeValues[1], CultureInfo.InvariantCulture.NumberFormat));
                //          System.Diagnostics.Debug.WriteLine(value);
            }
        }

        public override InputElement GetInputElement(ref int offset)
        {
            int prevOffset = offset;
            offset += 8;
            return new InputElement(Name, 0, Type, prevOffset, 0);
        }

        public override void WriteToStream(DataStream stream, int index)
        {
            stream.Write(data[index]);
        }

        public override int Size { get { return 2*sizeof(float); } }

        private Vector2[] data = null;
    }


    internal class Vector3VertexAttribute : VertexAttribute
    {
        public Vector3VertexAttribute()
        {
        }

        public Vector3VertexAttribute(Vector3[] values, string name, SharpDX.DXGI.Format type)
            : base(name, type)
        {
            data = (Vector3[]) values.Clone();
        }

        public Vector3VertexAttribute(XElement element, string name, SharpDX.DXGI.Format type)
            : base(name, type)
        {
            var attributes = element.Value.Replace('\n', ' ').Split(new char[] { ',' });
            data = new Vector3[attributes.Length];
            for (int i = 0; i < attributes.Length; ++i)
            {
                var attributeValues = attributes[i].Trim().Split(new char[] { ' ' });
                data[i] = new Vector3(float.Parse(attributeValues[0], CultureInfo.InvariantCulture.NumberFormat),
                                      float.Parse(attributeValues[1], CultureInfo.InvariantCulture.NumberFormat),
                                      float.Parse(attributeValues[2], CultureInfo.InvariantCulture.NumberFormat));
                //          System.Diagnostics.Debug.WriteLine(value);
            }
        }

        public override InputElement GetInputElement(ref int offset)
        {
            int prevOffset = offset;
            offset += 12;
            return new InputElement(Name, 0, Type, prevOffset, 0);
        }

        public override void WriteToStream(DataStream stream, int index)
        {
            stream.Write(data[index]);
        }

        public override int Size { get { return 3*sizeof(float); } }

        private Vector3[] data = null;
    }


    internal class Vector4VertexAttribute : VertexAttribute
    {
        public Vector4VertexAttribute()
        {
        }

        public Vector4VertexAttribute(Vector3[] values, string name, SharpDX.DXGI.Format type)
            : base(name, type)
        {
            data = (Vector4[]) values.Clone();
        }

        public Vector4VertexAttribute(XElement element, string name, SharpDX.DXGI.Format type)
            : base(name, type)
        {
            var attributes = element.Value.Replace('\n', ' ').Split(new char[] { ',' });
            data = new Vector4[attributes.Length];
            for (int i = 0; i < attributes.Length; ++i)
            {
                var attributeValues = attributes[i].Trim().Split(new char[] { ' ' });
                data[i] = new Vector4(float.Parse(attributeValues[0], CultureInfo.InvariantCulture.NumberFormat),
                                      float.Parse(attributeValues[1], CultureInfo.InvariantCulture.NumberFormat),
                                      float.Parse(attributeValues[2], CultureInfo.InvariantCulture.NumberFormat),
                                      float.Parse(attributeValues[3], CultureInfo.InvariantCulture.NumberFormat));
                //          System.Diagnostics.Debug.WriteLine(value);
            }
        }

        public override InputElement GetInputElement(ref int offset)
        {
            int prevOffset = offset;
            offset += 16;
            return new InputElement(Name, 0, Type, prevOffset, 0);
        }

        public override void WriteToStream(DataStream stream, int index)
        {
            stream.Write(data[index]);
        }

        public override int Size { get { return 4*sizeof(float); } }

        private Vector4[] data = null;
    }


    internal class ColorVertexAttribute : VertexAttribute
    {
        public ColorVertexAttribute()
        {
        }

        public ColorVertexAttribute(Vector4[] values, string name, SharpDX.DXGI.Format type) : base(name, type)
        {
            data = (Vector4[]) values.Clone();
        }

        public ColorVertexAttribute(XElement element, string name, SharpDX.DXGI.Format type)
            : base(name, SharpDX.DXGI.Format.R32G32B32A32_Float)
        {
            var attributes = element.Value.Replace('\n', ' ').Split(new char[] { ',' });
            data = new Vector4[attributes.Length];
            for (int i = 0; i < attributes.Length; ++i)
            {
                var attributeValues = attributes[i].Trim().Split(new char[] { ' ' });
                data[i] = new Vector4(float.Parse(attributeValues[0])/255.0f,
                                      float.Parse(attributeValues[1])/255.0f,
                                      float.Parse(attributeValues[2])/255.0f,
                                      float.Parse(attributeValues[3])/255.0f);
                //          System.Diagnostics.Debug.WriteLine(value);
            }
        }

        public override InputElement GetInputElement(ref int offset)
        {
            int prevOffset = offset;
            offset += 16;
            return new InputElement(Name, 0, Type, prevOffset, 0);
        }

        public override void WriteToStream(DataStream stream, int index)
        {
            stream.Write(data[index]);
        }

        public override int Size { get { return 4*sizeof(float); } }

        private Vector4[] data = null;
    }

}
