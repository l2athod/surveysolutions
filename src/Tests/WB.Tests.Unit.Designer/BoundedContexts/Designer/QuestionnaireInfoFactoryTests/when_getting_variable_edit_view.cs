using System;
using FluentAssertions;
using Main.Core.Documents;
using Moq;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.ChapterInfo;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.QuestionnaireEntities;


namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.QuestionnaireInfoFactoryTests
{
    internal class when_getting_variable_edit_view : QuestionnaireInfoFactoryTestContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            questionnaireEntityDetailsReaderMock = new Mock<IDesignerQuestionnaireStorage>();
            questionnaireView = CreateQuestionnaireDocument();
            questionnaireEntityDetailsReaderMock
                .Setup(x => x.Get(questionnaireId))
                .Returns(questionnaireView);

            factory = CreateQuestionnaireInfoFactory(questionnaireEntityDetailsReaderMock.Object);
            BecauseOf();
        }

        private void BecauseOf() =>
            result = factory.GetVariableEditView(questionnaireId, entityId);

        [NUnit.Framework.Test] public void should_return_not_null_view () =>
            result.Should().NotBeNull();

        [NUnit.Framework.Test] public void should_return_variable_with_Id_equals_variableId () =>
            result.Id.Should().Be(entityId);

        [NUnit.Framework.Test] public void should_return_variable_with_itemId_equals_formated_variableId () =>
            result.ItemId.Should().Be(entityId.FormatGuid());

        [NUnit.Framework.Test] public void should_return_variable_name_equals () =>
            result.VariableData.Name.Should().Be(GetVariable(entityId).Name);

        [NUnit.Framework.Test] public void should_return_variable_type_equals () =>
            result.VariableData.Type.Should().Be(GetVariable(entityId).Type);

        [NUnit.Framework.Test] public void should_return_variable_expression_equals () =>
            result.VariableData.Expression.Should().Be(GetVariable(entityId).Expression);

        private static IVariable GetVariable(Guid entityId)
        {
            return questionnaireView.Find<IVariable>(entityId);
        }

        private static QuestionnaireInfoFactory factory;
        private static VariableView result;
        private static QuestionnaireDocument questionnaireView;
        private static Mock<IDesignerQuestionnaireStorage> questionnaireEntityDetailsReaderMock;
        private static Guid entityId = var1Id;
    }
}
