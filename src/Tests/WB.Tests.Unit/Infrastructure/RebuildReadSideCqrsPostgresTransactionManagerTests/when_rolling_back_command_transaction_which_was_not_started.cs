using System;
using Machine.Specifications;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;

namespace WB.Tests.Unit.Infrastructure.RebuildReadSideCqrsPostgresTransactionManagerTests
{
    internal class when_rolling_back_command_transaction_which_was_not_started
    {
        Establish context = () =>
        {
            transactionManager = Create.Other.RebuildReadSideCqrsPostgresTransactionManager();
        };

        Because of = () =>
            exception = Catch.Exception(() =>
                transactionManager.RollbackCommandTransaction());

        It should_throw_InvalidOperationException = () =>
            exception.ShouldBeOfExactType<InvalidOperationException>();

        private static RebuildReadSideCqrsPostgresTransactionManagerWithoutSessions transactionManager;
        private static Exception exception;
    }
}