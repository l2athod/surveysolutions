﻿using System;
using System.Runtime.Serialization;

namespace Ncqrs.Domain
{
    public class EventNotHandledException : Exception
    {
        public object Event
        {
            get;
            private set;
        }

        public EventNotHandledException(object evnt)
            : this(evnt, evnt != null ? String.Format("No handler handled the {0} event.", evnt.GetType().FullName) : null)
        {

        }

        public EventNotHandledException(object evnt, String message)
            : this(evnt, message, null)
        {
        }

        public EventNotHandledException(object evnt, String message, Exception inner)
            : base(message, inner)
        {
            Event = evnt;
        }
    }
}
