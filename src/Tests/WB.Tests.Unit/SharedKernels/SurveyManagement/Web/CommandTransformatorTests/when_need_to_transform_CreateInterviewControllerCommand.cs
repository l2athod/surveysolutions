﻿using Machine.Specifications;
using Ncqrs.Commanding;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.SurveyManagement.Web.Code.CommandTransformation;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Web.CommandTransformatorTests
{
    internal class when_need_to_transform_CreateInterviewControllerCommand: CommandTransformatorTestsContext
    {
        Establish context = () =>
        {
            commandTransformator = CreateCommandTransformator();
        };

        Because of = () =>
            command = commandTransformator.TransformCommnadIfNeeded(command);

        It should_return_command_of_CreateInterviewCommand_type = () =>
            command.ShouldBeOfExactType<CreateInterviewCommand>();

        private static CommandTransformator commandTransformator;
        private static ICommand command = Create.Other.CreateInterviewControllerCommand();
    }
}
