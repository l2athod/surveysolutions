﻿using System;
using NodaTime;

namespace WB.Core.BoundedContexts.Headquarters.Assignments
{
    public class CalendarEventView
    {
        public DateTime StartUtc { get; set; }
        public string StartTimezone { get; set; }
        public ZonedDateTime Start{ get; set; }
        public string Comment { get; set; }
        public Guid PublicKey { get; set; }
    }
}
