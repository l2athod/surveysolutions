﻿using System.Collections.Generic;

namespace WB.Core.BoundedContexts.Headquarters.ValueObjects.PreloadedData
{
    public class VerificationStatus
    {
        public VerificationStatus()
        {
            this.WasResponsibleProvided = false;
        }

        public IEnumerable<PreloadedDataVerificationError> Errors { set; get; }

        public bool WasResponsibleProvided { set; get; }
    }
}
