﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Events.Questionnaire;
using Moq;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection.Events.Questionnaire;
using WB.Core.SharedKernels.DataCollection.Utils;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.SharedKernels.SurveyManagement.Views.Questionnaire;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.EventHandlers.QuestionnaireQuestionsInfoDenormalizerTests
{
    internal class when_template_imported_event_recived_with_version_set : QuestionnaireQuestionsInfoDenormalizerTestContext
    {
        Establish context = () =>
        {
            questionnaireQuestionsInfoWriter = new Mock<IReadSideRepositoryWriter<QuestionnaireQuestionsInfo>>();
            denormalizer = CreateQuestionnaireQuestionsInfoDenormalizer(questionnaireQuestionsInfoWriter.Object);
            evnt = CreateTemplateImportedEvent(new QuestionnaireDocument(), 2);
        };

        Because of = () =>
            denormalizer.Handle(evnt);

        It should_view_with_id_equal_to_combination_of_questionnaireid_and_version_be_stored = () =>
            questionnaireQuestionsInfoWriter.Verify(x => x.Store(Moq.It.IsAny<QuestionnaireQuestionsInfo>(), RepositoryKeysHelper.GetVersionedKey(evnt.EventSourceId, 2)));

        private static Guid questionnaireId = Guid.Parse("33332222111100000000111122223333");
        private static QuestionnaireQuestionsInfoDenormalizer denormalizer;
        private static IPublishedEvent<TemplateImported> evnt;
        private static Mock<IReadSideRepositoryWriter<QuestionnaireQuestionsInfo>> questionnaireQuestionsInfoWriter;
    }
}
