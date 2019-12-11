﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Ncqrs.Eventing;
using Ncqrs.Eventing.Storage;
using Newtonsoft.Json;
using NHibernate;
using Npgsql;
using NpgsqlTypes;
using IEvent = WB.Core.Infrastructure.EventBus.IEvent;

namespace WB.Infrastructure.Native.Storage.Postgre.Implementation
{
    [Localizable(false)]
    public class PostgresEventStore : IHeadquartersEventStore
    {
        private readonly IEventTypeResolver eventTypeResolver;

        private static int BatchSize = 4096;
        private static string tableNameWithSchema;
        private readonly string tableName;
        private readonly string[] obsoleteEvents = new[] { "tabletregistered" };

        private readonly IUnitOfWork sessionProvider;

        public PostgresEventStore(PostgreConnectionSettings connectionSettings,
            IEventTypeResolver eventTypeResolver,
            IUnitOfWork sessionProvider)
        {
            this.eventTypeResolver = eventTypeResolver;
            this.sessionProvider = sessionProvider;

            this.tableName = "events";
            tableNameWithSchema = connectionSettings.SchemaName + "." + this.tableName;
        }

        public IEnumerable<CommittedEvent> Read(Guid id, int minVersion)
        {
            int processed = 0;
            IEnumerable<CommittedEvent> batch;
            do
            {
                batch = this.ReadBatch(id, minVersion, processed).ToList();
                foreach (var @event in batch)
                {
                    processed++;
                    yield return @event;
                }
            } while (batch.Any());
        }

        public IEnumerable<CommittedEvent> Read(Guid id, int minVersion, IProgress<EventReadingProgress> progress, CancellationToken cancellationToken)
            => this.Read(id, minVersion);

        private IEnumerable<CommittedEvent> ReadBatch(Guid id, int minVersion, int processed)
        {
            var rawEvents = sessionProvider.Session.Connection.Query<RawEvent>(
                $"SELECT id, eventsourceid, origin, eventsequence, timestamp, globalsequence, eventtype, value::text " +
                $"FROM {tableNameWithSchema} " +
                $"WHERE eventsourceid= @sourceId AND eventsequence >= @minVersion " +
                $"ORDER BY eventsequence LIMIT @batchSize OFFSET @processed",
                new
                {
                    sourceId = id,
                    minVersion = minVersion,
                    batchSize = BatchSize,
                    processed = processed
                }, buffered: true);

            foreach (var committedEvent in ToCommittedEvent(rawEvents))
            {
                yield return committedEvent;
            }
        }

        public int? GetLastEventSequence(Guid id)
        {
            return this.sessionProvider.Session.Connection.ExecuteScalar<int?>(
                $"SELECT MAX(eventsequence) as eventsourceid FROM {tableNameWithSchema} WHERE eventsourceid=@sourceId", 
                new { sourceId = id });
        }

        public CommittedEventStream Store(UncommittedEventStream eventStream)
        {
            if (eventStream.IsNotEmpty)
            {
                return new CommittedEventStream(eventStream.SourceId, this.Store(eventStream, this.sessionProvider.Session));
            }

            return new CommittedEventStream(eventStream.SourceId);
        }

        private List<CommittedEvent> Store(UncommittedEventStream eventStream, ISession connection)
        {
            var result = new List<CommittedEvent>();

            ValidateStreamVersion(eventStream, connection);

            var copyFromCommand = $"COPY {tableNameWithSchema}(id, origin, timestamp, eventsourceid, value, eventsequence, eventtype) FROM STDIN BINARY;";
            var npgsqlConnection = connection.Connection as NpgsqlConnection;

            using (var writer = npgsqlConnection.BeginBinaryImport(copyFromCommand))
            {
                foreach (var @event in eventStream)
                {
                    var eventString = JsonConvert.SerializeObject(@event.Payload, Formatting.Indented,
                        EventSerializerSettings.BackwardCompatibleJsonSerializerSettings);

                    writer.StartRow();
                    writer.Write(@event.EventIdentifier, NpgsqlDbType.Uuid);
                    writer.Write(@event.Origin, NpgsqlDbType.Text);
                    writer.Write(@event.EventTimeStamp, NpgsqlDbType.Timestamp);
                    writer.Write(@event.EventSourceId, NpgsqlDbType.Uuid);
                    writer.Write(eventString, NpgsqlDbType.Jsonb);
                    writer.Write(@event.EventSequence, NpgsqlDbType.Integer);
                    writer.Write(@event.Payload.GetType().Name, NpgsqlDbType.Text);

                    var committedEvent = new CommittedEvent(eventStream.CommitId,
                        @event.Origin,
                        @event.EventIdentifier,
                        @event.EventSourceId,
                        @event.EventSequence,
                        @event.EventTimeStamp,
                        null,
                        @event.Payload);
                    result.Add(committedEvent);
                }

                writer.Complete();
            }

            return result;
        }

        private static void ValidateStreamVersion(UncommittedEventStream eventStream, ISession connection)
        {
            void AppendEventSourceParameter(IDbCommand command)
            {
                IDbDataParameter sourceIdParameter = command.CreateParameter();
                sourceIdParameter.Value = eventStream.SourceId;
                sourceIdParameter.DbType = DbType.Guid;
                sourceIdParameter.ParameterName = "sourceId";
                command.Parameters.Add(sourceIdParameter);
            }

            if (eventStream.InitialVersion == 0)
            {
                using (var validateVersionCommand = connection.Connection.CreateCommand())
                {
                    validateVersionCommand.CommandText = $"SELECT EXISTS(SELECT 1 FROM {tableNameWithSchema} WHERE eventsourceid = :sourceId)";
                    AppendEventSourceParameter(validateVersionCommand);

                    var streamExists = validateVersionCommand.ExecuteScalar() as bool?;
                    if (streamExists.GetValueOrDefault())
                        throw new InvalidOperationException(
                            $"Unexpected stream version. Expected non existant stream, but received stream with version {eventStream.InitialVersion}. EventSourceId: {eventStream.SourceId}");
                }
            }
            else
            {
                using (var validateVersionCommand = connection.Connection.CreateCommand())
                {
                    validateVersionCommand.CommandText =
                        $"SELECT MAX(eventsequence) FROM {tableNameWithSchema} WHERE eventsourceid = :sourceId";
                    AppendEventSourceParameter(validateVersionCommand);

                    var storedLastSequence = validateVersionCommand.ExecuteScalar() as int?;
                    if (storedLastSequence != eventStream.InitialVersion)
                        throw new InvalidOperationException(
                            $"Unexpected stream version. Expected {eventStream.InitialVersion}. Actual {storedLastSequence}. EventSourceId: {eventStream.SourceId}");
                }
            }
        }

        public bool HasEventsAfterSpecifiedSequenceWithAnyOfSpecifiedTypes(long sequence, Guid eventSourceId, params string[] typeNames)
        {
            var connection = sessionProvider.Session.Connection as NpgsqlConnection;
            var command = connection.CreateCommand();
            command.CommandText = $@"select 1 from {tableNameWithSchema} where
                                        eventsourceid = :eventSourceId
                                        and eventsequence > :eventSequence
                                        and eventtype = ANY(:eventTypes)
                                        limit 1";
            command.Parameters.AddWithValue("eventSourceId", NpgsqlDbType.Uuid, eventSourceId);
            command.Parameters.AddWithValue("eventSequence", NpgsqlDbType.Bigint, sequence);
            command.Parameters.AddWithValue("eventTypes", NpgsqlDbType.Array | NpgsqlDbType.Text, typeNames);
            var scalar = command.ExecuteScalar();
            return scalar != null;
        }

        public int? GetMaxEventSequenceWithAnyOfSpecifiedTypes(Guid eventSourceId, params string[] typeNames)
        {
            return this.sessionProvider.Session.Connection.ExecuteScalar<int?>(
                $@"select MAX(eventsequence) from {tableNameWithSchema} where
                                        eventsourceid = @eventSourceId
                                        and eventtype = ANY(@eventTypes)
                                        limit 1", new { eventSourceId, eventTypes = typeNames} );
        }

        public async Task<EventsFeedPage> GetEventsFeedAsync(long startWithGlobalSequence, int pageSize)
        {
            var rawEventsData = await this.sessionProvider.Session.Connection
                .QueryAsync<RawEvent>
                 ($@"SELECT id, eventsourceid, origin, eventsequence, timestamp, globalsequence, eventtype, value::text 
                   FROM {tableNameWithSchema} 
                   WHERE globalsequence > @minVersion 
                   ORDER BY globalsequence 
                   LIMIT @batchSize",
                   new { minVersion = startWithGlobalSequence, batchSize = pageSize });

            var globalSequence = await this.sessionProvider.Session.Connection.ExecuteScalarAsync<long?>("SELECT max(globalsequence) FROM events.events") ?? 0;

            var events = ToCommittedEvent(rawEventsData).ToList();

            return new EventsFeedPage(globalSequence, events);
        }

        public IEnumerable<RawEvent> GetRawEventsFeed(long startWithGlobalSequence, int pageSize)
        {
            var rawEventsData = this.sessionProvider.Session.Connection
                .Query<RawEvent>
                ($@"SELECT id, eventsourceid, origin, eventsequence, timestamp, globalsequence, eventtype, value::text 
                   FROM {tableNameWithSchema} 
                   WHERE globalsequence > @minVersion 
                   ORDER BY globalsequence 
                   LIMIT @batchSize",
                    new { minVersion = startWithGlobalSequence, batchSize = pageSize }, buffered: false);

            return rawEventsData;
        }

        public async Task<List<CommittedEvent>> GetEventsInReverseOrderAsync(Guid aggregateRootId, int offset, int limit)
        {
            List<CommittedEvent> result = new List<CommittedEvent>();
            var connection = sessionProvider.Session.Connection as NpgsqlConnection;
            var rawEvents = await connection.QueryAsync<RawEvent>(
                        $"SELECT id, eventsourceid, origin, eventsequence, timestamp, globalsequence, eventtype, value::text " +
                        $"FROM {tableNameWithSchema} " +
                        "WHERE eventsourceid = @sourceId " +
                        "ORDER BY eventsequence DESC LIMIT @limit OFFSET @offset",
                        new
                        {
                            sourceId = aggregateRootId,
                            limit = limit,
                            offset = offset
                        });

            foreach (var committedEvent in ToCommittedEvent(rawEvents))
            {
                result.Add(committedEvent);
            }

            return result;
        }

        public async Task<int> TotalEventsCountAsync(Guid aggregateRootId)
        {
            var connection = sessionProvider.Session.Connection as NpgsqlConnection;
            var result = await connection.ExecuteScalarAsync<int?>(
                    $"SELECT COUNT(id) FROM {tableNameWithSchema} WHERE eventsourceid=:sourceId",
                    new
                    {
                        sourceId = aggregateRootId
                    });

            return result.GetValueOrDefault();
        }

        public async Task<long> GetMaximumGlobalSequence()
        {
            return await this.sessionProvider.Session.Connection.ExecuteScalarAsync<long?>("SELECT max(globalsequence) FROM events.events") ?? 0;
        }

        private IEnumerable<CommittedEvent> ToCommittedEvent(IEnumerable<RawEvent> rawEventsData)
        {
            // reusing serializer to save few bytes of allocation
            var serializer = JsonSerializer.CreateDefault(EventSerializerSettings.BackwardCompatibleJsonSerializerSettings);

            IEvent Deserialize(string value, Type type)
            {
                using (var sr = new StringReader(value))
                {
                    using (JsonTextReader jsonTextReader = new JsonTextReader(sr))
                    {
                        return (IEvent)serializer.Deserialize(jsonTextReader, type);
                    }
                }
            }

            foreach (var raw in rawEventsData)
            {
                if (obsoleteEvents.Contains(raw.EventType.ToLower()))
                {
                    continue;
                }

                var resolvedEventType = this.eventTypeResolver.ResolveType(raw.EventType);
                IEvent typedEvent = Deserialize(raw.Value, resolvedEventType);

                yield return new CommittedEvent(
                    Guid.Empty,
                    raw.Origin,
                    raw.Id,
                    raw.EventSourceId,
                    raw.EventSequence,
                    raw.TimeStamp,
                    raw.GlobalSequence,
                    typedEvent
                );
            }
        }
    }
}
