﻿using System;
using WB.Core.SharedKernels.DataCollection.ValueObjects;

namespace WB.Core.BoundedContexts.Headquarters.DataExport.Views
{
    public class InterviewDataExportLevelView
    {
        public InterviewDataExportLevelView(ValueVector<Guid> levelVector, string levelName, InterviewDataExportRecord[] records)
        {
            this.LevelVector = levelVector;
            this.LevelName = levelName;
            this.Records = records;
        }

        public ValueVector<Guid> LevelVector { get; private set; } // used only in tests
        public string LevelName { get; private set; }
        public InterviewDataExportRecord[] Records { get; private set; }
    }
}