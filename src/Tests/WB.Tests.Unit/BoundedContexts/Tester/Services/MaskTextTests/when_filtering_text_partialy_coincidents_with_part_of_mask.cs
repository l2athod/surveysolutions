﻿using System;
using System.Collections.Generic;
using Machine.Specifications;
using Moq;
using WB.Core.BoundedContexts.Tester.Implementation.Aggregates;
using WB.Core.BoundedContexts.Tester.Implementation.Entities;
using WB.Core.BoundedContexts.Tester.Implementation.Services;
using WB.Core.BoundedContexts.Tester.Repositories;
using WB.Core.BoundedContexts.Tester.Services;
using WB.Core.BoundedContexts.Tester.Services.MaskText;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection;
using It = Machine.Specifications.It;


namespace WB.Tests.Unit.BoundedContexts.QuestionnaireTester.Services.MaskTextTests
{
    internal class when_filtering_text_partialy_coincidents_with_part_of_mask : MaskTextTestsContext
    {
        Establish context = () =>
        {
            maskedText = CreateMaskedText("*-**");
        };

        Because of = () =>
            filterResult = maskedText.FilterOnlyMaskedChars("ss", 2);

        It should_be_accept_all_chars = () =>
            filterResult.ShouldEqual("ss");

        static string filterResult;
        static MaskedText maskedText;
    }
}