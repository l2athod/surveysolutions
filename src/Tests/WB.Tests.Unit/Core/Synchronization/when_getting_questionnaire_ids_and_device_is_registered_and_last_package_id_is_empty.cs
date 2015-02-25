using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Moq;
using WB.Core.GenericSubdomains.Utils;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernel.Structures.Synchronization;
using WB.Core.Synchronization.Documents;
using WB.Core.Synchronization.Implementation.ReadSide.Indexes;
using WB.Core.Synchronization.Implementation.SyncManager;
using WB.Core.Synchronization.SyncStorage;

using It = Machine.Specifications.It;

namespace WB.Tests.Unit.Core.Synchronization
{
    internal class when_getting_questionnaire_ids_and_device_is_registered_and_last_package_id_is_empty : SyncManagerTestContext
    {
        Establish context = () =>
        {
            tabletDocument = CreateTabletDocument(deviceId, androidId);
            devices = Mock.Of<IReadSideRepositoryReader<TabletDocument>>(x => x.GetById(deviceId.FormatGuid()) == tabletDocument);

            questionnaireSyncPackageMetas= new List<QuestionnaireSyncPackageMeta>
            {
                CreateQuestionnaireSyncPackageMetaInformation(questionnaireId, questionnaireVersion:1, sortIndex:1, itemType: SyncItemType.Questionnaire),
                CreateQuestionnaireSyncPackageMetaInformation(questionnaireId, questionnaireVersion:1, sortIndex:2, itemType: SyncItemType.QuestionnaireAssembly),
                CreateQuestionnaireSyncPackageMetaInformation(questionnaireId, questionnaireVersion:2, sortIndex:3, itemType: SyncItemType.Questionnaire),
                CreateQuestionnaireSyncPackageMetaInformation(questionnaireId, questionnaireVersion:1, sortIndex:4, itemType: SyncItemType.DeleteQuestionnaire),
                CreateQuestionnaireSyncPackageMetaInformation(questionnaireId, questionnaireVersion:2, sortIndex:5, itemType: SyncItemType.QuestionnaireAssembly),
            };

            indexAccessorMock = new Mock<IReadSideRepositoryIndexAccessor>();
            indexAccessorMock.Setup(x => x.Query<QuestionnaireSyncPackageMeta>(questionnireQueryIndexName))
                .Returns(questionnaireSyncPackageMetas.AsQueryable());
            syncManager = CreateSyncManager(devices: devices, indexAccessor: indexAccessorMock.Object);
        };

        Because of = () =>
            result = syncManager.GetQuestionnaireArIdsWithOrder(userId, deviceId, lastSyncedPackageId);

        It should_return_not_null_result = () =>
            result.ShouldNotBeNull();

        It should_return_list_with_3_package_ids = () =>
            result.SyncPackagesMeta.Count().ShouldEqual(3);

        It should_return_list_with_package_ids_specified = () =>
            result.SyncPackagesMeta.Select(x => x.Id).ShouldContainOnly(
                "22222222222222222222222222222222_2$3",
                "22222222222222222222222222222222_2$5",
                "22222222222222222222222222222222_1$4");

        It should_return_list_with_ordered_by_index_items = () =>
            result.SyncPackagesMeta.Select(x => x.SortIndex).ShouldEqual(new long[] { 3, 4, 5 });

        private static SyncManager syncManager;
        private static SyncItemsMetaContainer result;

        private static string androidId = "Android";
        private static Guid deviceId = androidId.ToGuid();
        private static TabletDocument tabletDocument;
        private static IReadSideRepositoryReader<TabletDocument> devices;
        
        private static Guid userId = Guid.Parse("11111111111111111111111111111111");
        private static string lastSyncedPackageId = null;
        
        private static Guid questionnaireId = Guid.Parse("22222222222222222222222222222222");
        private static Mock<IReadSideRepositoryIndexAccessor> indexAccessorMock;
        private static readonly string questionnireQueryIndexName = typeof(QuestionnaireSyncPackagesByBriefFields).Name;
        private static List<QuestionnaireSyncPackageMeta> questionnaireSyncPackageMetas;
    }
}