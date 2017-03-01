﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Humanizer;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Main.Core.Events;
using Microsoft.Practices.ServiceLocation;
using Moq;
using Ncqrs.Domain.Storage;
using Ncqrs.Eventing;
using Ncqrs.Eventing.ServiceModel.Bus;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration;
using WB.Core.BoundedContexts.Designer.Implementation.Services.LookupTableService;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Services.CodeGeneration;
using WB.Core.BoundedContexts.Headquarters.Commands;
using WB.Core.BoundedContexts.Headquarters.EventHandler.WB.Core.SharedKernels.SurveyManagement.Views.Questionnaire;
using WB.Core.BoundedContexts.Headquarters.Implementation.Aggregates;
using WB.Core.BoundedContexts.Headquarters.Questionnaires.Translations;
using WB.Core.BoundedContexts.Headquarters.Views.DataExport;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Implementation.Services;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.Aggregates;
using WB.Core.Infrastructure.CommandBus.Implementation;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.Infrastructure.EventBus.Lite.Implementation;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.Implementation;
using WB.Core.Infrastructure.Implementation.Aggregates;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Preloading;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Implementation.Accessors;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities.Answers;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Implementation.Services;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Services;
using WB.Core.SharedKernels.DataCollection.ValueObjects;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails;
using WB.Core.SharedKernels.SurveySolutions;
using WB.Core.SharedKernels.SurveySolutions.Documents;
using WB.Infrastructure.Native.Files.Implementation.FileSystem;
using WB.Infrastructure.Native.Storage.Postgre;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;
using IEvent = WB.Core.Infrastructure.EventBus.IEvent;
using WB.Core.SharedKernels.QuestionnaireEntities;
using WB.Infrastructure.Native.Storage;
using WB.Infrastructure.Native.Storage.Postgre.NhExtensions;
using Configuration = NHibernate.Cfg.Configuration;
using User = WB.Core.BoundedContexts.Designer.Aggregates.User;

namespace WB.Tests.Integration
{
    internal static class Create
    {
        public static CodeGenerator CodeGenerator(
            IMacrosSubstitutionService macrosSubstitutionService = null,
            IExpressionProcessor expressionProcessor = null,
            ILookupTableService lookupTableService = null)
        {
            return new CodeGenerator(
                macrosSubstitutionService ?? DefaultMacrosSubstitutionService(),
                expressionProcessor ?? ServiceLocator.Current.GetInstance<IExpressionProcessor>(),
                lookupTableService ?? ServiceLocator.Current.GetInstance<ILookupTableService>(),
                new FileSystemIOAccessor(), 
                GetCompilerSettingsStub());
        }

        private static ICompilerSettings GetCompilerSettingsStub()
            => System.Environment.MachineName.ToLower() == "powerglide" // TLK's :)
                ? Mock.Of<ICompilerSettings>(settings
                    => settings.EnableDump == false
                    && settings.DumpFolder == "C:/Projects/Data/Tests/CodeDump")
                : Mock.Of<ICompilerSettings>();

        public static IMacrosSubstitutionService DefaultMacrosSubstitutionService()
        {
            var macrosSubstitutionServiceMock = new Mock<IMacrosSubstitutionService>();
            macrosSubstitutionServiceMock.Setup(
                x => x.InlineMacros(It.IsAny<string>(), It.IsAny<IEnumerable<Macro>>()))
                .Returns((string e, IEnumerable<Macro> macros) =>
                {
                    return e;
                });

            return macrosSubstitutionServiceMock.Object;
        }

        public static RosterVector RosterVector(params int[] vectors) => Abc.Create.Entity.RosterVector(vectors);

        public static SingleQuestion SingleOptionQuestion(Guid? questionId = null, string variable = null, string enablementCondition = null, string validationExpression = null,
            Guid? linkedToQuestionId = null, Guid? cascadeFromQuestionId = null, decimal[] answerCodes = null, string title = null, bool hideIfDisabled = false, string linkedFilterExpression = null,
            Guid? linkedToRosterId = null)
        {
            return new SingleQuestion
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                StataExportCaption = variable ?? "single_option_question",
                QuestionText = title ?? "SO Question",
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                QuestionType = QuestionType.SingleOption,
                LinkedToQuestionId = linkedToQuestionId,
                LinkedToRosterId = linkedToRosterId,
                CascadeFromQuestionId = cascadeFromQuestionId,
                Answers = (answerCodes ?? new decimal[] { 1, 2, 3 }).Select(a => Create.Answer(a.ToString(), a)).ToList(),
                LinkedFilterExpression = linkedFilterExpression
            };
        }

        public static QuestionnaireDocument QuestionnaireDocumentWithOneChapter(Guid? id = null, params IComposite[] children)
            => Create.QuestionnaireDocument(id: id, children: Create.Chapter(children: children));

        public static QuestionnaireDocument QuestionnaireDocument(Guid? id = null, params IComposite[] children)
            => new QuestionnaireDocument()
            {
                PublicKey = id ?? Guid.NewGuid(),
                Children = children?.ToReadOnlyCollection() ?? new ReadOnlyCollection<IComposite>(new List<IComposite>()),
            };

        public static Group Chapter(string title = "Chapter X", 
            Guid? id = null,
            string enablementCondition = null,
            IEnumerable<IComposite> children = null)
            => Abc.Create.Entity.Group(id, title, null, enablementCondition, false, children);

        public static IQuestion Question(Guid? id = null, string variable = null, string enablementCondition = null, string validationExpression = null)
        {
            return new TextQuestion("Question X")
            {
                PublicKey = id ?? Guid.NewGuid(),
                QuestionType = QuestionType.Text,
                StataExportCaption = variable,
                ConditionExpression = enablementCondition,
                ValidationExpression = validationExpression,
            };
        }

        public static MultyOptionsQuestion MultyOptionsQuestion(Guid? id = null, 
            IEnumerable<Answer> options = null, Guid? linkedToQuestionId = null, string variable = null, 
            Guid? linkedToRosterId=null, 
            bool yesNo = false,
            string optionsFilter = null,
            string linkedFilter = null)
        {
            var multyOptionsQuestion = new MultyOptionsQuestion
            {
                QuestionType = QuestionType.MultyOption,
                PublicKey = id ?? Guid.NewGuid(),
                Answers = linkedToQuestionId.HasValue ? null : new List<Answer>(options ?? new Answer[] {}),
                LinkedToQuestionId = linkedToQuestionId,
                StataExportCaption = variable,
                LinkedToRosterId = linkedToRosterId,
                YesNoView = yesNo,
                LinkedFilterExpression = linkedFilter,
                Properties = {OptionsFilterExpression = optionsFilter}
            };
            return multyOptionsQuestion;
        }

        public static MultyOptionsQuestion MultipleOptionsQuestion(Guid? questionId = null, string enablementCondition = null,
            string validationExpression = null, bool areAnswersOrdered = false, int? maxAllowedAnswers = null, Guid? linkedToQuestionId = null, Guid? linkedToRosterId = null,
            bool isYesNo = false, bool hideIfDisabled = false, string optionsFilterExpression = null, Answer[] textAnswers = null,
            params int[] answers)
            => new MultyOptionsQuestion("Question MO")
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                StataExportCaption = "mo_question",
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                AreAnswersOrdered = areAnswersOrdered,
                MaxAllowedAnswers = maxAllowedAnswers,
                QuestionType = QuestionType.MultyOption,
                LinkedToQuestionId = linkedToQuestionId,
                LinkedToRosterId = linkedToRosterId,
                YesNoView = isYesNo,
                Answers = textAnswers?.ToList() ?? answers.Select(a => Answer(a.ToString(), a)).ToList(),
                Properties = new QuestionProperties(false, false)
                {
                    OptionsFilterExpression = optionsFilterExpression
                }
            };

        public static MultyOptionsQuestion YesNoQuestion(Guid? questionId = null, int[] answers = null, bool ordered = false,
            int? maxAnswersCount = null, string variable = null)
        {
            var yesNo = MultipleOptionsQuestion(
                isYesNo: true,
                questionId: questionId,
                answers: answers ?? new int[] { },
                areAnswersOrdered: ordered,
                maxAllowedAnswers: maxAnswersCount);
            yesNo.StataExportCaption = variable;
            return yesNo;
        }

        public static NumericQuestion NumericIntegerQuestion(Guid? id = null, 
            string variable = null,
            string enablementCondition = null, 
            string validationExpression = null,
            IEnumerable<ValidationCondition> validationConditions = null,
            string title = null)
        {
            return new NumericQuestion
            {
                QuestionType = QuestionType.Numeric,
                PublicKey = id ?? Guid.NewGuid(),
                StataExportCaption = variable,
                IsInteger = true,
                QuestionText = title,
                ConditionExpression = enablementCondition,
                ValidationExpression = validationExpression,
                ValidationConditions = validationConditions?.ToList(),
            };
        }

        public static NumericQuestion NumericIntegerQuestion(Guid id, string variable, IList<ValidationCondition> validationExpression)
        {
            return new NumericQuestion
            {
                QuestionType = QuestionType.Numeric,
                PublicKey = id,
                StataExportCaption = variable,
                IsInteger = true,
                ValidationConditions = validationExpression?? new List<ValidationCondition>()
            };
        }

        public static SingleQuestion SingleQuestion(Guid? id = null, string variable = null, string enablementCondition = null, 
            string validationExpression = null, Guid? cascadeFromQuestionId = null, List<Answer> options = null, Guid? linkedToQuestionId = null, 
            Guid? linkedToRosterId=null, string optionsFilter = null, string linkedFilter = null)
        {
            var singleQuestion = new SingleQuestion
            {
                QuestionType = QuestionType.SingleOption,
                PublicKey = id ?? Guid.NewGuid(),
                StataExportCaption = variable,
                ConditionExpression = enablementCondition,
                ValidationExpression = validationExpression,
                Answers = options ?? new List<Answer>(),
                CascadeFromQuestionId = cascadeFromQuestionId,
                LinkedToQuestionId = linkedToQuestionId,
                LinkedToRosterId = linkedToRosterId,
                LinkedFilterExpression = linkedFilter,
                Properties =
                {
                    OptionsFilterExpression = optionsFilter
                }
            };
            return singleQuestion;
        }

        public static Answer Option(string value = null, Guid? id = null, string text = null, string parentValue = null)
        {
            return new Answer
            {
                PublicKey = id ?? Guid.NewGuid(),
                AnswerText = text ?? ("Option " + value),
                AnswerValue = value ?? "1",
                ParentValue = parentValue
            };
        }

        public static NumericQuestion NumericRealQuestion(Guid? id = null, string variable = null, string enablementCondition = null, string validationExpression = null)
        {
            return new NumericQuestion
            {
                QuestionType = QuestionType.Numeric,
                PublicKey = id ?? Guid.NewGuid(),
                StataExportCaption = variable,
                IsInteger = false,
                ConditionExpression = enablementCondition,
                ValidationExpression = validationExpression
            };
        }

        public static DateTimeQuestion DateTimeQuestion(Guid id, string variable, string enablementCondition = null, string validationExpression = null)
        {
            return new DateTimeQuestion
            {
                PublicKey = id,
                QuestionType = QuestionType.DateTime,
                StataExportCaption = variable,
                ConditionExpression = enablementCondition,
                ValidationExpression = validationExpression
            };
        }

        public static StaticText StaticText(
            Guid? id = null, string enablementCondition = null, IEnumerable<ValidationCondition> validationConditions = null)
            => new StaticText(
                id ?? Guid.NewGuid(),
                "Static Text",
                enablementCondition,
                false,
                validationConditions?.ToList());

        public static Questionnaire Questionnaire(QuestionnaireDocument questionnaireDocument)
        {
            var questionnaire = new Questionnaire(
                Mock.Of<IQuestionnaireStorage>(),
                Mock.Of<IQuestionnaireAssemblyAccessor>(),
                Mock.Of<IPlainStorageAccessor<QuestionnaireBrowseItem>>(),
                Mock.Of<IPlainKeyValueStorage<QuestionnaireQuestionsInfo>>(),
                Mock.Of<IFileSystemAccessor>(),
                new InMemoryPlainStorageAccessor<TranslationInstance>());

            questionnaire.ImportFromDesigner(new ImportFromDesigner(Guid.NewGuid(), questionnaireDocument, false, "base64 string of assembly", 1, 1));

            return questionnaire;
        }

        public static Interview Interview(Guid? questionnaireId = null,
            IQuestionnaireStorage questionnaireRepository = null, IInterviewExpressionStatePrototypeProvider expressionProcessorStatePrototypeProvider = null)
        {
            var interview = new Interview(questionnaireRepository ?? Mock.Of<IQuestionnaireStorage>(),
                expressionProcessorStatePrototypeProvider ?? Mock.Of<IInterviewExpressionStatePrototypeProvider>(),
                Create.SubstitionTextFactory());

            interview.CreateInterview(
                questionnaireId ?? new Guid("B000B000B000B000B000B000B000B000"),
                1,
                new Guid("D222D222D222D222D222D222D222D222"),
                new Dictionary<Guid, AbstractAnswer>(),
                new DateTime(2012, 12, 20),
                new Guid("F111F111F111F111F111F111F111F111"));

            return interview;
        }

        public static StatefulInterview PreloadedInterview(
            PreloadedDataDto preloadedData,
            Guid? questionnaireId = null,
            IQuestionnaireStorage questionnaireRepository = null, 
            IInterviewExpressionStatePrototypeProvider expressionProcessorStatePrototypeProvider = null)
        {
            var interview = new StatefulInterview(questionnaireRepository ?? Mock.Of<IQuestionnaireStorage>(),
                expressionProcessorStatePrototypeProvider ?? Mock.Of<IInterviewExpressionStatePrototypeProvider>(),
                Create.SubstitionTextFactory());
          
            interview.CreateInterviewWithPreloadedData(new CreateInterviewWithPreloadedData(
                interviewId: Guid.NewGuid(),
                userId: Guid.NewGuid(),
                questionnaireId: questionnaireId ?? new Guid("B000B000B000B000B000B000B000B000"),
                version: 1,
                preloadedDataDto: preloadedData,
                answersTime: new DateTime(2012, 12, 20),
                supervisorId: Guid.NewGuid(),
                interviewerId: Guid.NewGuid()));

            return interview;
        }

        public static ISubstitionTextFactory SubstitionTextFactory()
        {
            return new SubstitionTextFactory(Create.SubstitutionService(), Create.VariableToUIStringService());
        }

        private static IVariableToUIStringService VariableToUIStringService()
        {
            return new VariableToUIStringService();
        }

        public static QuestionnaireIdentity QuestionnaireIdentity(Guid? questionnaireId = null, long? questionnaireVersion = null)
            => new QuestionnaireIdentity(questionnaireId ?? Guid.NewGuid(), questionnaireVersion ?? 7);

        public static StatefulInterview StatefulInterview(QuestionnaireDocument questionnaireDocument)
        {
            var questionnaireIdentity = QuestionnaireIdentity();

            var questionnaireRepository = QuestionnaireRepositoryWithOneQuestionnaire(questionnaireIdentity, questionnaireDocument);

            return StatefulInterview(
                questionnaireIdentity: questionnaireIdentity,
                questionnaireRepository: questionnaireRepository);
        }
        
        public static IQuestionnaireStorage QuestionnaireRepositoryWithOneQuestionnaire(IQuestionnaire questionnaire)
            => Stub<IQuestionnaireStorage>.Returning(questionnaire);

        public static StatefulInterview StatefulInterview(QuestionnaireIdentity questionnaireIdentity,
            IQuestionnaireStorage questionnaireRepository = null, 
            IInterviewExpressionStatePrototypeProvider expressionProcessorStatePrototypeProvider = null,
            Dictionary<Guid, AbstractAnswer> answersOnPrefilledQuestions = null)
        {
            var interview = new StatefulInterview(
                questionnaireRepository ?? Mock.Of<IQuestionnaireStorage>(),
                expressionProcessorStatePrototypeProvider ??
                Stub<IInterviewExpressionStatePrototypeProvider>.WithNotEmptyValues,
                Create.SubstitionTextFactory());

            interview.CreateInterview(
                questionnaireIdentity?.QuestionnaireId ?? new Guid("B000B000B000B000B000B000B000B000"),
                questionnaireIdentity?.Version ?? 1,
                new Guid("D222D222D222D222D222D222D222D222"),
                answersOnPrefilledQuestions ?? new Dictionary<Guid, AbstractAnswer>(),
                new DateTime(2012, 12, 20),
                new Guid("F111F111F111F111F111F111F111F111"));

            return interview;
        }

        public static Identity Identity(Guid id, decimal[] rosterVector = null)
        {
            return new Identity(id, rosterVector ?? Core.SharedKernels.DataCollection.RosterVector.Empty);
        }

        public static AddedRosterInstance AddedRosterInstance(Guid groupId, decimal[] outerRosterVector = null,
            decimal rosterInstanceId = 0, int? sortIndex = null)
        {
            return new AddedRosterInstance(groupId, outerRosterVector ?? Core.SharedKernels.DataCollection.RosterVector.Empty, rosterInstanceId, sortIndex);
        }

        public static RosterInstance RosterInstance(Guid groupId, decimal[] outerRosterVector = null, decimal rosterInstanceId = 0)
        {
            return new RosterInstance(groupId, outerRosterVector ?? Core.SharedKernels.DataCollection.RosterVector.Empty, rosterInstanceId);
        }

        public static HeaderStructureForLevel HeaderStructureForLevel(string levelName = "table name", string[] referenceNames = null, ValueVector<Guid> levelScopeVector = null)
        {
            return new HeaderStructureForLevel()
            {
                LevelScopeVector = levelScopeVector ?? new ValueVector<Guid>(),
                LevelName = levelName,
                LevelIdColumnName = "Id",
                IsTextListScope = referenceNames != null,
                ReferencedNames = referenceNames,
                HeaderItems =
                    new Dictionary<Guid, ExportedHeaderItem>
                    {
                        { Guid.NewGuid(), ExportedHeaderItem() },
                        { Guid.NewGuid(), ExportedHeaderItem(QuestionType.Numeric, new[] { "a" }) }
                    }
            };
        }

        public static ExportedHeaderItem ExportedHeaderItem(QuestionType type = QuestionType.Text, string[] columnNames = null)
        {
            return new ExportedHeaderItem() { ColumnNames = columnNames ?? new[] { "1" }, QuestionType = type };
        }

        public static QuestionnaireExportStructure QuestionnaireExportStructure(params HeaderStructureForLevel[] levels)
        {
            var header = new Dictionary<ValueVector<Guid>, HeaderStructureForLevel>();
            if (levels != null && levels.Length > 0)
            {
                header = levels.ToDictionary((i) => i.LevelScopeVector, (i) => i);
            }
            return new QuestionnaireExportStructure() { HeaderToLevelMap = header };
        }

        public static CommandService CommandService(
            IEventSourcedAggregateRootRepository repository = null,
            IPlainAggregateRootRepository plainRepository = null,
            IEventBus eventBus = null, 
            IAggregateSnapshotter snapshooter = null,
            IServiceLocator serviceLocator = null)
        {
            return new CommandService(
                repository ?? Mock.Of<IEventSourcedAggregateRootRepository>(),
                eventBus ?? Mock.Of<IEventBus>(),
                snapshooter ?? Mock.Of<IAggregateSnapshotter>(),
                serviceLocator ?? Mock.Of<IServiceLocator>(),
                plainRepository ?? Mock.Of<IPlainAggregateRootRepository>(),
                new AggregateLock());
        }


        public static CommittedEvent CommittedEvent(string origin = null, 
            Guid? eventSourceId = null,
            IEvent payload = null,
            Guid? eventIdentifier = null, 
            int eventSequence = 1)
        {
            return new CommittedEvent(
                Guid.Parse("33330000333330000003333300003333"),
                origin,
                eventIdentifier ?? Guid.Parse("44440000444440000004444400004444"),
                eventSourceId ?? Guid.Parse("55550000555550000005555500005555"),
                eventSequence,
                new DateTime(2014, 10, 22),
                0,
                payload ?? Mock.Of<IEvent>());
        }

        public static CommittedEventStream CommittedEventStream(Guid eventSourceId, IEnumerable<UncommittedEvent> events)
        {
            return new CommittedEventStream(eventSourceId,
                events
                    .Select(x => Create.CommittedEvent(payload: x.Payload,
                        eventSourceId: x.EventSourceId,
                        eventSequence: x.EventSequence)));
        }

        public static FileSystemIOAccessor FileSystemIOAccessor()
        {
            return new FileSystemIOAccessor();
        }

        public static SequentialCommandService SequentialCommandService(IEventSourcedAggregateRootRepository repository = null, ILiteEventBus eventBus = null, IAggregateSnapshotter snapshooter = null)
        {
            return new SequentialCommandService(
                repository ?? Mock.Of<IEventSourcedAggregateRootRepository>(),
                eventBus ?? Mock.Of<ILiteEventBus>(),
                snapshooter ?? Mock.Of<IAggregateSnapshotter>(), Mock.Of<IServiceLocator>(),
                Mock.Of<IPlainAggregateRootRepository>(),
                new AggregateLock());
        }

        public static Answer Answer(string answer, decimal value, decimal? parentValue = null)
        {
            return new Answer()
            {
                AnswerText = answer,
                AnswerValue = value.ToString(),
                ParentValue = parentValue.HasValue ? parentValue.ToString() : null
            };
        }

        public static FixedRosterTitle FixedTitle(decimal value, string title = null)
        {
            return new FixedRosterTitle(value, title ?? ("Roster " + value));
        }

        public static LookupTable LookupTable(string tableName)
        {
            return new LookupTable
            {
                TableName = tableName
            };
        }

        public static LookupTableContent LookupTableContent(string[] variableNames, params LookupTableRow[] rows)
        {
            return new LookupTableContent
            {
                VariableNames = variableNames,
                Rows = rows
            };
        }

        public static LookupTableRow LookupTableRow(long rowcode, decimal?[] values)
        {
            return new LookupTableRow
                   {
                       RowCode = rowcode,
                       Variables = values
            };
        }

        public static PostgresReadSideKeyValueStorage<TEntity> PostgresReadSideKeyValueStorage<TEntity>(
            ISessionProvider sessionProvider = null, PostgreConnectionSettings postgreConnectionSettings = null)
            where TEntity : class, IReadSideRepositoryEntity
        {
            return new PostgresReadSideKeyValueStorage<TEntity>(
                sessionProvider ?? Mock.Of<ISessionProvider>(),
                postgreConnectionSettings ?? new PostgreConnectionSettings(),
                Mock.Of<ILogger>(),
                new EntitySerializer<TEntity>());
        }

        public static ISessionFactory SessionFactory(string connectionString, IEnumerable<Type> painStorageEntityMapTypes)
        {
            var cfg = new Configuration();
            cfg.DataBaseIntegration(db =>
            {
                db.ConnectionString = connectionString;
                db.Dialect<PostgreSQL91Dialect>();
                db.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
            });

            cfg.AddDeserializedMapping(GetMappingsFor(painStorageEntityMapTypes), "Plain");
            var update = new SchemaUpdate(cfg);
            update.Execute(true, true);

            return cfg.BuildSessionFactory();
        }

        private static HbmMapping GetMappingsFor(IEnumerable<Type> painStorageEntityMapTypes)
        {
            var mapper = new ModelMapper();
            mapper.AddMappings(painStorageEntityMapTypes);
            mapper.BeforeMapProperty += (inspector, member, customizer) =>
            {
                var propertyInfo = (PropertyInfo)member.LocalMember;
                if (propertyInfo.PropertyType == typeof(string))
                {
                    customizer.Type(NHibernateUtil.StringClob);
                }
            };
            mapper.BeforeMapClass += (inspector, type, customizer) =>
            {
                var tableName = type.Name.Pluralize();
                customizer.Table(tableName);
            };

            return mapper.CompileMappingForAllExplicitlyAddedEntities();
        }

        public static class Command
        {
            public static AnswerYesNoQuestion AnswerYesNoQuestion(Guid questionId, RosterVector rosterVector, params AnsweredYesNoOption[] answers)
                => new AnswerYesNoQuestion(Guid.NewGuid(), Guid.NewGuid(), questionId, rosterVector, DateTime.Now, answers);

            public static AnswerYesNoQuestion AnswerYesNoQuestion(Guid? userId = null,
                Guid? questionId = null, RosterVector rosterVector = null, AnsweredYesNoOption[] answeredOptions = null,
                DateTime? answerTime = null)
                => new AnswerYesNoQuestion(
                    interviewId: Guid.NewGuid(),
                    userId: userId ?? Guid.NewGuid(),
                    questionId: questionId ?? Guid.NewGuid(),
                    rosterVector: rosterVector ?? Core.SharedKernels.DataCollection.RosterVector.Empty,
                    answerTime: answerTime ?? DateTime.UtcNow,
                    answeredOptions: answeredOptions ?? new AnsweredYesNoOption[] { });

            public static AnswerNumericIntegerQuestionCommand AnswerNumericIntegerQuestion(
                Guid? questionId = null,
                RosterVector rosterVector = null,
                int? answer = null)
                => new AnswerNumericIntegerQuestionCommand(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    questionId ?? Guid.NewGuid(),
                    rosterVector ?? Core.SharedKernels.DataCollection.RosterVector.Empty,
                    DateTime.UtcNow,
                    answer ?? 42);
        }

        public static AnsweredYesNoOption AnsweredYesNoOption(decimal optionValue, bool yes)
        {
            return new AnsweredYesNoOption(optionValue, yes);
        }


        public static AggregateRootEvent AggregateRootEvent(IEvent evnt)
        {
            var rnd = new Random();
            return new AggregateRootEvent(new CommittedEvent(Guid.NewGuid(), "origin", Guid.NewGuid(), Guid.NewGuid(),
                    rnd.Next(1, 10000000), DateTime.UtcNow, rnd.Next(1, 1000000), evnt));
        }

        public static ValidationCondition ValidationCondition(string expression = null, string message = null)
            => new ValidationCondition(
                expression,
                message ?? (expression != null ? $"condition '{expression}' is not met" : null));

        public static CumulativeReportStatusChange CumulativeReportStatusChange(Guid? questionnaireId=null, long? questionnaireVersion=null, DateTime? date = null)
        {
            return new CumulativeReportStatusChange(Guid.NewGuid().FormatGuid(), questionnaireId ?? Guid.NewGuid(),
                questionnaireVersion ?? 1, date??DateTime.Now, InterviewStatus.Completed, 1);
        }

        public static RosterVector RosterVector(params decimal[] coordinates) => new RosterVector(coordinates);

        public static DesignerEngineVersionService DesignerEngineVersionService()
            => new DesignerEngineVersionService();

        public static PostgreReadSideStorage<TEntity> PostgresReadSideRepository<TEntity>(
            ISessionProvider sessionProvider = null, string idColumnName = "Id")
            where TEntity : class, IReadSideRepositoryEntity
        {
            return new PostgreReadSideStorage<TEntity>(
                sessionProvider ?? Mock.Of<ISessionProvider>(),
                Mock.Of<ILogger>(), idColumnName);
        }

        public static Variable Variable(Guid? id=null, VariableType type=VariableType.LongInteger, string variableName="v1", string expression="2*2")
        {
            return new Variable(publicKey: id ?? Guid.NewGuid(),
                variableData: new VariableData(type: type, name: variableName, expression: expression, label: null));
        }

        public static ChangedVariable ChangedVariableValueDto(Guid? variableId=null, RosterVector vector=null, object value=null)
        {
            return new ChangedVariable(Create.Identity(variableId ?? Guid.NewGuid(), vector?? new RosterVector(new decimal[0])), value);
        }

        public static DynamicTextViewModel DynamicTextViewModel(
            ILiteEventRegistry registry = null,
            IStatefulInterviewRepository interviewRepository = null,
            IQuestionnaireStorage questionnaireRepository = null)
            => new DynamicTextViewModel(
                registry ?? Abc.Create.Service.LiteEventRegistry(),
                interviewRepository: interviewRepository,
                substitutionService: Create.SubstitutionService());

        public static AnswerNotifier AnswerNotifier(ILiteEventRegistry registry = null)
            =>new AnswerNotifier(registry ?? Abc.Create.Service.LiteEventRegistry());

        public static ISubstitutionService SubstitutionService()
            => new SubstitutionService();

        public static CategoricalOption CategoricalOption(int value, string title, int? parentValue = null)
        {
            return new CategoricalOption
            {
                Value = value,
                Title = title,
                ParentValue = parentValue
            };
        }

        public static IDictionary<Identity, IReadOnlyList<FailedValidationCondition>> FailedValidationCondition(Identity questionIdentity)
            => new Dictionary<Identity, IReadOnlyList<FailedValidationCondition>>
            {
                {
                    questionIdentity,
                    new List<FailedValidationCondition>() {new FailedValidationCondition(0)}
                }
            };

        public static IQuestionnaireStorage QuestionnaireRepositoryWithOneQuestionnaire(
            QuestionnaireIdentity questionnaireIdentity, QuestionnaireDocument questionnaireDocument)
            => Create.QuestionnaireRepositoryWithOneQuestionnaire(
                questionnaireIdentity,
                Create.PlainQuestionnaire(questionnaireDocument));

        public static IQuestionnaireStorage QuestionnaireRepositoryWithOneQuestionnaire(
            QuestionnaireIdentity questionnaireIdentity, Expression<Func<IQuestionnaire, bool>> questionnaireMoqPredicate)
            => Create.QuestionnaireRepositoryWithOneQuestionnaire(
                questionnaireIdentity,
                Mock.Of<IQuestionnaire>(questionnaireMoqPredicate));

        private static IQuestionnaireStorage QuestionnaireRepositoryWithOneQuestionnaire(
            QuestionnaireIdentity questionnaireIdentity, IQuestionnaire questionnaire)
            => Stub<IQuestionnaireStorage>.Returning(questionnaire);

        private static IQuestionnaireStorage QuestionnaireRepository(QuestionnaireDocument questionnaireDocument)
            => Mock.Of<IQuestionnaireStorage>(repository
                => repository.GetQuestionnaire(It.IsAny<QuestionnaireIdentity>(), It.IsAny<string>()) == Create.PlainQuestionnaire(questionnaireDocument)
                && repository.GetQuestionnaireDocument(It.IsAny<QuestionnaireIdentity>()) == questionnaireDocument
                && repository.GetQuestionnaireDocument(It.IsAny<Guid>(), It.IsAny<long>()) == questionnaireDocument);

        public static PlainQuestionnaire PlainQuestionnaire(QuestionnaireDocument questionnaireDocument)
            => Create.PlainQuestionnaire(document: questionnaireDocument);

        public static PlainQuestionnaire PlainQuestionnaire(QuestionnaireDocument document = null, long version = 19)
            => new PlainQuestionnaire(document, version);


        public static PreloadedDataDto PreloadedDataDto(params PreloadedLevelDto[] levels)
        {
            return new PreloadedDataDto(levels);
        }

        public static PreloadedLevelDto PreloadedLevelDto(RosterVector rosterVector, Dictionary<Guid, AbstractAnswer> answers)
        {
            return new PreloadedLevelDto(rosterVector, answers);
        }
    }
}
