// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D11;

namespace Framefield.Core.Profiling
{
    public class GPUQuery : IDisposable
    {
        public GPUQuery(Device device, QueryDescription desc)
        {
            _query = new Query(device, desc);
            _inBetweenQuery = false;
        }

        public void Dispose()
        {
            _inBetweenQuery = false;
            Utilities.DisposeObj(ref _query);
        }

        public void Begin(DeviceContext context)
        {
            if (_inBetweenQuery)
                return;

            _inBetweenQuery = true;
            context.Begin(_query);
        }

        public void End(DeviceContext context)
        {
            context.End(_query);
            _inBetweenQuery = false;
        }

        public bool GetData<T>(DeviceContext context, AsynchronousFlags flags, out T result) where T : struct
        {
            if (!_inBetweenQuery)
            {
                return context.GetData(_query, flags, out result);
            }
            else
            {
                result = new T();
                return false;
            }
        }

        Query _query;
        bool _inBetweenQuery;
    }

}
