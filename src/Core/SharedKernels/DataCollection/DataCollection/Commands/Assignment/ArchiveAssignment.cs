﻿using System;

namespace WB.Core.SharedKernels.DataCollection.Commands.Assignment
{
    public class ArchiveAssignment : AssignmentCommand
    {
        public ArchiveAssignment(Guid assignmentId, Guid userId) : base(assignmentId, userId)
        {
        }
    }
}