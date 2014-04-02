﻿using System;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;

namespace WB.UI.Headquarters.Models
{
    public class InterviewModel
    {
        public Guid InterviewId { get; set; }
        public InterviewStatus InterviewStatus { get; set; }
        public Guid? CurrentGroupId { get; set; }
        public Guid? CurrentPropagationKeyId { get; set; }
    }
}