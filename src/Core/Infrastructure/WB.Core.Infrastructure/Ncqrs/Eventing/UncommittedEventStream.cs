﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Ncqrs.Eventing
{
    /// <summary>
    /// Represents a stream of events which has not been persisted yet. They are to be persisted atomicaly as a single commit with
    /// given <see cref="CommitId"/> ID.
    /// </summary>
    public class UncommittedEventStream : IEnumerable<UncommittedEvent>
    {
        private readonly Guid _commitId;
        private readonly string _origin;
        private Guid? _singleSource;
        private bool _hasSingleSource = true;
        private readonly List<UncommittedEvent> _events = new List<UncommittedEvent>();
        private readonly Dictionary<Guid, EventSourceInformation> _eventSourceInformation = new Dictionary<Guid, EventSourceInformation>();

        public UncommittedEventStream(string origin)
            : this(Guid.NewGuid(), origin) { }

        public UncommittedEventStream(string origin, IEnumerable<UncommittedEvent> events)
            : this(Guid.NewGuid(), origin)
        {
            this.Append(events);
        }

        public UncommittedEventStream(Guid commitId, string origin)
        {
            _commitId = commitId;
            _origin = origin;
        }

        public void Append(IEnumerable<UncommittedEvent> events)
        {
            foreach (var @event in events)
            {
                this.Append(@event);
            }
        }

        public void Append(UncommittedEvent evnt)
        {
            if (_events.Count > 0 && _hasSingleSource)
            {
                if (_events[0].EventSourceId != evnt.EventSourceId)
                {
                    _hasSingleSource = false;
                }                
            }
            else if (_events.Count == 0)
            {
                _singleSource = evnt.EventSourceId;
            }
            
            _events.Add(evnt);
            evnt.OnAppendedToStream(_commitId, _origin);
            UpdateEventSourceInformation(evnt);
        }

        private void UpdateEventSourceInformation(UncommittedEvent evnt)
        {
            var newInformation = new EventSourceInformation(evnt.EventSourceId, evnt.InitialVersionOfEventSource, evnt.EventSequence);
            _eventSourceInformation[evnt.EventSourceId] = newInformation;
        }

        public IEnumerable<EventSourceInformation> Sources => this._eventSourceInformation.Values;

        /// <summary>
        /// Returns whether this stream of events has a single source. An empty stream has single source by definition.
        /// </summary>
        public bool HasSingleSource => _hasSingleSource;

        /// <summary>
        /// If the stream has a single source, it returns this source.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the stream has multiple sources.</exception>
        public Guid SourceId
        {
            get
            {
                if (!HasSingleSource)
                {
                    throw new InvalidOperationException("Event stream must have a single source in order to retrieve its source.");
                }
                return _singleSource.Value;
            }
        }

        public int InitialVersion
        {
            get
            {
                if (!HasSingleSource)
                {
                    throw new InvalidOperationException("Event stream must have a single source in order to retrieve its source initial version.");
                }
                return _eventSourceInformation[SourceId].InitialVersion;
            }
        }

        public int CurrentVersion 
        {
            get
            {
                if (!HasSingleSource)
                {
                    throw new InvalidOperationException("Event stream must have a single source in order to retrieve its source current version.");
                }
                return _eventSourceInformation[SourceId].InitialVersion;
            }
        }

        /// <summary>
        /// Returns if the stream contains at least one event.
        /// </summary>
        public bool IsNotEmpty => _events.Count > 0;

        /// <summary>
        /// Returns unique id of commit associated with this stream.
        /// </summary>
        public Guid CommitId => _commitId;

        public string Origin => this._origin;

        public IEnumerator<UncommittedEvent> GetEnumerator() => _events.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
