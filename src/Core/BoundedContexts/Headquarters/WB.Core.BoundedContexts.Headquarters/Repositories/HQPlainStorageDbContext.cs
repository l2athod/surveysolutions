﻿using System.Data.Entity;
using WB.Core.BoundedContexts.Headquarters.Views.Device;

namespace WB.Core.BoundedContexts.Headquarters.Repositories
{
    public class HQPlainStorageDbContext : DbContext
    {
        public static string ConnectionStringName { get; set; }
        public DbSet<DeviceSyncInfo> DeviceSyncInfo { get; set; }
        public DbSet<SyncStatistics> SyncStatistics { get; set; }
        public DbSet<DeviceException> DeviceException { get; set; }

        public HQPlainStorageDbContext() : base(ConnectionStringName)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<HQPlainStorageDbContext, DbConfiguration>());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DeviceSyncInfo>().ToTable("devicesyncinfo", DbConfiguration.SchemaName);
            modelBuilder.Entity<SyncStatistics>().ToTable("devicesyncstatistics", DbConfiguration.SchemaName);
            modelBuilder.Entity<DeviceException>().ToTable("deviceexceptions", DbConfiguration.SchemaName);
        }
    }
}
