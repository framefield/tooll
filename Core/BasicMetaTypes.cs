// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framefield.Core
{
    public class BasicMetaTypes
    {
        private static MetaOperatorPart Create(string guid, FunctionType type)
        {
            Func<Guid, OperatorPart.Function, bool, string, OperatorPart> createFunc = (id, defaultFunction, isMultiInput, name) =>
                                                                                       {
                                                                                           var opPart = Utilities.CreateValueOpPart(id, defaultFunction as Utilities.ValueFunction,
                                                                                                                                    isMultiInput);
                                                                                           opPart.Type = type;
                                                                                           opPart.Name = name;
                                                                                           return opPart;
                                                                                       };
            return new MetaOperatorPart(Guid.Parse(guid)) { CreateFunc = createFunc, Type = type };
        }

        public static MetaOperatorPart GenericMeta { get { return _genericMeta; } }
        public static MetaOperatorPart FloatMeta { get { return _floatMeta; } }
        public static MetaOperatorPart TextMeta { get { return _textMeta; } }
        public static MetaOperatorPart SceneMeta { get { return _sceneMeta; } }
        public static MetaOperatorPart ImageMeta { get { return _imageMeta; } }
        public static MetaOperatorPart DynamicMeta { get { return _dynamicMeta; } }
        public static MetaOperatorPart MeshMeta { get { return _meshMeta; } }

        public static MetaOperatorPart GetMetaOperatorPartOf(FunctionType funcType)
        {
            switch (funcType)
            {
                case FunctionType.Generic : return GenericMeta;
                case FunctionType.Float : return FloatMeta;
                case FunctionType.Text : return TextMeta;
                case FunctionType.Scene: return SceneMeta;
                case FunctionType.Image: return ImageMeta;
                case FunctionType.Dynamic: return DynamicMeta;
                case FunctionType.Mesh: return MeshMeta;
            }
            return null;
        }

        private static readonly MetaOperatorPart _genericMeta = Create("{9F831CF2-A1EC-41F4-BA80-CCED9736AF6B}", FunctionType.Generic);
        private static readonly MetaOperatorPart _floatMeta = Create("{3F76DEE3-3897-44AC-82D6-25CE9F53A506}", FunctionType.Float);
        private static readonly MetaOperatorPart _textMeta = Create("{C522A66E-3260-4692-B3E3-79FD0361FA3D}", FunctionType.Text);
        private static readonly MetaOperatorPart _sceneMeta = Create("{79122951-7BC4-4C68-B085-866EAB828248}", FunctionType.Scene);
        private static readonly MetaOperatorPart _imageMeta = Create("{9848060D-FD84-45B0-B658-D0D531C61DAB}", FunctionType.Image);
        private static readonly MetaOperatorPart _dynamicMeta = Create("{9701D534-B3FF-4889-A250-84AECE4A7D76}", FunctionType.Dynamic);
        private static readonly MetaOperatorPart _meshMeta = Create("{CC257632-61CE-4950-ACF1-8A25FA3E2206}", FunctionType.Mesh);
    }
}
