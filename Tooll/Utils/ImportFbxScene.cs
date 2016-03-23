// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using Framefield.Autodesk.FBX;
using Framefield.Core;
using Framefield.Core.Commands;
using Mesh = Framefield.Autodesk.FBX.Mesh;

namespace Framefield.Tooll.Utils
{
    public class ImportFbxScene
    {
        public void ImportFbxAsOperator()
        {
            var filename = UIHelper.PickFileWithDialog(".", ".", "Select Fbx");
            if (!filename.Any())
                return;

            var scene = Importer.Import(filename, includeTransformMatrix:false);
            if (scene == null)
                return;

            List<Node> roots = scene.CreateAsTrees();
            if (roots.Count == 0)
                return;

            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
            var targetOp = cgv.CompositionOperator;

            var posX = cgv.ScreenCenter.X;
            var posY = cgv.ScreenCenter.Y;

            var tmpX = posX;
            var importFbxCommandList = new List<ICommand>();

            for (var i = 1; i < roots.Count; ++i)
            {
                BuildTree(targetOp, importFbxCommandList,  null, null, roots[i], tmpX, posY, "");
                tmpX += TreeWidth(roots[i]);
            }

            BuildTree(targetOp, importFbxCommandList, null, null, roots[0], posX, posY + 2 * CompositionGraphView.GRID_SIZE, filename.Split('/').Last());

            var importFbxSceneCommand = new MacroCommand("ImportFbxSceneCommand", importFbxCommandList);
            App.Current.UndoRedoStack.Add(importFbxSceneCommand);
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private void BuildTree(Operator fbxOp, List<ICommand> importFbxCommandList, Operator parent, OperatorPart input, Node node, double x, double y, string rootName = "")
        {
            Guid metaIDToAdd = GetIDOfNode(node);

            var newOpName = "";

            if (node is Group)
            {
                newOpName = "+ " + node.Name;
            }
            else if (node is Transform)
            {
                newOpName = "Transform";
            }
            else if (node is TransformMatrix)
            {
                newOpName = "TransformMatrix";
            }
            else if (node is Material)
            {
                newOpName = "Material";
            }
            else
            {
                newOpName = node.Name;
            }

            if (parent == null)
            {
                newOpName += rootName;
            }

            var addOperatorCommand = new AddOperatorCommand(fbxOp, metaIDToAdd, x, y, TreeWidth(node),true,newOpName);
            importFbxCommandList.Add(addOperatorCommand);
            addOperatorCommand.Do();

            var newOp = (from o in fbxOp.InternalOps
                         where addOperatorCommand.AddedInstanceID == o.ID
                         select o).Single();

            SetupValues(newOp, node);

            if (input != null)
            {
                var newConnection = new MetaConnection(newOp.ID, newOp.Outputs[0].ID,
                    parent == null ? Guid.Empty : parent.ID, input.ID);

                var firstOccuranceOfTargetOpID = fbxOp.Definition.Connections.FindIndex(con => (con.TargetOpID == newConnection.TargetOpID) &&
                                                                                               (con.TargetOpPartID == newConnection.TargetOpPartID));
                var lastOccuranceOfTargetOpID = fbxOp.Definition.Connections.FindLastIndex(con => (con.TargetOpID == newConnection.TargetOpID) &&
                                                                                                  (con.TargetOpPartID == newConnection.TargetOpPartID));

                int index = 0;
                if (firstOccuranceOfTargetOpID > -1 && lastOccuranceOfTargetOpID > -1)
                    index = lastOccuranceOfTargetOpID - firstOccuranceOfTargetOpID + 1;

                var addConnectionCommand = new InsertConnectionCommand(fbxOp.Definition, newConnection, index);
                addConnectionCommand.Do();
                importFbxCommandList.Add(addConnectionCommand);
            }

            double childX = 0;
            foreach (Node child in node.Children)
            {
                BuildTree(fbxOp, importFbxCommandList, newOp, newOp.Inputs[0], child, x + childX, y + CompositionGraphView.GRID_SIZE);
                childX += TreeWidth(child);
            }
        }

        private double TreeWidth(Node node)
        {
            if (node == null)
                return 0;

            if (node.Children.Count == 0)
                return 100; //default op width

            double width = 0;
            foreach (Node child in node.Children)
                width += TreeWidth(child);
            return width;
        }

        private Guid GetIDOfNode(Node node)
        {
            Guid metaIDToAdd = Guid.Empty;
            if (node is Group)
                metaIDToAdd = Guid.Parse("46e0d20b-9ecc-42bc-ad5a-faeaf23e62f1");
            else if (node is Transform)
                metaIDToAdd = Guid.Parse("5f9364f8-36b4-4c1c-9cc2-5eddfa6774aa");
            else if (node is Light)
                metaIDToAdd = Guid.Parse("944a5d15-2485-479a-b850-519141237dd2");
            else if (node is Camera)
                metaIDToAdd = Guid.Parse("43403a8d-9c87-414a-89e2-9393b87d9e47");
            else if (node is Mesh)
                metaIDToAdd = Guid.Parse("fc2b869e-335a-4123-851c-9aecd3349a50");
            else if (node is Material)
                metaIDToAdd = Guid.Parse("72c0d6f1-ef64-4df6-b535-000b4b085b1e");
            else if(node is TransformMatrix)
                metaIDToAdd = Guid.Parse("b7a3b216-37c5-4c36-83c5-f1b823ce1d3f");
            return metaIDToAdd;
        }

        private void SetupValues(Operator op, Node node)
        {
            if (node is Transform)
                SetupValuesTransform(op, node as Transform);
            else if (node is TransformMatrix)
                SetupValuesTransformMatrix(op, node as TransformMatrix);
            else if (node is Light)
                SetupValuesLight(op, node as Light);
            else if (node is Camera)
                SetupValuesCamera(op, node as Camera);
            else if (node is Mesh)
                SetupValuesMesh(op, node as Mesh);
            else if (node is Material)
                SetupValuesMaterial(op, node as Material);
        }

        private void SetupValuesTransform(Operator op, Transform transform)
        {
            op.Inputs[1].Func = Utilities.CreateValueFunction(new Float(transform.Translation.X));
            op.Inputs[2].Func = Utilities.CreateValueFunction(new Float(transform.Translation.Y));
            op.Inputs[3].Func = Utilities.CreateValueFunction(new Float(transform.Translation.Z));

            op.Inputs[4].Func = Utilities.CreateValueFunction(new Float(transform.Rotation.X));
            op.Inputs[5].Func = Utilities.CreateValueFunction(new Float(transform.Rotation.Y));
            op.Inputs[6].Func = Utilities.CreateValueFunction(new Float(transform.Rotation.Z));

            op.Inputs[7].Func = Utilities.CreateValueFunction(new Float(transform.Scale.X));
            op.Inputs[8].Func = Utilities.CreateValueFunction(new Float(transform.Scale.Y));
            op.Inputs[9].Func = Utilities.CreateValueFunction(new Float(transform.Scale.Z));

            op.Inputs[10].Func = Utilities.CreateValueFunction(new Float(transform.Pivot.X));
            op.Inputs[11].Func = Utilities.CreateValueFunction(new Float(transform.Pivot.Y));
            op.Inputs[12].Func = Utilities.CreateValueFunction(new Float(transform.Pivot.Z));
        }

        private void SetupValuesTransformMatrix(Operator op, TransformMatrix matrix)
        {
            op.Inputs[1].Func = Utilities.CreateValueFunction(new Float(matrix.Row0.X));
            op.Inputs[2].Func = Utilities.CreateValueFunction(new Float(matrix.Row0.Y));
            op.Inputs[3].Func = Utilities.CreateValueFunction(new Float(matrix.Row0.Z));
            op.Inputs[4].Func = Utilities.CreateValueFunction(new Float(matrix.Row0.W));

            op.Inputs[5].Func = Utilities.CreateValueFunction(new Float(matrix.Row1.X));
            op.Inputs[6].Func = Utilities.CreateValueFunction(new Float(matrix.Row1.Y));
            op.Inputs[7].Func = Utilities.CreateValueFunction(new Float(matrix.Row1.Z));
            op.Inputs[8].Func = Utilities.CreateValueFunction(new Float(matrix.Row1.W));

            op.Inputs[9].Func = Utilities.CreateValueFunction(new Float(matrix.Row2.X));
            op.Inputs[10].Func = Utilities.CreateValueFunction(new Float(matrix.Row2.Y));
            op.Inputs[11].Func = Utilities.CreateValueFunction(new Float(matrix.Row2.Z));
            op.Inputs[12].Func = Utilities.CreateValueFunction(new Float(matrix.Row2.W));

            op.Inputs[13].Func = Utilities.CreateValueFunction(new Float(matrix.Row3.X));
            op.Inputs[14].Func = Utilities.CreateValueFunction(new Float(matrix.Row3.Y));
            op.Inputs[15].Func = Utilities.CreateValueFunction(new Float(matrix.Row3.Z));
            op.Inputs[16].Func = Utilities.CreateValueFunction(new Float(matrix.Row3.W));
        }

        private void SetupValuesLight(Operator op, Light light)
        {
            op.Inputs[1].Func = Utilities.CreateValueFunction(new Float(light.Position.X));
            op.Inputs[2].Func = Utilities.CreateValueFunction(new Float(light.Position.Y));
            op.Inputs[3].Func = Utilities.CreateValueFunction(new Float(light.Position.Z));

            op.Inputs[8].Func = Utilities.CreateValueFunction(new Float(light.Color.X));
            op.Inputs[9].Func = Utilities.CreateValueFunction(new Float(light.Color.Y));
            op.Inputs[10].Func = Utilities.CreateValueFunction(new Float(light.Color.Z));

            op.Inputs[16].Func = Utilities.CreateValueFunction(new Float((float)light.Intensity/100));
            op.Inputs[17].Func = Utilities.CreateValueFunction(new Float((float)light.Intensity/100));
            op.Inputs[18].Func = Utilities.CreateValueFunction(new Float((float)light.Intensity/100));
        }

        private void SetupValuesCamera(Operator op, Camera camera)
        {
            op.Inputs[1].Func = Utilities.CreateValueFunction(new Float(camera.Position.X));
            op.Inputs[2].Func = Utilities.CreateValueFunction(new Float(camera.Position.Y));
            op.Inputs[3].Func = Utilities.CreateValueFunction(new Float(camera.Position.Z));

            op.Inputs[4].Func = Utilities.CreateValueFunction(new Float(camera.InterestPosition.X));
            op.Inputs[5].Func = Utilities.CreateValueFunction(new Float(camera.InterestPosition.Y));
            op.Inputs[6].Func = Utilities.CreateValueFunction(new Float(camera.InterestPosition.Z));

            op.Inputs[7].Func = Utilities.CreateValueFunction(new Float(camera.Up.X));
            op.Inputs[8].Func = Utilities.CreateValueFunction(new Float(camera.Up.Y));
            op.Inputs[9].Func = Utilities.CreateValueFunction(new Float(camera.Up.Z));
        }

        private void SetupValuesMesh(Operator op, Mesh mesh)
        {
            op.Inputs[0].Func = Utilities.CreateValueFunction(new Text(mesh.FileName));
            op.Inputs[1].Func = Utilities.CreateValueFunction(new Float(mesh.Index));
            op.Inputs[3].Func = Utilities.CreateValueFunction(new Float(1));
        }

        private void SetupValuesMaterial(Operator op, Material material)
        {
            op.Inputs[1].Func = Utilities.CreateValueFunction(new Float(material.Ambient.X));
            op.Inputs[2].Func = Utilities.CreateValueFunction(new Float(material.Ambient.Y));
            op.Inputs[3].Func = Utilities.CreateValueFunction(new Float(material.Ambient.Z));
            //op.Inputs[4].Func = Utilities.CreateValueFunction(new Float(material.Alpha));

            op.Inputs[5].Func = Utilities.CreateValueFunction(new Float(material.Diffuse.X));
            op.Inputs[6].Func = Utilities.CreateValueFunction(new Float(material.Diffuse.Y));
            op.Inputs[7].Func = Utilities.CreateValueFunction(new Float(material.Diffuse.Z));
            //op.Inputs[8].Func = Utilities.CreateValueFunction(new Float(material.Alpha));

            op.Inputs[9].Func = Utilities.CreateValueFunction(new Float(material.Specular.X));
            op.Inputs[10].Func = Utilities.CreateValueFunction(new Float(material.Specular.Y));
            op.Inputs[11].Func = Utilities.CreateValueFunction(new Float(material.Specular.Z));
            //op.Inputs[12].Func = Utilities.CreateValueFunction(new Float(material.Alpha));

            op.Inputs[13].Func = Utilities.CreateValueFunction(new Float(material.Emissive.X));
            op.Inputs[14].Func = Utilities.CreateValueFunction(new Float(material.Emissive.Y));
            op.Inputs[15].Func = Utilities.CreateValueFunction(new Float(material.Emissive.Z));
            //op.Inputs[16].Func = Utilities.CreateValueFunction(new Float(material.Alpha));

            //op.Inputs[17].Func = Utilities.CreateValueFunction(new Float(material.Shininess));
        }
    }
}