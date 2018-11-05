﻿using System;
using System.ComponentModel;
using NHibernate;
using Ninject;
using Npgsql;
using StackExchange.Exceptional;
using WB.Core.BoundedContexts.Headquarters.Implementation.Synchronization;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Infrastructure.Native.Monitoring;
using WB.Infrastructure.Native.Storage.Postgre;
using WB.UI.Shared.Web.Extensions;

namespace WB.UI.Headquarters.Services
{
    public class MetricsService
    {
        public static bool IsEnabled => System.Configuration.ConfigurationManager.AppSettings.GetBool(@"Metrics.Enable", true);

        [Localizable(false)]
        public static void Start(IServiceLocator serviceLocator)
        {
            var logger = serviceLocator.GetInstance<ILoggerProvider>().GetFor<MetricsService>();

            if (!IsEnabled)
            {
                logger.Info("Metrics are disabled per configuration");
                return;
            }
            try
            {
                var sessionFactory = serviceLocator.GetInstance<ISessionFactory>(PostgresPlainStorageModule.SessionFactoryName);
                MetricsRegistry.Instance.RegisterOnDemandCollectors(new BrokenPackagesStatsCollector(sessionFactory));

                // getting instance name from connection string information
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["Postgres"];
                var npgsConnectionString = new NpgsqlConnectionStringBuilder(connectionString.ConnectionString);
                var instanceName = npgsConnectionString.ApplicationName;

                // configuring address for metrics pushgateway
                var metricsGateway = System.Configuration.ConfigurationManager.AppSettings["Metrics.Gateway"];
                if (string.IsNullOrEmpty(metricsGateway))
                    return;

                // initialize push mechanizm
                new Prometheus.MetricPusher(metricsGateway, job: "hq",
                    additionalLabels: new[] { Tuple.Create("site", instanceName) },
                    intervalMilliseconds: 5000).Start();
            }
            catch (Exception e)
            {
                e.Log(null);
                logger.Error("Unable to start metrics push", e);
            }
        }
    }
}