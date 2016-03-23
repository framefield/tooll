// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using SharpDX;

namespace Framefield.Core.OperatorPartTraits
{
    public interface ISceneTransform
    {
        Matrix Transform { get; }
    }
}
