﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Ncqrs.Domain.Storage;
using Ncqrs.Eventing;
using Ncqrs.Eventing.Sourcing.Snapshotting;
using Ncqrs.Eventing.Storage;
using WB.Core.Infrastructure.Aggregates;

namespace WB.Core.Infrastructure.Implementation.Aggregates
{
    internal class AggregateRootRepositoryWithCache : AggregateRootRepository
    {
        static readonly ConcurrentDictionary<Type, IAggregateRoot> memoryCache = new ConcurrentDictionary<Type, IAggregateRoot>();

        public AggregateRootRepositoryWithCache(IEventStore eventStore, ISnapshotStore snapshotStore,
            IDomainRepository repository)
            : base(eventStore, snapshotStore, repository){}


        public override IAggregateRoot GetLatest(Type aggregateType, Guid aggregateId)
        {
            IAggregateRoot aggregateRoot;

            if (memoryCache.TryGetValue(aggregateType, out aggregateRoot)
                && aggregateRoot.EventSourceId == aggregateId)
                return aggregateRoot;

            aggregateRoot = base.GetLatest(aggregateType, aggregateId);

            if (aggregateRoot != null)
            {
                memoryCache[aggregateType] = aggregateRoot;
            }

            return aggregateRoot;
        }
    }
}