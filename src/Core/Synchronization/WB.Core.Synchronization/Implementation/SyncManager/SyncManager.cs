using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Entities.SubEntities;
using WB.Core.GenericSubdomains.Utils.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernel.Structures.Synchronization;
using WB.Core.SharedKernel.Structures.Synchronization.SurveyManagement;
using WB.Core.SharedKernels.DataCollection.Commands.User;
using WB.Core.SharedKernels.DataCollection.Views;
using WB.Core.Synchronization.Commands;
using WB.Core.Synchronization.Documents;
using WB.Core.Synchronization.Implementation.ReadSide.Indexes;
using WB.Core.Synchronization.SyncStorage;

namespace WB.Core.Synchronization.Implementation.SyncManager
{
    internal class SyncManager : ISyncManager
    {
        private readonly IReadSideKeyValueStorage<ClientDeviceDocument> devices;
        private readonly IIncomingPackagesQueue incomingQueue;
        private readonly ILogger logger;
        private readonly ICommandService commandService;
        private readonly IQueryableReadSideRepositoryReader<UserDocument> userStorage;
        private readonly IReadSideRepositoryIndexAccessor indexAccessor;

        private readonly IQueryableReadSideRepositoryReader<UserSyncPackage> userPackageStorage;
        private readonly IQueryableReadSideRepositoryReader<QuestionnaireSyncPackage> questionnairePackageStorage;
        private readonly IQueryableReadSideRepositoryReader<InterviewSyncPackage> interviewPackageStore;

        private readonly string userQueryIndexName = typeof(UserSyncPackagesByBriefFields).Name;
        private readonly string interviewQueryIndexName = typeof(InterviewSyncPackagesByBriefFields).Name;
        private readonly string questionnireQueryIndexName = typeof(QuestionnaireSyncPackagesByBriefFields).Name;

        public SyncManager(IReadSideKeyValueStorage<ClientDeviceDocument> devices, 
            IIncomingPackagesQueue incomingQueue,
            ILogger logger, 
            ICommandService commandService, 
            IQueryableReadSideRepositoryReader<UserDocument> userStorage,
            IReadSideRepositoryIndexAccessor indexAccessor, IQueryableReadSideRepositoryReader<UserSyncPackage> userPackageStorage, IQueryableReadSideRepositoryReader<QuestionnaireSyncPackage> questionnairePackageStorage, IQueryableReadSideRepositoryReader<InterviewSyncPackage> interviewPackageStore)
        {
            this.devices = devices;
            this.incomingQueue = incomingQueue;
            this.logger = logger;
            this.commandService = commandService;
            this.userStorage = userStorage;
            this.indexAccessor = indexAccessor;
            this.userPackageStorage = userPackageStorage;
            this.questionnairePackageStorage = questionnairePackageStorage;
            this.interviewPackageStore = interviewPackageStore;
        }

        public HandshakePackage ItitSync(ClientIdentifier clientIdentifier)
        {
            if (clientIdentifier.ClientInstanceKey == Guid.Empty)
                throw new ArgumentException("ClientInstanceKey is incorrect.");

            if (string.IsNullOrWhiteSpace(clientIdentifier.ClientDeviceKey))
                throw new ArgumentException("ClientDeviceKey is incorrect.");

            if (string.IsNullOrWhiteSpace(clientIdentifier.ClientVersionIdentifier))
                throw new ArgumentException("ClientVersionIdentifier is incorrect.");

            return this.CheckAndCreateNewSyncActivity(clientIdentifier);
        }

        public void SendSyncItem(string item)
        {
            this.incomingQueue.PushSyncItem(item);
        }

        public SyncItemsMetaContainer GetQuestionnaireArIdsWithOrder(Guid userId, Guid clientRegistrationId, string lastSyncedPackageId)
        {
            this.MakeSureThisDeviceIsRegisteredOrThrow(clientRegistrationId);

            var updateFromLastPakageByQuestionnaire =
                this.GetUpdateFromLastPakage(lastSyncedPackageId, this.indexAccessor.Query<QuestionnaireSyncPackage>(questionnireQueryIndexName));

            return new SyncItemsMetaContainer
            {
                SyncPackagesMeta = updateFromLastPakageByQuestionnaire
            };
        }

        public SyncItemsMetaContainer GetUserArIdsWithOrder(Guid userId, Guid clientRegistrationId, string lastSyncedPackageId)
        {
            this.MakeSureThisDeviceIsRegisteredOrThrow(clientRegistrationId);

            var updateFromLastPakageByUser =
                this.GetUpdateFromLastPakage(lastSyncedPackageId, this.indexAccessor.Query<UserSyncPackage>(userQueryIndexName).Where(x => x.UserId == userId));

            return new SyncItemsMetaContainer
            {
                SyncPackagesMeta = updateFromLastPakageByUser
            };
        }

        public SyncItemsMetaContainer GetInterviewArIdsWithOrder(Guid userId, Guid clientRegistrationId, string lastSyncedPackageId)
        {
            this.MakeSureThisDeviceIsRegisteredOrThrow(clientRegistrationId);

            var updateFromLastPakageByInterview =
                this.GetUpdateFromLastPakage(lastSyncedPackageId, this.indexAccessor.Query<InterviewSyncPackage>(interviewQueryIndexName).Where(x => x.UserId == userId));

            return new SyncItemsMetaContainer
            {
                SyncPackagesMeta = updateFromLastPakageByInterview
            };
        }

        public void LinkUserToDevice(Guid interviewerId, string deviceId)
        {
            if (interviewerId == Guid.Empty)
                throw new ArgumentException("Interview id is not set.");

            if (string.IsNullOrEmpty(deviceId))
                throw new ArgumentException("Device id is not set.");

            commandService.Execute(new LinkUserToDevice(interviewerId, deviceId));
        }

        public UserSyncPackageDto ReceiveUserSyncPackage(Guid clientRegistrationId, string packageId)
        {
            this.MakeSureThisDeviceIsRegisteredOrThrow(clientRegistrationId);

            var package = this.userPackageStorage.GetById(packageId);

            if (package == null)
                throw new ArgumentException(string.Format("Package {0} with user is absent", packageId));

            return new UserSyncPackageDto
                   {
                       PackageId = package.PackageId, 
                       Content = package.Content, 
                       Timestamp = package.Timestamp, 
                       SortIndex = package.SortIndex,
                       UserId = package.UserId
                   };
        }

        public QuestionnaireSyncPackageDto ReceiveQuestionnaireSyncPackage(Guid clientRegistrationId, string packageId)
        {
            var package = this.questionnairePackageStorage.GetById(packageId);

            if (package == null)
                throw new ArgumentException(string.Format("Package {0} with questionnaire is absent", packageId));

            return new QuestionnaireSyncPackageDto
                   {
                       PackageId = package.PackageId,
                       Content = package.Content,
                       Timestamp = package.Timestamp,
                       SortIndex = package.SortIndex,
                       QuestionnaireId = package.QuestionnaireId,
                       QuestionnaireVersion = package.QuestionnaireVersion,
                       ItemType = package.ItemType,
                       MetaInfo = package.MetaInfo,
                   };
        }

        public InterviewSyncPackageDto ReceiveInterviewSyncPackage(Guid clientRegistrationId, string packageId)
        {
            this.MakeSureThisDeviceIsRegisteredOrThrow(clientRegistrationId);

            InterviewSyncPackage package = this.interviewPackageStore.GetById(packageId);
            if (package == null)
                throw new ArgumentException(string.Format("Package {0} with interview is absent", packageId));
            return new InterviewSyncPackageDto
                   {
                       PackageId = package.PackageId,
                       Content = package.Content,
                       Timestamp = package.Timestamp,
                       SortIndex = package.SortIndex,
                       MetaInfo = package.MetaInfo,
                       InterviewId = package.InterviewId,
                       VersionedQuestionnaireId = package.VersionedQuestionnaireId,
                       UserId = package.UserId,
                       ItemType = package.ItemType,
                   };
        }

        public string GetPackageIdByTimestamp(Guid userId, DateTime timestamp)
        {
            /*var users = this.GetUserTeamates(userId);
            var items = this.indexAccessor.Query<SynchronizationDelta>(this.queryIndexName);
            var userIds = users.Concat(new[] { Guid.Empty });

            SynchronizationDelta meta = items.Where(x => timestamp >= x.Timestamp && x.UserId.In(userIds))
                .ToList()
                .OrderBy(x => x.SortIndex)
                .Last();
            return new SynchronizationChunkMeta(meta.PublicKey).Id;*/
            return string.Empty;
        }

        private IEnumerable<Guid> GetUserTeamates(Guid userId)
        {
            var user = userStorage.Query(_ => _.Where(u => u.PublicKey == userId)).ToList().FirstOrDefault();
            if (user == null)
                return Enumerable.Empty<Guid>();

            Guid supervisorId = user.Roles.Contains(UserRoles.Supervisor) ? userId : user.Supervisor.Id;

            var team =
                userStorage.Query(
                    _ => _.Where(u => u.Supervisor != null && u.Supervisor.Id == supervisorId).Select(u => u.PublicKey)).ToList();
            team.Add(supervisorId);
            return team;
        }

        private HandshakePackage CheckAndCreateNewSyncActivity(ClientIdentifier identifier)
        {
            Guid ClientRegistrationKey;

            ClientDeviceDocument device = null;
            if (identifier.ClientRegistrationKey.HasValue)
            {
                if (identifier.ClientRegistrationKey.Value == Guid.Empty)
                    throw new ArgumentException("Unknown device.");

                device = devices.GetById(identifier.ClientRegistrationKey.Value);
                if (device == null)
                {
                    throw new Exception("Device was not found. It could be linked to other system.");
                }

                //old sync devices are already in use
                //lets allow to sync them for awhile
                //then we have to delete this code
                if (device.SupervisorKey == Guid.Empty)
                {
                    logger.Error("Old registered device is synchronizing. Errors could be on client in case of different team.");
                }
                else if (device.SupervisorKey != identifier.SupervisorPublicKey)
                {
                    throw new Exception("Device was assigned to another Supervisor.");
                }

                //TODO: check device validity

                ClientRegistrationKey = device.PublicKey;
            }
            else //register new device
            {
                ClientRegistrationKey = Guid.NewGuid();

                commandService.Execute(new CreateClientDeviceCommand(ClientRegistrationKey,
                    identifier.ClientDeviceKey, identifier.ClientInstanceKey, identifier.SupervisorPublicKey));
            }

            Guid syncActivityKey = Guid.NewGuid();

            return new HandshakePackage(identifier.UserId, identifier.ClientInstanceKey, syncActivityKey, ClientRegistrationKey);
        }

        private IEnumerable<SynchronizationChunkMeta> GetUpdateFromLastPakage<T>(string lastSyncedPackageId, IQueryable<T> items) where T : ISyncPackage
        {
            if (lastSyncedPackageId == null)
            {
                var orderedQueryable = items.OrderBy(x => x.SortIndex).ToList();

                List<SynchronizationChunkMeta> fullListResult = orderedQueryable
                    .Select(s => new SynchronizationChunkMeta(s.PackageId))
                    .ToList();

                return fullListResult;
            }

            var lastSyncedPackage = items.FirstOrDefault(x => x.PackageId == lastSyncedPackageId);

            if (lastSyncedPackage == null)
            {
                throw new SyncPackageNotFoundException(string.Format("Sync package with id {0} was not found on server", lastSyncedPackageId));
            }

            int lastSyncedSortIndex = lastSyncedPackage.SortIndex;

            var orderedPackages = items
                .Where(x => x.SortIndex > lastSyncedSortIndex)
                .OrderBy(x => x.SortIndex)
                .ToList();

            var deltas = orderedPackages
                    .Select(s => new SynchronizationChunkMeta(s.PackageId))
                    .ToList();

            return deltas;
        }

        private void MakeSureThisDeviceIsRegisteredOrThrow(Guid clientRegistrationId)
        {
            var device = this.devices.GetById(clientRegistrationId);

            if (device == null)
            {
                throw new ArgumentException("Device was not found.");
            }
        }
    }
}