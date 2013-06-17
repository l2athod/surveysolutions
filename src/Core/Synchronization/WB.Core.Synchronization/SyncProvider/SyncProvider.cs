﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Main.Core.Commands.Sync;
using Main.Core;
using Main.Core.Entities.SubEntities;
using Ncqrs;
using Ncqrs.Commanding.ServiceModel;
using Raven.Client.Linq;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.SharedKernel.Structures.Synchronization;

namespace WB.Core.Synchronization.SyncProvider
{
    using System;
    using Main.Core.Documents;
    using Newtonsoft.Json;
    
    using Main.Core.Events;
    using Infrastructure;

    public class SyncProvider : ISyncProvider
    {
        private const bool UseCompression = true;

        //compressed content could be larger than uncompressed for small items 
        //private int limitLengtForCompression = 0;

        #warning ViewFactory should be used here
        private readonly IQueryableReadSideRepositoryReader<CompleteQuestionnaireStoreDocument> questionnaires;

        #warning ViewFactory should be used here
        private readonly IQueryableReadSideRepositoryReader<UserDocument> users;

        #warning ViewFactory should be used here
        private readonly IQueryableReadSideRepositoryReader<ClientDeviceDocument> devices;
        

        public SyncProvider(
            IQueryableReadSideRepositoryReader<CompleteQuestionnaireStoreDocument> surveys,
            IQueryableReadSideRepositoryReader<UserDocument> users,
            IQueryableReadSideRepositoryReader<ClientDeviceDocument> devices)
        {
            this.questionnaires = surveys;
            this.users = users;
            this.devices = devices;
        }

        public SyncItem GetSyncItem(Guid id, string type)
        {
            switch (type)
            {
                case SyncItemType.File:
                    return null; // todo: file support
                case SyncItemType.Questionnare:
                    return GetItem(CreateQuestionnarieDocument(id), id, type);
                case SyncItemType.User:
                    return GetItem(this.users.GetById(id), id, type);
                default:
                    return null;
            }
        }

        public IEnumerable<SyncItemsMeta> GetAllARIds(Guid userId)
        {
            var result = new List<SyncItemsMeta>();

            List<Guid> users = GetUsers(userId);
            result.AddRange(users.Select(i => new SyncItemsMeta(i, SyncItemType.User, null)));

            List<Guid> questionnaires = GetQuestionnaires(users);
            result.AddRange(questionnaires.Select(i => new SyncItemsMeta(i, SyncItemType.Questionnare, null)));
            /*
                        //temporary disabled due to non support in android app
                        List<Guid> files = GetFiles();
                        result.AddRange(files.Select(i => new SyncItemsMeta(i, SyncItemType.File, null)));
            */

            return result;
        }


        private List<Guid> GetQuestionnaires(List<Guid> users)
        {
            var listOfStatuses = SurveyStatus.StatusAllowDownSupervisorSync();
            return this.questionnaires.Query<List<Guid>>(_ => _
                                                                  .Where(q => q.Status.PublicId.In(listOfStatuses)
                                                                              && q.Responsible != null &&
                                                                              q.Responsible.Id.In(users))
                                                                  .Select(i => i.PublicKey)
                                                                  .ToList());
        }

        private List<Guid> GetUsers(Guid userId)
        {
            var supervisor =
                users.Query<UserLight>(_ => _.Where(u => u.PublicKey == userId).Select(u => u.Supervisor).FirstOrDefault());
            if (supervisor == null)
                throw new ArgumentException("user is absent");
            return
                 this.users.Query<List<Guid>>(_ => _
                     .Where(t => t.Supervisor != null && t.Supervisor.Id == supervisor.Id)
                     .Select(u => u.PublicKey)
                     .ToList());

        }


        public Guid CheckAndCreateNewSyncActivity(ClientIdentifier identifier)
        {

            var commandService = NcqrsEnvironment.Get<ICommandService>();
            Guid deviceId;
            //device verification
            ClientDeviceDocument device = null;
            if (identifier.ClientKey.HasValue || identifier.ClientKey != Guid.Empty)
            {
                device = devices.GetById(identifier.ClientKey.Value);
                if (device == null)
                {
                    //keys were provided but we can't find device
                    throw new InvalidDataException("Unknown device.");
                }

                deviceId = identifier.ClientKey.Value;
            }
            else //register new device
            {
                deviceId = Guid.NewGuid();
                
                commandService.Execute(new CreateClientDeviceCommand(deviceId, identifier.ClientDeviceKey, identifier.ClientInstanceKey));
            }


            Guid syncActivityId = Guid.NewGuid();
            commandService.Execute(new CreateSyncActivityCommand(syncActivityId, deviceId));



            throw new NotImplementedException();
        }
        
        public bool HandleSyncItem(SyncItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Content))
                return false;

            var items = GetContentAsItem<AggregateRootEvent[]>(item.Content);

            var processor = new SyncEventHandler();
            processor.Merge(items);
            processor.Commit();

            return true;
        }

        private CompleteQuestionnaireDocument CreateQuestionnarieDocument(Guid id)
        {
            var retval = new CompleteQuestionnaireDocument();
            var data = this.questionnaires.GetById(id);

            retval.CreatedBy = data.CreatedBy;
            retval.CreationDate = data.CreationDate;
            retval.Creator = data.Creator;
            retval.LastEntryDate = data.LastEntryDate;
            retval.PublicKey = data.PublicKey;
            retval.Responsible = data.Responsible;
            retval.Status = data.Status;
            retval.TemplateId = data.TemplateId;
            retval.Title = data.Title;
            
            retval.Children = data.Children;
            return retval;
        }

        private SyncItem GetItem(object item, Guid id, string type)
        {
            if (item == null)
            {
                return null;
            }

            var result = new SyncItem {Id = id, 
                Content = GetItemAsContent(item), 
                ItemType = type, 
                IsCompressed = UseCompression};

            return result;
        }

        private string GetItemAsContent(object item)
        {
            var settings = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Objects};
            string itemToSync = JsonConvert.SerializeObject(item, Formatting.None, settings);
            
            return UseCompression ? PackageHelper.CompressString(itemToSync) : itemToSync;
        }


        private T GetContentAsItem<T>(string content)
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
            return JsonConvert.DeserializeObject<T>(PackageHelper.DecompressString(content), settings);
        }

    }
}
