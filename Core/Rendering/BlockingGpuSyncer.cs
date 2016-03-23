// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using SharpDX.Direct3D11;

namespace Framefield.Core.Rendering
{
    public class BlockingGpuSyncer : IDisposable
    {
        public BlockingGpuSyncer(Device dxDevice)
        {
            var queryDesc = new QueryDescription
                                {
                                    Type = QueryType.Event,
                                    Flags = QueryFlags.None
                                };
            _waitForGpuQuery = new Query(dxDevice, queryDesc);
        }

        public void Dispose()
        {
            _waitForGpuQuery.Dispose();
        }

        public void Sync(DeviceContext dxContext)
        {
            dxContext.End(_waitForGpuQuery);
            int queryResult;
            while (!dxContext.GetData(_waitForGpuQuery, out queryResult))
            {
                // do nothing, simply wait blocking for gpu to finish
            }
        }

        private readonly Query _waitForGpuQuery;
    }
}
