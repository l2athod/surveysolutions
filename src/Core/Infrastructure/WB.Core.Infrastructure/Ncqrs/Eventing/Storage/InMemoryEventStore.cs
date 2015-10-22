﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ncqrs.Eventing.Sourcing.Snapshotting;

namespace Ncqrs.Eventing.Storage
{
    /// <summary>
    /// An in memory event store that can be used for unit testing purpose. We can't
    /// think of any situation where you want to use this in production.
    /// </summary>
    public class InMemoryEventStore : IEventStore, ISnapshotStore
    {
        private readonly Dictionary<Guid, Queue<CommittedEvent>> _events = new Dictionary<Guid, Queue<CommittedEvent>>();
        private readonly Dictionary<Guid, Snapshot> _snapshots = new Dictionary<Guid, Snapshot>();
        
        /// <summary>
        /// Saves a snapshot of the specified event source.
        /// </summary>
        public void SaveShapshot(Snapshot snapshot)
        {
            _snapshots[snapshot.EventSourceId] = snapshot;
        }

        /// <summary>
        /// Gets a snapshot of a particular event source, if one exists. Otherwise, returns <c>null</c>.
        /// </summary>
        public Snapshot GetSnapshot(Guid eventSourceId, int maxVersion)
        {
            if (!_snapshots.ContainsKey(eventSourceId))
                return null;
            var result = _snapshots[eventSourceId];
            return result.Version > maxVersion 
                ? null 
                : result;
        }

        public CommittedEventStream ReadFrom(Guid id, int minVersion, int maxVersion)
        {
            Queue<CommittedEvent> events;
            
            if (_events.TryGetValue(id, out events))
            {
                var committedEvents = events
                    .Where(x => x.EventSequence >= minVersion && x.EventSequence <= maxVersion);                    
                return new CommittedEventStream(id, committedEvents);
            }
            return new CommittedEventStream(id);
        }

        public CommittedEventStream Store(UncommittedEventStream eventStream)
        {
            Queue<CommittedEvent> events;
            if (eventStream.IsNotEmpty)
            {
                List<CommittedEvent> result = new List<CommittedEvent>();
                if (!_events.TryGetValue(eventStream.SourceId, out events))
                {
                    events = new Queue<CommittedEvent>();
                    _events.Add(eventStream.SourceId, events);
                }

                foreach (var evnt in eventStream)
                {
                    var committedEvent = new CommittedEvent(eventStream.CommitId, 
                        evnt.Origin, 
                        evnt.EventIdentifier, 
                        eventStream.SourceId, 
                        evnt.EventSequence,
                        evnt.EventTimeStamp, 
                        events.Count,
                        evnt.Payload);
                    events.Enqueue(committedEvent);
                    result.Add(committedEvent);   
                }

                return new CommittedEventStream(eventStream.SourceId, result);
            }

            return new CommittedEventStream(eventStream.SourceId);
        }
    }
}
