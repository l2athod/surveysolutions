using System;

namespace WB.Core.SharedKernel.Structures.Synchronization.SurveyManagement
{
    public abstract class BaseSyncPackageDto
    {
        public string PackageId { get; set; }

        public string Content { get; set; }
    }
}