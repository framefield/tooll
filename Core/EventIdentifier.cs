// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Linq;
using System.Reflection;

namespace Framefield.Core
{

    public abstract class IdentifierAttribute : Attribute
    {
        public string name;
        public Guid id;

        protected IdentifierAttribute(string name, string id)
        {
            this.name = name;
            this.id = Guid.Parse(id);
        }
    }

    [AttributeUsage(AttributeTargets.Event)]
    public sealed class EventIdentifier : IdentifierAttribute
    {
        public EventIdentifier(string name, string id)
            : base(name, id)
        {
        }

        public static Tuple<EventIdentifier, EventInfo>[] GetEventsIn(Type type)
        {
            var identifiedEvents = from @event in type.GetEvents()
                                   from attribute in @event.GetCustomAttributes(true)
                                   let identifier = attribute as EventIdentifier
                                   where identifier != null
                                   select Tuple.Create(identifier, @event);

            return identifiedEvents.ToArray();
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class EventHandlerIdentifier : IdentifierAttribute
    {
        public EventHandlerIdentifier(string name, string id)
            : base(name, id)
        {
        }

        public static Tuple<EventHandlerIdentifier, MethodInfo>[] GetHandlerIn(Type type)
        {
            var identfiedHandler = from method in type.GetMethods()
                                   from attribute in method.GetCustomAttributes(true)
                                   let identifier = attribute as EventHandlerIdentifier
                                   where identifier != null
                                   select Tuple.Create(identifier, method);

            return identfiedHandler.ToArray();
        }
    }
}
