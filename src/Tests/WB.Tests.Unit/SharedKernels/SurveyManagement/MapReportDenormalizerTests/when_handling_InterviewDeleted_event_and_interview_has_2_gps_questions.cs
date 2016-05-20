using System;
using Machine.Specifications;
using Main.Core.Entities.Composite;
using Moq;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Views.Questionnaire;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.MapReportDenormalizerTests
{
    internal class when_handling_InterviewDeleted_event_and_interview_has_2_gps_questions
    {
        Establish context = () =>
        {
            @event = Create.Event.Published.InterviewDeleted(interviewId: interviewId);

            var questionnaireDocument = Create.Other.QuestionnaireDocumentWithOneChapter(new IComposite[]
            {
                Create.Other.GpsCoordinateQuestion(variable: gpsVariable1),
                Create.Other.GpsCoordinateQuestion(variable: gpsVariable2),
            });

            IReadSideKeyValueStorage<InterviewReferences> interviewReferencesStorage = Setup.ReadSideKeyValueStorageWithSameEntityForAnyGet(
                Create.Other.InterviewReferences());

            denormalizer = Create.Other.MapReportDenormalizer(
                mapReportPointStorage: mapReportPointStorageMock.Object,
                interviewReferencesStorage: interviewReferencesStorage,
                questionnaireDocument: questionnaireDocument);
        };

        Because of = () =>
            denormalizer.Handle(@event);

        It should_delete_first_gps_question_map_report_point_for_deleted_interview = () =>
            mapReportPointStorageMock.Verify(storage => storage.Remove($"{interviewId}-{gpsVariable1}-{RosterVector.Empty}"));

        It should_delete_second_gps_question_map_report_point_for_deleted_interview = () =>
            mapReportPointStorageMock.Verify(storage => storage.Remove($"{interviewId}-{gpsVariable2}-{RosterVector.Empty}"));

        private static MapReportDenormalizer denormalizer;
        private static IPublishedEvent<InterviewDeleted> @event;
        private static Mock<IReadSideRepositoryWriter<MapReportPoint>> mapReportPointStorageMock = new Mock<IReadSideRepositoryWriter<MapReportPoint>>();
        private static Guid interviewId = Guid.Parse("11111111111111111111111111111111");
        private static string gpsVariable1 = "gps1";
        private static string gpsVariable2 = "gps2";
    }
}