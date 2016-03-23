// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.CodeDom.Compiler;

namespace Framefield.Core.Rendering
{

    public interface IFXSourceCode
    {
        string GetCode(int idx);
        void SetCode(int idx, string code);
        int NumCodes();
        CompilerErrorCollection Compile(int codeIdx);
    }

    public interface IFXImageSourceCode
    {
    }

    public interface IFXSceneSourceCode
    {
    }

}
