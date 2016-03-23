// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.ComponentModel;
using System.Threading;

namespace Framefield.Core
{
    public static class EventExt
    {
        public static void Raise<TEventArgs>(EventHandler<TEventArgs> eventDelegate, Object sender, TEventArgs args) where TEventArgs : EventArgs
        {
            if (eventDelegate!= null)
                eventDelegate(sender, args);
        }

        public static void RaiseThreadsafe<TEventArgs>(ref EventHandler<TEventArgs> eventDelegate, Object sender, TEventArgs args) where TEventArgs : EventArgs
        {
            // Copy a reference to the delegate field now into a temporary field for thread safety
            var temp = Volatile.Read(ref eventDelegate);
            if (temp != null)
                temp(sender, args);
        }

        public static void Raise(PropertyChangedEventHandler eventDelegate, Object sender, PropertyChangedEventArgs args)
        {
            if (eventDelegate != null)
                eventDelegate(sender, args);
        }

    }
}
