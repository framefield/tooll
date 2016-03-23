// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

namespace Framefield.Core.Testing
{
    public interface ITestEvaluator
    {
        bool GetStartTestsEnabled(OperatorPartContext context);
        bool GetRebuildReferenceEnabled(OperatorPartContext context);
        string GetFilter(OperatorPartContext context);
    }
}
