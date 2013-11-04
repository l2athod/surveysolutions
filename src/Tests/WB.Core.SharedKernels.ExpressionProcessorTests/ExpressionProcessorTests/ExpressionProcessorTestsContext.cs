﻿using WB.Core.SharedKernels.ExpressionProcessor.Services;

namespace WB.Core.SharedKernels.ExpressionProcessorTests.ExpressionProcessorTests
{
    internal class ExpressionProcessorTestsContext
    {
        protected static IExpressionProcessor CreateExpressionProcessor()
        {
            return new WB.Core.SharedKernels.ExpressionProcessor.Implementation.Services.ExpressionProcessor();
        }
    }
}