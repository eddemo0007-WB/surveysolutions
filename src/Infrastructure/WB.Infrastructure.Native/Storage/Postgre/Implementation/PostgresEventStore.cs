﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Threading;
using Ncqrs.Eventing;
using Ncqrs.Eventing.Storage;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using IEvent = WB.Core.Infrastructure.EventBus.IEvent;

namespace WB.Infrastructure.Native.Storage.Postgre.Implementation
{
    public class PostgresEventStore : IStreamableEventStore
    {
        private const string MissingTableErrorCode = "42P01";
        private readonly PostgreConnectionSettings connectionSettings;
        private static long lastUsedGlobalSequence = -1;
        private static readonly object lockObject = new object();
        private readonly IEventTypeResolver eventTypeResolver;
        private static int BatchSize = 4096;
        
        public PostgresEventStore(PostgreConnectionSettings connectionSettings, 
            IEventTypeResolver eventTypeResolver)
        {
            this.connectionSettings = connectionSettings;
            this.eventTypeResolver = eventTypeResolver;
        }

        public CommittedEventStream ReadFrom(Guid id, int minVersion, int maxVersion)
        {
            if (minVersion > maxVersion)
            {
                return new CommittedEventStream(id);
            }

            var streamEvents = new List<CommittedEvent>();
            
            using (var connection = new NpgsqlConnection(this.connectionSettings.ConnectionString))
            {
                connection.Open();
                using (connection.BeginTransaction())
                {
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT * FROM events WHERE eventsourceid=:sourceId AND eventsequence BETWEEN {minVersion} AND {maxVersion} ORDER BY eventsequence";
                    command.Parameters.AddWithValue("sourceId", NpgsqlDbType.Uuid, id);

                    using (IDataReader npgsqlDataReader = command.ExecuteReader())
                    {
                        while (npgsqlDataReader.Read())
                        {
                            var commitedEvent = this.ReadSingleEvent(npgsqlDataReader);

                            streamEvents.Add(commitedEvent);
                        }
                    }
                }
            }

            return new CommittedEventStream(id, streamEvents);
        }

        public CommittedEventStream Store(UncommittedEventStream eventStream)
        {
            if (eventStream.IsNotEmpty)
            {
                using (var connection = new NpgsqlConnection(this.connectionSettings.ConnectionString))
                {
                    connection.Open();
                    try
                    {
                        return new CommittedEventStream(eventStream.SourceId, this.Store(eventStream, connection));
                    }
                    catch (NpgsqlException npgsqlException)
                    {
                        connection.Open();
                        if (npgsqlException.Code == MissingTableErrorCode)
                        {
                            this.CreateRelations(connection);
                            return new CommittedEventStream(eventStream.SourceId, this.Store(eventStream, connection));
                        }
                    }
                }
            }

            return new CommittedEventStream(eventStream.SourceId);
        }

        private void CreateRelations(IDbConnection connection)
        {
            var assembly = Assembly.GetAssembly(typeof(PostgresEventStore));
            var resourceName = typeof(PostgresEventStore).Namespace + ".InitEventStore.sql";
            
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                var dbCommand = connection.CreateCommand();
                dbCommand.CommandText = reader.ReadToEnd();
                dbCommand.ExecuteNonQuery();
            }
        }

        private List<CommittedEvent> Store(UncommittedEventStream eventStream, NpgsqlConnection connection)
        {
            var result = new List<CommittedEvent>();
            using (var npgsqlTransaction = connection.BeginTransaction())
            {
                var copyFromCommand = "COPY events(id, origin, timestamp, eventsourceid, globalsequence, value, eventsequence, eventtype) FROM STDIN BINARY;";
                using (var writer = connection.BeginBinaryImport(copyFromCommand))
                {
                    foreach (var @event in eventStream)
                    {
                        var eventString = JsonConvert.SerializeObject(@event.Payload, Formatting.Indented,
                            EventSerializerSettings.BackwardCompatibleJsonSerializerSettings);
                        var nextSequnce = this.GetNextSequnce();

                        writer.StartRow();
                        writer.Write(@event.EventIdentifier, NpgsqlDbType.Uuid);
                        writer.Write(@event.Origin, NpgsqlDbType.Text);
                        writer.Write(@event.EventTimeStamp, NpgsqlDbType.Timestamp);
                        writer.Write(@event.EventSourceId, NpgsqlDbType.Uuid);
                        writer.Write(nextSequnce, NpgsqlDbType.Integer);
                        writer.Write(eventString, NpgsqlDbType.Json);
                        writer.Write(@event.EventSequence, NpgsqlDbType.Integer);
                        writer.Write(@event.Payload.GetType().Name, NpgsqlDbType.Text);

                        var committedEvent = new CommittedEvent(eventStream.CommitId,
                            @event.Origin,
                            @event.EventIdentifier,
                            @event.EventSourceId,
                            @event.EventSequence,
                            @event.EventTimeStamp,
                            nextSequnce,
                            @event.Payload);
                        result.Add(committedEvent);
                    }
                }

                npgsqlTransaction.Commit();
            }
            return result;
        }

        public int CountOfAllEvents()
        {
            using (var connection = new NpgsqlConnection(this.connectionSettings.ConnectionString))
            {
                try
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM events";

                    var scalar = command.ExecuteScalar();
                    return scalar is DBNull ? 0 : Convert.ToInt32(scalar);
                }
                catch (NpgsqlException npgsqlException)
                {
                    if (npgsqlException.Code == MissingTableErrorCode)
                    {
                        this.CreateRelations(connection);
                        return this.CountOfAllEvents();
                    }
                    throw;
                }
            }
        }

        public IEnumerable<CommittedEvent> GetAllEvents()
        {
            var countOfAllEvents = this.CountOfAllEvents();
            int processed = 0;

            while (processed < countOfAllEvents)
            {
                foreach (var committedEvent in this.ReadEventsBatch(processed))
                {
                    processed++;
                    yield return committedEvent;
                }
            }
        }

        private IEnumerable<CommittedEvent> ReadEventsBatch(int processed)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(this.connectionSettings.ConnectionString))
            {
                conn.Open();

                var npgsqlCommand = conn.CreateCommand();
                npgsqlCommand.CommandText = $"SELECT * FROM events ORDER BY globalsequence LIMIT {BatchSize} OFFSET {processed}";

                using (var reader = npgsqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return this.ReadSingleEvent(reader);
                    }
                }
            }
        }

        public IEnumerable<EventSlice> GetEventsAfterPosition(EventPosition? position)
        {
            using (var connection = new NpgsqlConnection(this.connectionSettings.ConnectionString))
            {
                connection.Open();

                int globalSequence = 0;
                if (position.HasValue)
                {
                    var lastGlobalSequenceCommand = connection.CreateCommand();
                    lastGlobalSequenceCommand.CommandText = "SELECT globalsequence FROM events WHERE eventsourceid=:eventSourceId AND eventsequence = :sequence";
                    lastGlobalSequenceCommand.Parameters.AddWithValue("eventSourceId", position.Value.EventSourceIdOfLastEvent);
                    lastGlobalSequenceCommand.Parameters.AddWithValue("sequence", position.Value.SequenceOfLastEvent);
                    globalSequence = (int)lastGlobalSequenceCommand.ExecuteScalar();
                }

                long eventsCountAfterPosition = this.GetEventsCountAfterPosition(position);
                long processed = 0;
                while (processed < eventsCountAfterPosition)
                {
                    var npgsqlCommand = connection.CreateCommand();
                    npgsqlCommand.CommandText = $"SELECT * FROM events WHERE globalsequence > {globalSequence} ORDER BY globalsequence LIMIT {BatchSize} OFFSET {processed}";

                    List<CommittedEvent> events = new List<CommittedEvent>();

                    using (var reader = npgsqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            events.Add(this.ReadSingleEvent(reader));
                        }
                    }

                    yield return new EventSlice(events, new EventPosition(), false);

                    processed += BatchSize;
                }

            }
        }

        public long GetEventsCountAfterPosition(EventPosition? position)
        {
            var totalCountOfEvents = this.CountOfAllEvents();
            if (!position.HasValue)
                return totalCountOfEvents;

            using (var connection = new NpgsqlConnection(this.connectionSettings.ConnectionString))
            {
                connection.Open();

                var npgsqlCommand = connection.CreateCommand();
                npgsqlCommand.CommandText = "SELECT globalsequence FROM events WHERE eventsourceid=:eventSourceId AND eventsequence = :sequence";
                npgsqlCommand.Parameters.AddWithValue("eventSourceId", position.Value.EventSourceIdOfLastEvent);
                npgsqlCommand.Parameters.AddWithValue("sequence", position.Value.SequenceOfLastEvent);

                int globalSequence = (int) npgsqlCommand.ExecuteScalar();

                NpgsqlCommand countCommand = connection.CreateCommand();
                countCommand.CommandText = $"SELECT COUNT(*) FROM events WHERE globalsequence > {globalSequence}";

                return (long) countCommand.ExecuteScalar();
            }
        }

        private CommittedEvent ReadSingleEvent(IDataReader npgsqlDataReader)
        {
            string value = (string) npgsqlDataReader["value"];

            string eventType = (string) npgsqlDataReader["eventtype"];
            var resolvedEventType = this.eventTypeResolver.ResolveType(eventType);
            IEvent typedEvent = JsonConvert.DeserializeObject(value, resolvedEventType, EventSerializerSettings.BackwardCompatibleJsonSerializerSettings) as IEvent;

            var origin = npgsqlDataReader["origin"];

            var eventIdentifier = (Guid) npgsqlDataReader["id"];
            var eventSourceId = (Guid) npgsqlDataReader["eventsourceid"];
            var eventSequence = (int) npgsqlDataReader["eventsequence"];
            var eventTimeStamp = (DateTime) npgsqlDataReader["timestamp"];
            var globalSequence = (int) npgsqlDataReader["globalsequence"];

            var commitedEvent = new CommittedEvent(Guid.Empty,
                origin is DBNull ? null : (string) origin,
                eventIdentifier,
                eventSourceId,
                eventSequence,
                eventTimeStamp,
                globalSequence,
                typedEvent
                );
            return commitedEvent;
        }

        private long GetNextSequnce()
        {
            if (lastUsedGlobalSequence == -1)
            {
                lock (lockObject)
                {
                    if (lastUsedGlobalSequence == -1)
                    {
                        this.FillLastUsedSequenceInEventStore();
                    }
                }
            }

            Interlocked.Increment(ref lastUsedGlobalSequence);
            return lastUsedGlobalSequence;
        }

        private void FillLastUsedSequenceInEventStore()
        {
            using (var connection = new NpgsqlConnection(this.connectionSettings.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "select MAX(globalsequence) from events";

                var scalar = command.ExecuteScalar();
                lastUsedGlobalSequence = scalar is DBNull ? 0 : Convert.ToInt32(scalar);
            }
        }
    }
}