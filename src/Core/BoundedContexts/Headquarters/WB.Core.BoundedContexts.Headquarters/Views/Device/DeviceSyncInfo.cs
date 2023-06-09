﻿using System;

namespace WB.Core.BoundedContexts.Headquarters.Views.Device
{   
    public class DeviceSyncInfo
    {
        public virtual int Id { get; set; }
        public virtual DateTime SyncDate { get; set; }
        public virtual Guid InterviewerId { get; set; }
        public virtual string DeviceId { get; set; }
        public virtual string DeviceModel { get; set; }
        public virtual string DeviceType { get; set; }
        public virtual DateTime DeviceDate { get; set; }
        public virtual double? DeviceLocationLat { get; set; }
        public virtual double? DeviceLocationLong { get; set; }
        public virtual string DeviceLanguage { get; set; }
        public virtual string DeviceManufacturer { get; set; }
        public virtual string DeviceBuildNumber { get; set; }
        public virtual string DeviceSerialNumber { get; set; }
        public virtual string AndroidVersion { get; set; }
        public virtual int AndroidSdkVersion { get; set; }
        public virtual string AndroidSdkVersionName { get; set; }
        public virtual string AppVersion { get; set; }
        public virtual int AppBuildVersion { get; set; }
        public virtual DateTime LastAppUpdatedDate { get; set; }
        public virtual string NetworkType { get; set; }
        public virtual string NetworkSubType { get; set; }
        public virtual string MobileOperator { get; set; }
        public virtual string AppOrientation { get; set; }
        public virtual string BatteryPowerSource { get; set; }
        public virtual int BatteryChargePercent { get; set; }
        public virtual bool IsPowerInSaveMode { get; set; }
        public virtual int MobileSignalStrength { get; set; }
        public virtual long StorageTotalInBytes { get; set; }
        public virtual long StorageFreeInBytes { get; set; }
        public virtual long RAMTotalInBytes { get; set; }
        public virtual long RAMFreeInBytes { get; set; }
        public virtual long DBSizeInfo { get; set; }
        public virtual int NumberOfStartedInterviews { get; set; }

        public virtual SyncStatistics Statistics { get; set; } 
    }

    public class SyncStatistics
    {
        public virtual int Id { get; set; }

        public virtual int UploadedInterviewsCount { get; set; }
        public virtual int DownloadedInterviewsCount { get; set; }
        public virtual int DownloadedQuestionnairesCount { get; set; }

        public virtual int RejectedInterviewsOnDeviceCount { get; set; }
        public virtual int NewInterviewsOnDeviceCount { get; set; }

        public virtual int NewAssignmentsCount { get; set; }
        public virtual int RemovedAssignmentsCount { get; set; }
        public virtual int? RemovedInterviewsCount { get; set; }
        public virtual int AssignmentsOnDeviceCount { get; set; }

        public virtual long TotalUploadedBytes { get; set; }

        public virtual long TotalDownloadedBytes { get; set; }

        public virtual double TotalConnectionSpeed { get; set; }

        public virtual TimeSpan TotalSyncDuration { get; set; }

        public virtual DateTime SyncFinishDate { get; set; }
    }
}
