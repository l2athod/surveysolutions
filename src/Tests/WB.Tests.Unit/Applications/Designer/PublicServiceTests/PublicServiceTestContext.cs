﻿using System;
using Main.Core.Documents;
using Moq;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.QuestionnaireList;
using WB.Core.GenericSubdomains.Utils.Services;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.SharedKernel.Utils.Services;
using WB.Core.SharedKernels.DataCollection;
using WB.UI.Designer.WebServices;
using WB.UI.Designer.WebServices.Questionnaire;
using WB.UI.Shared.Web.Membership;

namespace WB.Tests.Unit.Applications.Designer.PublicServiceTests
{
    internal class PublicServiceTestContext
    {
        protected static PublicService CreatePublicService(IQuestionnaireExportService exportService = null,
            IStringCompressor zipUtils = null,
            IMembershipUserService userHelper = null,
            IViewFactory<QuestionnaireListInputModel, QuestionnaireListView> viewFactory = null,
            IViewFactory<QuestionnaireViewInputModel, QuestionnaireView> questionnaireViewFactory = null,
            IQuestionnaireVerifier questionnaireVerifier = null,
            IExpressionProcessorGenerator expressionProcessorGenerator = null)
        {
            return new PublicService(exportService ?? Mock.Of<IQuestionnaireExportService>(),
                zipUtils ?? Mock.Of<IStringCompressor>(),
                userHelper ?? Mock.Of<IMembershipUserService>(),
                viewFactory ?? Mock.Of<IViewFactory<QuestionnaireListInputModel, QuestionnaireListView>>(),
                questionnaireViewFactory ?? Mock.Of<IViewFactory<QuestionnaireViewInputModel, QuestionnaireView>>(),
                questionnaireVerifier ?? Mock.Of<IQuestionnaireVerifier>(),
                expressionProcessorGenerator ?? Mock.Of<IExpressionProcessorGenerator>());
        }

        protected static TemplateInfo CreateTemplateInfo(QuestionnaireVersion version)
        {
            return new TemplateInfo
            {
                Version = version,
                Source = "aaaa",
                Title = "aaaa"
            };
        }

        protected static DownloadQuestionnaireRequest CreateDownloadQuestionnaireRequest(Guid questionnaireId,
            QuestionnaireVersion supportedQuestionnaireVersion)
        {
            return new DownloadQuestionnaireRequest
            {
                SupportedQuestionnaireVersion = supportedQuestionnaireVersion,
                QuestionnaireId = questionnaireId
            };
        }

        protected static IViewFactory<QuestionnaireViewInputModel, QuestionnaireView> CreateQuestionnaireViewFactory(Guid id)
        {
            var questionnaire = new QuestionnaireDocument();
            var questionnaireView = new QuestionnaireView(questionnaire);
            var questionnaireViewFactory = Mock.Of<IViewFactory<QuestionnaireViewInputModel, QuestionnaireView>>(x => x.Load(Moq.It.IsAny<QuestionnaireViewInputModel>()) == questionnaireView);

            return questionnaireViewFactory;
        }
    }
}
