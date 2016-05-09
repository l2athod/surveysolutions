﻿using System;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using WB.Core.BoundedContexts.Designer.Services;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.DesignerEngineVersionServiceTests
{
    internal class when_questionnaire_contains_a_variable
    {
        Establish context = () =>
        {
            Guid staticTextId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            questionnaire = Create.QuestionnaireDocument(children: new IComposite[]
            {
                Create.Variable()
            }
                );
            designerEngineVersionService = Create.DesignerEngineVersionService();
        };

        Because of = () => calculatedVersion = designerEngineVersionService.GetQuestionnaireContentVersion(questionnaire);

        It should_return_version_14 = () => calculatedVersion.ShouldEqual(new Version(15, 0, 0));

        static QuestionnaireDocument questionnaire;
        static IDesignerEngineVersionService designerEngineVersionService;
        static Version calculatedVersion;
    }
}