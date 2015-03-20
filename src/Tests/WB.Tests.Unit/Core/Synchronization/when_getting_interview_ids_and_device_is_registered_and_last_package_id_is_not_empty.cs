using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Specifications;

using Moq;

using WB.Core.GenericSubdomains.Utils;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernel.Structures.Synchronization;
using WB.Core.Synchronization.Documents;
using WB.Core.Synchronization.Implementation.SyncManager;
using WB.Core.Synchronization.SyncStorage;

using It = Machine.Specifications.It;

namespace WB.Tests.Unit.Core.Synchronization
{
    [Ignore("Postgres")]
    internal class when_getting_interview_ids_and_device_is_registered_and_last_package_id_is_not_empty : SyncManagerTestContext
    {
        Establish context = () =>
        {
            tabletDocument = CreateTabletDocument(deviceId, androidId);
            devices = Mock.Of<IReadSideRepositoryReader<TabletDocument>>(x => x.GetById(deviceId.FormatGuid()) == tabletDocument);

            interviewSyncPackageMetas = new List<InterviewSyncPackageMeta>
                                        {
                                            CreateInterviewSyncPackageMetaInformation(interviewId, sortIndex:1, itemType: SyncItemType.Interview, userId:userId),
                                            CreateInterviewSyncPackageMetaInformation(interviewId, sortIndex:2, itemType: SyncItemType.DeleteInterview, userId:userId),
                                            CreateInterviewSyncPackageMetaInformation(interviewId, sortIndex:3, itemType: SyncItemType.Interview, userId:user1Id),
                                            CreateInterviewSyncPackageMetaInformation(interviewId, sortIndex:4, itemType: SyncItemType.DeleteInterview, userId:user1Id),
                                            CreateInterviewSyncPackageMetaInformation(interviewId, sortIndex:5, itemType: SyncItemType.Interview, userId:userId),
                                            CreateInterviewSyncPackageMetaInformation(interview1Id, sortIndex:6, itemType: SyncItemType.Interview, userId:userId)
                                        };

            lastSyncedPackageId = interviewSyncPackageMetas[0].PackageId;

            indexAccessorMock = new Mock<IQueryableReadSideRepositoryReader<InterviewSyncPackageMeta>>();
            var syncPackageMetas = new List<InterviewSyncPackageMeta>
            {
                CreateInterviewSyncPackageMetaInformation(interviewId, sortIndex:3, itemType: SyncItemType.Interview, userId:user1Id),
                CreateInterviewSyncPackageMetaInformation(interviewId, sortIndex:4, itemType: SyncItemType.DeleteInterview, userId:user1Id),
                CreateInterviewSyncPackageMetaInformation(interviewId, sortIndex:5, itemType: SyncItemType.Interview, userId:userId),
                CreateInterviewSyncPackageMetaInformation(interview1Id, sortIndex:6, itemType: SyncItemType.Interview, userId:userId)
            };


            indexAccessorMock.Setup(x => x.Query(Moq.It.IsAny<Func<IQueryable<InterviewSyncPackageMeta>, List<InterviewSyncPackageMeta>>>()))
                .Returns(syncPackageMetas);

            syncManager = CreateSyncManager(devices: devices, interviewSyncPackageReader: indexAccessorMock.Object);
        };

        Because of = () =>
            result = syncManager.GetInterviewPackageIdsWithOrder(userId, deviceId, lastSyncedPackageId);

        It should_return_not_null_result = () =>
            result.ShouldNotBeNull();

        It should_return_list_with_2_package_ids = () =>
            result.SyncPackagesMeta.Count().ShouldEqual(2);

        It should_return_list_with_package_ids_specified = () =>
            result.SyncPackagesMeta.Select(x => x.Id).ShouldContainOnly(
                interviewSyncPackageMetas[4].PackageId,
                interviewSyncPackageMetas[5].PackageId);

        It should_return_list_with_ordered_by_index_items = () =>
            result.SyncPackagesMeta.Select(x => x.SortIndex).ShouldEqual(new long[]
            {
                interviewSyncPackageMetas[4].SortIndex,
                interviewSyncPackageMetas[5].SortIndex
            });

        private static SyncManager syncManager;
        private static SyncItemsMetaContainer result;

        private const string androidId = "Android";
        private static Guid deviceId = androidId.ToGuid();
        private static TabletDocument tabletDocument;
        private static IReadSideRepositoryReader<TabletDocument> devices;

        private static readonly Guid userId = Guid.Parse("11111111111111111111111111111111");
        private static readonly Guid user1Id = Guid.Parse("22222222222222222222222222222222");
        private static string lastSyncedPackageId = null;

        private static readonly Guid interviewId = Guid.Parse("33333333333333333333333333333333");
        private static readonly Guid interview1Id = Guid.Parse("44444444444444444444444444444444");
        private static Mock<IQueryableReadSideRepositoryReader<InterviewSyncPackageMeta>> indexAccessorMock;
        private static List<InterviewSyncPackageMeta> interviewSyncPackageMetas;
    }
}