using System;
using System.Collections.Generic;
using System.Linq;
using AppDomainToolkit;
using Machine.Specifications;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;

namespace WB.Tests.Integration.InterviewTests.OptionsFilter
{
    internal class when_answer_is_roster_title_and_removed_because_of_filtering : InterviewTestsContext
    {
        Establish context = () =>
        {
            appDomainContext = AppDomainContext.Create();
        };

        Because of = () =>
            results = Execute.InStandaloneAppDomain(appDomainContext.Domain, () =>
            {
                Setup.MockedServiceLocator();
                var triggerQuestionId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
                Guid rosterId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
                Guid rosterTitleQuestionId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
                Guid questionnaireId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");

                Guid rosterWithMultioptionsTitle = Guid.Parse("11111111111111111111111111111111");
                Guid multioptionsQuestionId = Guid.Parse("22222222222222222222222222222222");

                Guid rosterWithYesNoTitle = Guid.Parse("33333333333333333333333333333333");
                Guid yesNoQuestionId = Guid.Parse("44444444444444444444444444444444");

                var rosterTitleQuestionIdentity = Abc.Create.Identity(rosterTitleQuestionId,
                    Abc.Create.Entity.RosterVector(new[] {0}));
                var rosterIdentity = Abc.Create.Identity(rosterId, Abc.Create.Entity.RosterVector(new[] {0}));

                var questionnaire = Abc.Create.Entity.QuestionnaireDocumentWithOneChapter(questionnaireId,
                    Abc.Create.Entity.NumericIntegerQuestion(id: triggerQuestionId,
                        variable: "trigger"),
                    Abc.Create.Entity.Roster(rosterId: rosterId,
                        rosterSizeSourceType: RosterSizeSourceType.Question,
                        rosterSizeQuestionId: triggerQuestionId,
                        rosterTitleQuestionId: rosterTitleQuestionId,
                        variable: null,
                        children: new IComposite[]
                        {
                            Abc.Create.Entity.SingleQuestion(
                                id: rosterTitleQuestionId,
                                variable: "singleopt",
                                optionsFilter: "trigger == 1",
                                options: new List<Answer>
                                {
                                    Abc.Create.Entity.Option(value: "1", text: "Option 1"),
                                    Abc.Create.Entity.Option(value: "2", text: "Option 2"),
                                })
                        }),
                     Abc.Create.Entity.Roster(rosterId: rosterWithMultioptionsTitle,
                        rosterSizeSourceType: RosterSizeSourceType.Question,
                        rosterSizeQuestionId: triggerQuestionId,
                        rosterTitleQuestionId: multioptionsQuestionId,
                        children: new IComposite[]
                        {
                            Abc.Create.Entity.MultyOptionsQuestion(
                                id: multioptionsQuestionId,
                                variable: "multiopt",
                                optionsFilter: "trigger == 1",
                                options: new List<Answer>
                                {
                                    Abc.Create.Entity.Option(value: "1", text: "Option 1"),
                                    Abc.Create.Entity.Option(value: "2", text: "Option 2"),
                                })
                        }),
                      Abc.Create.Entity.Roster(rosterId: rosterWithYesNoTitle,
                        rosterSizeSourceType: RosterSizeSourceType.Question,
                        rosterSizeQuestionId: triggerQuestionId,
                        rosterTitleQuestionId: yesNoQuestionId,
                        children: new IComposite[]
                        {
                            Abc.Create.Entity.MultyOptionsQuestion(
                                id: yesNoQuestionId,
                                variable: "yesno",
                                yesNoView: true,
                                optionsFilter: "trigger == 1",
                                options: new List<Answer>
                                {
                                    Abc.Create.Entity.Option(value: "1", text: "Option 1"),
                                    Abc.Create.Entity.Option(value: "2", text: "Option 2"),
                                })
                        })
                    );

                ILatestInterviewExpressionState interviewState = GetInterviewExpressionState(questionnaire);

                var interview = SetupInterview(questionnaire, precompiledState: interviewState);

                interview.AnswerNumericIntegerQuestion(userId, triggerQuestionId, RosterVector.Empty, DateTime.Today, 1);
                interview.AnswerSingleOptionQuestion(userId, rosterTitleQuestionIdentity.Id, rosterTitleQuestionIdentity.RosterVector, DateTime.Today, 1);
                interview.AnswerMultipleOptionsQuestion(userId, multioptionsQuestionId, Abc.Create.Entity.RosterVector(new[] {0}), DateTime.Today, new [] { 1 });
                interview.AnswerYesNoQuestion(
                    new AnswerYesNoQuestion(interview.EventSourceId, userId, yesNoQuestionId, Abc.Create.Entity.RosterVector(new[] {0}), DateTime.Today, 
                    new List<AnsweredYesNoOption>
                    {
                        new AnsweredYesNoOption(1, true)
                    }));

                var result = new InvokeResults();
                using (var eventContext = new EventContext())
                {
                    interview.AnswerNumericIntegerQuestion(userId, triggerQuestionId, RosterVector.Empty, DateTime.Now, 2);

                    result.RosterTitleHasChanged = eventContext.AnyEvent<RosterInstancesTitleChanged>(x => 
                        x.ChangedInstances.Any(c => c.RosterInstance.GroupId == rosterIdentity.Id));
                    result.RosterTitleBecomesEmpty = eventContext.AnyEvent<RosterInstancesTitleChanged>(x => x.ChangedInstances.Any(c => c.RosterInstance.GroupId == rosterIdentity.Id && c.Title == null));

                    result.MultioptionRosterTriggeredTitleChanged = eventContext.AnyEvent<RosterInstancesTitleChanged>(x =>
                        x.ChangedInstances.Any(c => c.RosterInstance.GroupId == rosterWithMultioptionsTitle && string.IsNullOrEmpty(c.Title)));

                    result.YesNoRosterTriggeredRosterTitleChanged = eventContext.AnyEvent<RosterInstancesTitleChanged>(x =>
                        x.ChangedInstances.Any(c => c.RosterInstance.GroupId == rosterWithYesNoTitle && string.IsNullOrEmpty(c.Title)));
                }

                return result;
            });

        It should_raise_roster_titles_changed_for_roster = () => results.RosterTitleHasChanged.ShouldBeTrue();
        
        It should_calculate_new_roster_title_as_empty_string = () => results.RosterTitleBecomesEmpty.ShouldBeTrue();

        It should_trigger_roster_title_changed_for_roster_with_multioptions_roster_title_question =
            () => results.MultioptionRosterTriggeredTitleChanged.ShouldBeTrue();

        It should_trigger_roster_title_changed_for_roster_with_yesno_roster_title_question =
            () => results.YesNoRosterTriggeredRosterTitleChanged.ShouldBeTrue();

        static readonly Guid userId = Guid.NewGuid();

        static AppDomainContext<AssemblyTargetLoader, PathBasedAssemblyResolver> appDomainContext;
        static InvokeResults results;

        [Serializable]
        internal class InvokeResults
        {
            public bool RosterTitleHasChanged { get; set; }
            public bool RosterTitleBecomesEmpty { get; set; }
            public bool MultioptionRosterTriggeredTitleChanged { get; set; }
            public bool YesNoRosterTriggeredRosterTitleChanged { get; set; }
        }
    }
}