// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Collections.Generic;
using System.Linq;
using Framefield.Core.OperatorPartTraits;
using SharpDX;


namespace Framefield.Core.Rendering
{

    public class MeshCollector : OperatorPart.IPreTraverseEvaluator, OperatorPart.IPostTraverseEvaluator
    {
        public Dictionary<Matrix, ICollection<Mesh>> CollectedMeshes { get; private set; }
        public Mesh FirstMeshOrDefault
        {
            get
            {
                var meshEntry = CollectedMeshes.FirstOrDefault();
                return meshEntry.Value == null ? null : meshEntry.Value.FirstOrDefault();
            }
        }

        public int NumberOfCollectedMeshes
        {
            get
            {
                return (from meshEntry in CollectedMeshes
                        from mesh in meshEntry.Value
                        select mesh).Count();
            }
        }

        public MeshCollector(OperatorPart.Function parent)
        {
            _parent = parent;
            CollectedMeshes = new Dictionary<Matrix, ICollection<Mesh>>();
        }

        public void Collect(OperatorPart startPoint)
        {
            Clear();
            startPoint.TraverseWithFunction(this, this);
            if (!CollectedMeshes.Any())
            {
                Logger.Warn(_parent, "Found no mesh supplier, have you forgotten to add an input?");
            }
        }

        public void ChildrenStart() {}
        public void ChildrenEnd() {}

        public void PreEvaluate(OperatorPart opPart)
        {
            if (opPart.Disabled)
                return;

            var transformFunc = opPart.Func as ISceneTransform;
            if (transformFunc != null)
            {
                _transforms.Push(transformFunc.Transform * _transforms.Peek());
            }

            var meshSupplier = opPart.Func as IMeshSupplier;
            if (meshSupplier != null)
            {
                if (!CollectedMeshes.ContainsKey(_transforms.Peek()))
                    CollectedMeshes.Add(_transforms.Peek(), new List<Mesh>());
                meshSupplier.AddMeshesTo(CollectedMeshes[_transforms.Peek()]);
            }
        }

        public void PostEvaluate(OperatorPart opPart)
        {
            var transformFunc = opPart.Func as ISceneTransform;
            if (transformFunc != null)
            {
                _transforms.Pop();
            }
        }

        public void AlreadyVisited(OperatorPart opPart)
        {
        }

        private void Clear()
        {
            CollectedMeshes.Clear();
            _transforms.Clear();
            _transforms.Push(Matrix.Identity);
        }

        private readonly Stack<Matrix> _transforms = new Stack<Matrix>();
        private readonly OperatorPart.Function _parent;
    }

}

