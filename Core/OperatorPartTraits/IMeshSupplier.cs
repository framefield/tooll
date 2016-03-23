// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Collections.Generic;

namespace Framefield.Core.OperatorPartTraits
{

    public interface IMeshSupplier
    {
        void AddMeshesTo(ICollection<Mesh> meshes); // Mesh supplier adds their meshes within this method
    }

}
