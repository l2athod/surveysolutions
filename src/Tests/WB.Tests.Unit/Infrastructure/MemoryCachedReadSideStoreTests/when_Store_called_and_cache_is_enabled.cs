﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Moq;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.Infrastructure.Storage.Memory.Implementation;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.Infrastructure.MemoryCachedReadSideStoreTests
{
    internal class when_Store_called_and_cache_is_enabled : MemoryCachedReadSideStoreTestContext
    {
        Establish context = () =>
        {
            readSideStorageMock = new Mock<IReadSideStorage<ReadSideRepositoryEntity>>();
            memoryCachedReadSideStore = CreateMemoryCachedReadSideStore(readSideStorageMock.Object);
            memoryCachedReadSideStore.EnableCache();
        };
        Because of = () =>
            memoryCachedReadSideStore.Store(view, id);

        It should_never_call_Store_of_IReadSideStorage = () =>
            readSideStorageMock.Verify(x => x.Store(view, id), Times.Never);

        It should_store_view_in_cache = () =>
           memoryCachedReadSideStore.GetById(id).ShouldEqual(view);

        private static MemoryCachedReadSideStore<ReadSideRepositoryEntity> memoryCachedReadSideStore;
        private static Mock<IReadSideStorage<ReadSideRepositoryEntity>> readSideStorageMock;
        private static string id = "id_view";
        private static ReadSideRepositoryEntity view = new ReadSideRepositoryEntity();
    }
}
