// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace Framefield.Core.Rendering
{

    public abstract class FXSourceCodeFunction : OperatorPart.Function, IFXSourceCode
    {
        public override void Dispose()
        {
            Utilities.DisposeObj(ref _effect);
        }

        public virtual string GetCode(int idx)
        {
            if (idx < OperatorPart.Connections.Count)
            {
                var context = new OperatorPartContext() { Time = 0.0f };
                return OperatorPart.Connections[idx].Eval(context).Text;
            }
            else
                return "";
        }

        public virtual void SetCode(int idx, string code)
        {
            if (idx < OperatorPart.Connections.Count)
            {
                var input = OperatorPartUtilities.FindLowestUnconnectedOpPart(OperatorPart.Connections[idx], 5);
                input.SetValue(new Text(code));
                var setValueAsDefaultCmd = new Commands.SetInputAsAndResetToDefaultCommand(input);
                setValueAsDefaultCmd.Do();
            }
        }

        public virtual int NumCodes()
        {
            return 1;
        }

        public virtual CompilerErrorCollection Compile(int codeIdx)
        {
            Utilities.DisposeObj(ref _effect);
            var errors = new CompilerErrorCollection();
            try
            {
                using (var compilationResult = ShaderBytecode.Compile(GetCode(codeIdx), "fx_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, null))
                {
                    _effect = new Effect(D3DDevice.Device, compilationResult);
                    if (compilationResult.Message != null)
                    {
                        Logger.Warn("HLSL compile warning in '{0}':\n{1}", OperatorPart?.Name, compilationResult.Message);
                    }
                }
            }
            catch (SharpDX.CompilationException ex)
            {
                errors = ErrorsFromString(ex.Message);
                Logger.Error("FX compile error:\n{0}", ex.Message);
            }
            return errors;
        }

        /* convert shader-errors that come in the format of:
         * c:\self.demos\tooll2\n\a(58,12): error X3004: undeclared identifier 'col2'
         * c:\self.demos\tooll2\n\a(58,5): error X3080: 'PS': function must return a value
         **/
        protected CompilerErrorCollection ErrorsFromString(string errorString)
        {
            var errors = new CompilerErrorCollection();
            var errorLinePattern = new Regex(@"\((\d+),(\d+)\): error\s*(\w+):\s*(.*?)\s*$");

            foreach (var line in errorString.Split('\n'))
            {
                var matches = errorLinePattern.Matches(line);
                if (matches.Count == 1)
                {
                    var lineNumber = int.Parse(matches[0].Groups[1].Value);
                    var column = int.Parse(matches[0].Groups[2].Value);
                    string errorCode = matches[0].Groups[3].Value;
                    string errorMessage = matches[0].Groups[4].Value;

                    errors.Add(new CompilerError() { Column = column, ErrorNumber = errorCode, Line = lineNumber, ErrorText = errorMessage });
                }
            }

            return errors;
        }

        protected EffectVariable GetVariableByName(String variableName)
        {
            var x = _effect.GetVariableByName(variableName);
            if (x == null)
            {
                Logger.Error(this, "EffectVariable {0} not found", variableName);
            }
            return x;
        }

        protected void SetScalar(String variableName, float value)
        {
            var variable = _effect.GetVariableByName(variableName);
            if (variable == null)
            {
                Logger.Error(this, "Can't set undefined ShaderEffectVariable '{0}' to {1}", variableName, value);
                return;
            }
            var scalarVariable = variable.AsScalar();
            if (scalarVariable == null)
            {
                Logger.Error(this, "Can't set ShaderEffectVariable '{0}' to  scalar {1}", variableName, value);
                return;

            }
            scalarVariable.Set(value);
        }

        protected void SetVector<T>(string variableName, T value) where T : struct
        {
            var variable = _effect.GetVariableByName(variableName);
            if (variable == null)
            {
                Logger.Error(this, "Can't set undefined ShaderEffectVariable '{0}' to {1}", variableName, value);
                return;
            }
            var vector = variable.AsVector();
            if (vector == null)
            {
                Logger.Error(this, "Can't set ShaderEffectVariable '{0}' to Vector4 {1}", variableName, value);
                return;
            }
            vector.Set(value);
        }

        protected void SetColor(string variableName, Color4 color)
        {
            SetVector(variableName, color);
        }

        protected void SetVector2(string variableName, Vector2 value)
        {
            SetVector(variableName, value);
        }

        protected void SetVector4(string variableName, Vector4 value)
        {
            SetVector(variableName, value);
        }

        protected void SetMatrix(String variableName, Matrix matrixValue)
        {
            var variable = _effect.GetVariableByName(variableName);
            if (variable == null)
            {
                Logger.Error(this, "Can't set undefined ShaderEffectVariable '{0}' to Matrix {1}", variableName, matrixValue);
                return;
            }
            var matrixVariable = variable.AsMatrix();
            if (matrixVariable == null)
            {
                Logger.Error(this, "Can't set ShaderEffectVariable '{0}' to Matrix {1}", variableName, matrixValue);
                return;
            }
            matrixVariable.SetMatrix(matrixValue);
        }

        protected Effect _effect = null;
    }


}
