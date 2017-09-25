﻿using System;
using System.Collections.Generic;
using System.Data;
using Ninject;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.SurveySolutions;

namespace WB.Infrastructure.Native.Storage.Postgre.Implementation
{
    internal class PostgresReadSideKeyValueStorage<TEntity> : PostgresKeyValueStorageWithCache<TEntity>,
        IReadSideKeyValueStorage<TEntity>, IReadSideRepositoryCleaner, IDisposable
        where TEntity : class, IReadSideRepositoryEntity
    {
        private readonly ISessionProvider sessionProvider;

        public PostgresReadSideKeyValueStorage([Named(PostgresReadSideModule.SessionProviderName)]ISessionProvider sessionProvider, 
            PostgreConnectionSettings connectionSettings, ILogger logger, IEntitySerializer<TEntity> entitySerializer) 
            : base(connectionSettings.ConnectionString, connectionSettings.SchemaName, logger, entitySerializer)
        {
            this.sessionProvider = sessionProvider;

            this.EnshureTableExists();
        }

        protected override object ExecuteScalar(IDbCommand command)
        {
            this.EnlistInTransaction(command);
            return command.ExecuteScalar();
        }

        protected override int ExecuteNonQuery(IDbCommand command)
        {
            this.EnlistInTransaction(command);
            return command.ExecuteNonQuery();
        }

        private void EnlistInTransaction(IDbCommand command)
        {
            var session = this.sessionProvider.GetSession();
            command.Connection = session.Connection;
            session.Transaction.Enlist(command);
        }

        public IEnumerable<string> GetIdsStartWith(string beginingOfId)
        {
            throw new NotImplementedException();
        }
    }
}