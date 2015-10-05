﻿using System;
using System.Linq;
using Ncqrs.Domain;

namespace Ncqrs.Eventing.Sourcing.Mapping
{
    public abstract class MappedAggregateRoot : AggregateRoot
    {
        [NonSerialized] 
        private readonly IEventHandlerMappingStrategy _mappingStrategy;

        protected MappedAggregateRoot(IEventHandlerMappingStrategy strategy)
        {
            _mappingStrategy = strategy;
            InitializeHandlers();
        }

        protected MappedAggregateRoot(Guid id, IEventHandlerMappingStrategy strategy) : base(id)
        {
            _mappingStrategy = strategy;
            InitializeHandlers();
        }

        protected void InitializeHandlers()
        {
            foreach (var handler in _mappingStrategy.GetEventHandlers(this))
                RegisterHandler(handler);
        }

        public bool CanApplyHistory(CommittedEventStream history)
        {
            return history.All(committedEvent => _mappingStrategy.CanHandleEvent(this, committedEvent.Payload.GetType()));
        }
    }
}