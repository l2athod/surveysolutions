using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Main.Core.Entities.SubEntities;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities.Answers;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.ValueObjects;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.DataCollection.Views.Interview;
using WB.Core.SharedKernels.Questionnaire.Documents;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;

namespace WB.Core.BoundedContexts.Headquarters.Views.Interview
{
    public class InterviewFactory : IInterviewFactory
    {
        private readonly IQueryableReadSideRepositoryReader<InterviewSummary> summaryRepository;
        private readonly IQuestionnaireStorage questionnaireStorage;
        private readonly ISessionProvider sessionProvider;

        public InterviewFactory(
            IQueryableReadSideRepositoryReader<InterviewSummary> summaryRepository,
            IQuestionnaireStorage questionnaireStorage,
            ISessionProvider sessionProvider)
        {
            this.summaryRepository = summaryRepository;
            this.questionnaireStorage = questionnaireStorage;
            this.sessionProvider = sessionProvider;
        }

        public Identity[] GetQuestionsWithFlagBySectionId(QuestionnaireIdentity questionnaireId, Guid interviewId,
            Identity sectionId)
        {
            var questionnaire = questionnaireStorage.GetQuestionnaire(questionnaireId, null);
            var questionsInSection = questionnaire.GetChildQuestions(sectionId.Id);

            if (!questionsInSection.Any()) return Array.Empty<Identity>();

            return sessionProvider.GetSession().Connection.Query<(Guid entityid, int[] rostervector)>(
                    $@"SELECT {Column.EntityId}, {Column.RosterVector}
                       FROM {Table.InterviewsView}
                       WHERE {Column.InterviewId} = @InterviewId
                        AND {Column.HasFlag} = true
                        AND {Column.EntityId} IN (@questionsInSection)
                        AND {Column.RosterVector} = @RosterVector",
                    new
                    {
                        InterviewId = interviewId,
                        RosterVector = sectionId.RosterVector.Array,
                        questionsInSection
                    })
                .Select(x => Identity.Create(x.entityid, x.rostervector))
                .ToArray();
        }

        public Identity[] GetFlaggedQuestionIds(Guid interviewId)
            => sessionProvider.GetSession().Connection.Query<(Guid entityId, int[] rosterVector)>(
                    $"SELECT {Column.EntityId}, {Column.RosterVector} " +
                    $"FROM {Table.InterviewsView} WHERE {Column.InterviewId} = @InterviewId AND {Column.HasFlag} = true",
                    new { InterviewId = interviewId })
                .Select(x => Identity.Create(x.entityId, x.rosterVector))
                .ToArray();

        public void SetFlagToQuestion(Guid interviewId, Identity questionIdentity, bool flagged)
        {
            ThrowIfInterviewDeletedOrReadOnly(interviewId);
            
            var stateRows = GetInterviewEntities(null, interviewId);

            var perEntity = stateRows.ToDictionary(r => InterviewStateIdentity.Create(r.Identity));

            var id = InterviewStateIdentity.Create(questionIdentity);
            if (perEntity.TryGetValue(id, out var row))
            {
                row.HasFlag = flagged;
            }
            else
            {
                perEntity[id] = new InterviewEntity
                {
                    Identity = questionIdentity,
                    IsEnabled = true,
                    HasFlag = flagged
                };
            }

            SaveInterviewStateItem(interviewId, perEntity.Values.Where(v=> !v.IsAllFieldsDefault()).ToList());
        }

        public void RemoveInterview(Guid interviewId)
            => sessionProvider.GetSession().Connection.Execute(
                $@"DELETE FROM {Table.Interviews} i
                    USING {Table.InterviewSummaries} s
                    WHERE i.{Column.InterviewId} = s.id AND s.{Column.InterviewId} = @InterviewId",
                new { InterviewId = interviewId });

        public InterviewStringAnswer[] GetMultimediaAnswersByQuestionnaire(QuestionnaireIdentity questionnaireIdentity)
        {
            return sessionProvider.GetSession().Connection
                .Query<InterviewStringAnswer>(
                    $@"select s.{Column.InterviewId}, i.{Column.AsString} as answer
                       from {Table.Interviews} i
                       join {Table.InterviewSummaries} s on s.id = i.{Column.InterviewId} 
                            and s.{Column.QuestionnaireIdentity} = @questionnaireIdentity
                       join {Table.QuestionnaireEntities} q on q.id = i.{Column.EntityId}
                        where i.{Column.AsString} is not null and q.question_type = @questionType", new
                        {
                            questionType = QuestionType.Multimedia,
                            questionnaireIdentity = questionnaireIdentity.ToString()
                        })
                .ToArray();
        }

        public InterviewStringAnswer[] GetAudioAnswersByQuestionnaire(QuestionnaireIdentity questionnaireIdentity)
            => sessionProvider.GetSession().Connection.Query<InterviewStringAnswer>(
                $"SELECT i.{Column.InterviewId}, i.{Column.AsAudio}->>'{nameof(AudioAnswer.FileName)}' as Answer " +
                $"FROM readside.interviewsummaries s INNER JOIN {Table.InterviewsView} i ON(s.interviewid = i.{Column.InterviewId}) " +
                $"WHERE {Column.QuestionnaireIdentity} = '{questionnaireIdentity}' " +
                $"AND {Column.AsAudio} IS NOT NULL " +
                $"AND {Column.IsEnabled} = true").ToArray();

        public Guid[] GetAnsweredGpsQuestionIdsByQuestionnaire(QuestionnaireIdentity questionnaireIdentity)
            => sessionProvider.GetSession().Connection.Query<Guid>(
                $"SELECT {Column.EntityId} " +
                $"FROM readside.interviewsummaries s INNER JOIN {Table.InterviewsView} i ON(s.interviewid = i.{Column.InterviewId}) " +
                $"WHERE {Column.QuestionnaireIdentity} = '{questionnaireIdentity}' " +
                $"AND {Column.AsGpsColumn} is not null " +
                $"GROUP BY {Column.EntityId} ").ToArray();

        public string[] GetQuestionnairesWithAnsweredGpsQuestions()
            => sessionProvider.GetSession().Connection.Query<string>(
                $@"select distinct {Column.QuestionnaireIdentity} from {Table.InterviewSummaries}").ToArray();

        public InterviewGpsAnswer[] GetGpsAnswersByQuestionIdAndQuestionnaire(
            QuestionnaireIdentity questionnaireIdentity,
            Guid gpsQuestionId, int maxAnswersCount, double northEastCornerLatitude, double southWestCornerLatitude,
            double northEastCornerLongtitude, double southWestCornerLongtitude)
            => sessionProvider.GetSession().Connection.Query<InterviewGpsAnswer>(
                    $@"select interviewid, latitude, longitude
                        from (
	                        select s.interviewid, (i.asgps ->> 'Latitude')::float8 as latitude, (i.asgps ->> 'Longitude')::float8 as longitude
                            from readside.interviews i
                            join {Table.QuestionnaireEntities} q on q.id = i.entityid
                            join readside.interviewsummaries s on s.id = i.interviewid
                                where i.asgps is not null and s.questionnaireidentity = @Questionnaire and q.entityid = @QuestionId            
	                        ) as q
                        where  latitude > @SouthWestCornerLatitude and latitude < @NorthEastCornerLatitude
                            and longitude > @SouthWestCornerLongtitude
                                {(northEastCornerLongtitude >= southWestCornerLongtitude ? "AND" : "OR")} 
                                longitude < @NorthEastCornerLongtitude
                        limit @MaxCount",
                    new
                    {
                        Questionnaire = questionnaireIdentity.ToString(),
                        QuestionId = gpsQuestionId,
                        MaxCount = maxAnswersCount,
                        SouthWestCornerLatitude = southWestCornerLatitude,
                        NorthEastCornerLatitude = northEastCornerLatitude,
                        SouthWestCornerLongtitude = southWestCornerLongtitude,
                        NorthEastCornerLongtitude = northEastCornerLongtitude
                    })
                .ToArray();
        
        public void Save(InterviewState state)
        {
            var rows = GetInterviewEntities(null, state.Id);
            
            var perEntity = rows.ToDictionary(r => InterviewStateIdentity.Create(r.Identity));

            foreach (var removed in state.Removed)
            {
                perEntity.Remove(removed);
            }

            void Upsert<T>(Dictionary<InterviewStateIdentity, T> valueSource, Action<InterviewEntity, T> action)
            {
                foreach (var item in valueSource)
                {
                    if (perEntity.TryGetValue(item.Key, out var entity))
                    {
                        action(entity, item.Value);
                    }
                    else
                    {
                        entity = new InterviewEntity
                        {
                            InterviewId = state.Id,
                            IsEnabled = true,
                            Identity = Identity.Create(item.Key.Id, item.Key.RosterVector)
                        };
                        action(entity, item.Value);
                        perEntity.Add(item.Key, entity);
                    }
                }
            }

            Upsert(state.Enablement, (entity, value) => entity.IsEnabled = value);
            Upsert(state.ReadOnly.ToDictionary(r => r, v => true), (e, v) => e.IsReadonly = v);
            Upsert(state.Validity, (e, v) => e.InvalidValidations = v.Validations);

            Upsert(state.Answers, (entity, answer) =>
            {
                entity.AsString = answer.AsString;
                entity.AsInt = answer.AsInt;
                entity.AsLong = answer.AsLong;
                entity.AsDouble = answer.AsDouble;
                entity.AsDateTime = answer.AsDatetime;
                entity.AsList = answer.AsList;
                entity.AsIntArray = answer.AsIntArray;
                entity.AsIntMatrix = answer.AsIntMatrix;
                entity.AsGps = answer.AsGps;
                entity.AsBool = answer.AsBool;
                entity.AsYesNo = answer.AsYesNo;
                entity.AsAudio = answer.AsAudio;
                entity.AsArea = answer.AsArea;
            });
            
            SaveInterviewStateItem(state.Id, perEntity.Values.Where(v => !v.IsAllFieldsDefault()).ToList());
        }

        private void SaveInterviewStateItem(Guid interviewId, ICollection<InterviewEntity> stateItems)
        {
            var conn = sessionProvider.GetSession().Connection;

            var id = summaryRepository.GetById(interviewId.FormatGuid()).Id;

            var idsList = string.Join(",", stateItems.Select(s => "'" + s.Identity.Id + "'").Distinct());

            var entityMap = stateItems.Count == 0
                ? new Dictionary<Guid, int>()
                : conn.Query<(int id, Guid entityId)>($@"select q.id, q.entityId
                        from {Table.QuestionnaireEntities} q
                        join {Table.InterviewSummaries} s on s.{Column.QuestionnaireIdentity} = q.{Column.QuestionnaireIdentity}
                        where {Column.EntityId} in ({idsList}) and s.{Column.InterviewId} = '{interviewId}'")
                    .ToDictionary(q => q.entityId, q => q.id);

            conn.Execute($@"delete from {Table.Interviews} where {Column.InterviewId} = {id}");

            var npgConnection = conn as NpgsqlConnection ?? throw new NotSupportedException("Cannot import over non Postgres connection");

            using (var importer = npgConnection.BeginBinaryImport(@"copy 
                readside.interviews (
                    interviewid, entityid, rostervector, isenabled, isreadonly, invalidvalidations, asstring, asint, aslong, 
                    asdouble, asdatetime, aslist, asintarray, asintmatrix, asgps, asbool, asyesno, asaudio, asarea, hasflag 
                ) 
                from stdin (format binary)"))
            {
                foreach (var item in stateItems)
                {
                    importer.StartRow();
                    Write(id);
                    Write(entityMap[item.Identity.Id]);
                    Write(item.Identity.RosterVector.Array);
                    Write(item.IsEnabled);
                    Write(item.IsReadonly);
                    Write(item.InvalidValidations);
                    Write(item.AsString);
                    Write(item.AsInt);
                    Write(item.AsLong);
                    Write(item.AsDouble);
                    Write(item.AsDateTime);
                    WriteJson(item.AsList);
                    Write(item.AsIntArray);
                    WriteJson(item.AsIntMatrix);
                    WriteJson(item.AsGps);
                    Write(item.AsBool);
                    WriteJson(item.AsYesNo);
                    WriteJson(item.AsAudio);
                    WriteJson(item.AsArea);
                    Write(item.HasFlag);
                    
                    void Write<T>(T value)
                    {
                        if (value == null) importer.WriteNull();
                        else importer.Write(value);
                    }

                    void WriteJson<T>(T value)
                    {
                        if (value == null) importer.WriteNull();
                        else importer.Write(JsonConvert.SerializeObject(value), NpgsqlDbType.Jsonb);
                    }
                }
            }
        }
       
        public IEnumerable<InterviewEntity> GetInterviewEntities(QuestionnaireIdentity questionnaireId, IEnumerable<Guid> interviews)
        {
            var questionnaire = questionnaireId != null ? questionnaireStorage.GetQuestionnaire(questionnaireId, null) : null;

            var connection = sessionProvider.GetSession().Connection;

            var ids = string.Join(",", interviews.Select(i => "'" + i.ToString() + "'"));

            var queryResult = connection.Query<InterviewEntityDto>(
                "SELECT interviewid, entityid, rostervector, isenabled, isreadonly, invalidvalidations, asstring, asint," +
                " aslong, asdouble, asdatetime, aslist, asintarray, asintmatrix, asgps, asbool, asyesno, asaudio, asarea, hasflag " +
                $" from {Table.InterviewsView} where {Column.InterviewId} in ({ids})", commandTimeout: 0, buffered: false);

            foreach (var result in queryResult)
            {
                var entity = new InterviewEntity();

                entity.InterviewId = result.InterviewId;
                entity.Identity = new Identity(result.EntityId, result.RosterVector);

                entity.IsEnabled = result.IsEnabled;
                entity.IsReadonly = result.IsReadonly;
                entity.InvalidValidations = result.InvalidValidations;
                entity.AsString = result.AsString;
                entity.AsInt = result.AsInt;
                entity.AsLong = result.AsLong;
                entity.AsDouble = result.AsDouble;
                entity.AsDateTime = result.AsDateTime;
                entity.AsIntArray = result.AsIntArray;
                entity.AsBool = result.AsBool;
                entity.HasFlag = result.HasFlag;

                entity.AsList = Deserialize<InterviewTextListAnswer[]>(result.AsList);
                entity.AsIntMatrix = Deserialize<int[][]>(result.AsIntMatrix);
                entity.AsGps = Deserialize<GeoPosition>(result.AsGps);
                entity.AsYesNo = Deserialize<AnsweredYesNoOption[]>(result.AsYesNo);
                entity.AsAudio = Deserialize<AudioAnswer>(result.AsAudio);
                entity.AsArea = Deserialize<Area>(result.AsArea);

                if (questionnaire != null)
                    entity.EntityType = GetEntityType(entity.Identity.Id, questionnaire);

                T Deserialize<T>(string value) where T : class
                {
                    if (string.IsNullOrWhiteSpace(value)) return null;
                    return JsonConvert.DeserializeObject<T>(value);
                }

                yield return entity;
            }
        }

        #region Obsolete InterviewData
        public List<InterviewEntity> GetInterviewEntities(QuestionnaireIdentity questionnaireId, Guid interviews)
        {
            return GetInterviewEntities(questionnaireId, new[] { interviews }).ToList();
        }

        private EntityType GetEntityType(Guid entityid, IQuestionnaire questionnaire)
        {
            if (questionnaire.IsQuestion(entityid)) return EntityType.Question;
            if (questionnaire.IsVariable(entityid)) return EntityType.Variable;
            if (questionnaire.IsStaticText(entityid)) return EntityType.StaticText;
            if (questionnaire.HasGroup(entityid)) return EntityType.Section;

            throw new NotSupportedException("Unknown entity type");
        }

        public Dictionary<string, InterviewLevel> GetInterviewDataLevels(IQuestionnaire questionnaire, List<InterviewEntity> interviewEntities)
        {
            var interviewLevels = interviewEntities
                .GroupBy(x => x.Identity?.RosterVector ?? RosterVector.Empty)
                .Select(x => ToInterviewLevel(x.Key, x.ToArray(), questionnaire));

            var interviewDataLevels = interviewLevels.ToDictionary(k => CreateLevelIdFromPropagationVector(k.RosterVector), v => v);
            return interviewDataLevels;
        }

        private InterviewLevel ToInterviewLevel(RosterVector rosterVector, InterviewEntity[] interviewDbEntities,
            IQuestionnaire questionnaire)
        {
            Dictionary<ValueVector<Guid>, int?> scopeVectors = new Dictionary<ValueVector<Guid>, int?>();
            if (rosterVector.Length > 0)
            {
                // too slow
                scopeVectors = interviewDbEntities
                    .Select(x => questionnaire.GetRosterSizeSourcesForEntity(x.Identity.Id))
                    .Select(x => new ValueVector<Guid>(x))
                    .Distinct()
                    .ToDictionary(x => x, x => (int?)0);
            }
            else
            {
                scopeVectors.Add(new ValueVector<Guid>(), 0);
            }

            var disabledGroups = interviewDbEntities
                .Where(x => x.EntityType == EntityType.Section && x.IsEnabled == false).Select(x => x.Identity.Id)
                .ToHashSet();

            var disabledVariables = interviewDbEntities
                .Where(x => x.EntityType == EntityType.Variable && x.IsEnabled == false).Select(x => x.Identity.Id)
                .ToHashSet();

            var dictionary = interviewDbEntities.Where(x => x.EntityType == EntityType.Variable)
                .Select(x => new { x.Identity.Id, Answer = ToObjectAnswer(x) })
                .ToDictionary(x => x.Id, x => x.Answer);

            var interviewStaticTexts = interviewDbEntities.Where(x => x.EntityType == EntityType.StaticText)
                .Select(ToStaticText).ToDictionary(x => x.Id);

            var questionsSearchCache = interviewDbEntities.Where(x => x.EntityType == EntityType.Question)
                .Select(ToQuestion).ToDictionary(x => x.Id);

            return new InterviewLevel
            {
                RosterVector = rosterVector,
                DisabledGroups = disabledGroups,
                DisabledVariables = disabledVariables,
                Variables = dictionary,
                StaticTexts = interviewStaticTexts,
                QuestionsSearchCache = questionsSearchCache,
                ScopeVectors = scopeVectors
            };
        }

        private InterviewQuestion ToQuestion(InterviewEntity entity)
        {
            var objectAnswer = ToObjectAnswer(entity);

            return new InterviewQuestion
            {
                Id = entity.Identity.Id,
                Answer = objectAnswer,
                FailedValidationConditions = entity.InvalidValidations?.Select(x => new FailedValidationCondition(x)).ToReadOnlyCollection(),
                QuestionState = ToQuestionState(entity, objectAnswer != null)
            };
        }

        private QuestionState ToQuestionState(InterviewEntity entity, bool hasAnswer)
        {
            QuestionState state = 0;

            if (entity.IsEnabled) state = state.With(QuestionState.Enabled);
            if (entity.IsReadonly) state = state.With(QuestionState.Readonly);
            if (entity.InvalidValidations == null) state = state.With(QuestionState.Valid);
            if (entity.HasFlag) state = state.With(QuestionState.Flagged);
            if (hasAnswer) state = state.With(QuestionState.Answered);

            return state;
        }

        private InterviewStaticText ToStaticText(InterviewEntity entity) => new InterviewStaticText
        {
            Id = entity.Identity.Id,
            IsEnabled = entity.IsEnabled,
            FailedValidationConditions = (entity.InvalidValidations?.Select(x => new FailedValidationCondition(x)) ??
                                          new FailedValidationCondition[0]).ToReadOnlyCollection()
        };

        private object ToObjectAnswer(InterviewEntity entity) => entity.AsString ?? entity.AsInt ?? entity.AsDouble ??
                                                                 entity.AsDateTime ?? entity.AsLong ??
                                                                 entity.AsBool ?? entity.AsGps ?? entity.AsIntArray ??
                                                                 entity.AsList ?? entity.AsYesNo ??
                                                                 entity.AsIntMatrix ?? entity.AsArea ??
                                                                 (object)entity.AsAudio;

        public static string CreateLevelIdFromPropagationVector(decimal[] vector)
        {
            if (vector.Length == 0)
                return "#";
            return vector.CreateLeveKeyFromPropagationVector();
        }
        #endregion

        private void ThrowIfInterviewDeletedOrReadOnly(Guid interviewId)
        {
            var interview = summaryRepository.GetById(interviewId);

            if (interview == null)
                throw new InterviewException($"Interview {interviewId} not found.");

            ThrowIfInterviewApprovedByHq(interview);
            ThrowIfInterviewReceivedByInterviewer(interview);
        }

        private static void ThrowIfInterviewReceivedByInterviewer(InterviewSummary interview)
        {
            if (interview.ReceivedByInterviewer)
                throw new InterviewException($"Can't modify Interview {interview.InterviewId} on server, because it received by interviewer.");
        }

        private static void ThrowIfInterviewApprovedByHq(InterviewSummary interview)
        {
            if (interview.Status == InterviewStatus.ApprovedByHeadquarters)
                throw new InterviewException($"Interview was approved by Headquarters and cannot be edited. InterviewId: {interview.InterviewId}");
        }
        private static class Column
        {
            public const string InterviewId = "interviewid";
            public const string EntityId = "entityid";
            public const string RosterVector = "rostervector";
            public const string HasFlag = "hasflag";
            public const string IsEnabled = "isenabled";
            public const string AsGpsColumn = "asgps";
            public const string AsString = "asstring";
            public const string AsAudio = "asaudio";
            public const string QuestionnaireIdentity = "questionnaireidentity";
        }

        private static class Table
        {
            public const string Interviews = "readside.interviews";
            public const string InterviewSummaries = "readside.interviewsummaries";
            public const string QuestionnaireEntities = "readside.questionnaire_entities";
            public const string InterviewsView = "readside.interviews_view";
        }
    }
}