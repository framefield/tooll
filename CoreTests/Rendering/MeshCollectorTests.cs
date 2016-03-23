// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using Framefield.Core;
using Framefield.Core.OperatorPartTraits;
using Framefield.Core.Rendering;
using Utilities = Framefield.Core.Utilities;

namespace CoreTests.Rendering
{
    [TestClass]
    public class MeshCollectorTests
    {
        class ParentFunc : OperatorPart.Function
        {
            public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
            {
                return context;
            }
        }

        class MeshSupplier1Mesh : OperatorPart.Function, IMeshSupplier
        {
            public void AddMeshesTo(ICollection<Mesh> meshes)
            {
                meshes.Add(new Mesh());
            }

            public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
            {
                return context;
            }
        }

        class MeshSupplier2Meshes : OperatorPart.Function, IMeshSupplier
        {
            public void AddMeshesTo(ICollection<Mesh> meshes)
            {
                meshes.Add(new Mesh());
                meshes.Add(new Mesh());
            }

            public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
            {
                return context;
            }
        }

        class TransformX100 : OperatorPart.Function, ISceneTransform
        {
            public Matrix Transform { get { return Matrix.Translation(100, 0, 0); } }

            public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
            {
                return context;
            }
        }

        class TransformZ50 : OperatorPart.Function, ISceneTransform
        {
            public Matrix Transform { get { return Matrix.Translation(0, 0, 50); } }

            public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
            {
                return context;
            }
        }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _parentOperator = new Operator(Guid.NewGuid(), new MetaOperator(Guid.NewGuid()), new List<OperatorPart>(), new List<OperatorPart>(), new List<Operator>(), new List<OperatorPart>());
            _parentFunc = new ParentFunc();
            _parentOpPart = new OperatorPart(Guid.NewGuid(), _parentFunc) { Parent = _parentOperator };
        }

        private static Operator _parentOperator;
        private static OperatorPart _parentOpPart;
        private static ParentFunc _parentFunc;

        [TestMethod]
        public void Collect_1MeshSupplierWith1Mesh_1MeshIsFound()
        {
            var scene = new OperatorPart(Guid.NewGuid(), new Utilities.ValueFunction());
            var mesh1Supplier = new OperatorPart(Guid.NewGuid(), new MeshSupplier1Mesh());
            scene.Connections.Add(mesh1Supplier);

            var meshCollector = new MeshCollector(_parentFunc);
            meshCollector.Collect(scene);

            Assert.AreEqual(1, meshCollector.NumberOfCollectedMeshes);
        }

        [TestMethod]
        public void Collect_1MeshSupplierWith2Meshes_2MeshesAreFound()
        {
            var scene = new OperatorPart(Guid.NewGuid(), new Utilities.ValueFunction());
            var mesh2Supplier = new OperatorPart(Guid.NewGuid(), new MeshSupplier2Meshes());
            scene.Connections.Add(mesh2Supplier);

            var meshCollector = new MeshCollector(_parentFunc);
            meshCollector.Collect(scene);

            Assert.AreEqual(2, meshCollector.NumberOfCollectedMeshes);
        }

        [TestMethod]
        public void Collect_2MeshSupplierFirstWith1MeshSecondWith2Meshes_3MeshesAreFound()
        {
            var scene = new OperatorPart(Guid.NewGuid(), new Utilities.ValueFunction());
            var mesh1Supplier = new OperatorPart(Guid.NewGuid(), new MeshSupplier1Mesh());
            var mesh2Supplier = new OperatorPart(Guid.NewGuid(), new MeshSupplier2Meshes());
            scene.Connections.Add(mesh1Supplier);
            scene.Connections.Add(mesh2Supplier);

            var meshCollector = new MeshCollector(_parentFunc);
            meshCollector.Collect(scene);

            Assert.AreEqual(3, meshCollector.NumberOfCollectedMeshes);
        }

        [TestMethod]
        public void Collect_2MeshSupplierWith3MeshesOneWithTransformBefore_3MeshesAreFound()
        {
            var scene = new OperatorPart(Guid.NewGuid(), new Utilities.ValueFunction());
            var mesh1Supplier = new OperatorPart(Guid.NewGuid(), new MeshSupplier1Mesh());
            var mesh2Supplier = new OperatorPart(Guid.NewGuid(), new MeshSupplier2Meshes());
            var transformX100 = new OperatorPart(Guid.NewGuid(), new TransformX100());
            scene.Connections.Add(mesh2Supplier);
            scene.Connections.Add(transformX100);
            transformX100.Connections.Add(mesh1Supplier);

            var meshCollector = new MeshCollector(_parentFunc);
            meshCollector.Collect(scene);

            Assert.AreEqual(3, meshCollector.NumberOfCollectedMeshes);
        }

        [TestMethod]
        public void Collect_2MeshSupplierWith3MeshesOneWithTransformBefore_2TransformEntriesAreFound()
        {
            var scene = new OperatorPart(Guid.NewGuid(), new Utilities.ValueFunction());
            var mesh1Supplier = new OperatorPart(Guid.NewGuid(), new MeshSupplier1Mesh());
            var mesh2Supplier = new OperatorPart(Guid.NewGuid(), new MeshSupplier2Meshes());
            var transformX100 = new OperatorPart(Guid.NewGuid(), new TransformX100());
            scene.Connections.Add(mesh2Supplier);
            scene.Connections.Add(transformX100);
            transformX100.Connections.Add(mesh1Supplier);

            var meshCollector = new MeshCollector(_parentFunc);
            meshCollector.Collect(scene);

            Assert.AreEqual(2, meshCollector.CollectedMeshes.Count);
        }

        [TestMethod]
        public void Collect_1MeshSupplier_TransformEntryIsIdentity()
        {
            var scene = new OperatorPart(Guid.NewGuid(), new Utilities.ValueFunction());
            var mesh1Supplier = new OperatorPart(Guid.NewGuid(), new MeshSupplier1Mesh());
            scene.Connections.Add(mesh1Supplier);

            var meshCollector = new MeshCollector(_parentFunc);
            meshCollector.Collect(scene);

            Assert.AreEqual(Matrix.Identity, meshCollector.CollectedMeshes.First().Key);
        }

        [TestMethod]
        public void Collect_1MeshSupplierWithTransformX100Before_TransformEntryIsX100()
        {
            var scene = new OperatorPart(Guid.NewGuid(), new Utilities.ValueFunction());
            var mesh1Supplier = new OperatorPart(Guid.NewGuid(), new MeshSupplier1Mesh());
            var transformX100 = new OperatorPart(Guid.NewGuid(), new TransformX100());
            scene.Connections.Add(transformX100);
            transformX100.Connections.Add(mesh1Supplier);

            var meshCollector = new MeshCollector(_parentFunc);
            meshCollector.Collect(scene);

            Assert.AreEqual(Matrix.Translation(100, 0, 0), meshCollector.CollectedMeshes.First().Key);
        }

        [TestMethod]
        public void Collect_1MeshSupplierWithTransformX100AndZ50Before_TransformEntryIsX100Z50()
        {
            var scene = new OperatorPart(Guid.NewGuid(), new Utilities.ValueFunction());
            var mesh1Supplier = new OperatorPart(Guid.NewGuid(), new MeshSupplier1Mesh());
            var transformX100 = new OperatorPart(Guid.NewGuid(), new TransformX100());
            var transformZ50 = new OperatorPart(Guid.NewGuid(), new TransformZ50());
            scene.Connections.Add(transformX100);
            transformX100.Connections.Add(transformZ50);
            transformZ50.Connections.Add(mesh1Supplier);

            var meshCollector = new MeshCollector(_parentFunc);
            meshCollector.Collect(scene);

            Assert.AreEqual(Matrix.Translation(100, 0, 0)*Matrix.Translation(0, 0, 50), meshCollector.CollectedMeshes.First().Key);
        }

    }
}
