using System;
using Machine.Specifications;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;

namespace WB.Tests.Unit.Infrastructure.RebuildReadSideCqrsPostgresTransactionManagerTests
{
    internal class when_beginning_query_transaction
    {
        Establish context = () =>
        {
            transactionManager = Create.Other.RebuildReadSideCqrsPostgresTransactionManager();
        };

        Because of = () =>
            exception = Catch.Exception(() =>
                transactionManager.BeginQueryTransaction());

        It should_throw_NotSupportedException = () =>
            exception.ShouldBeOfExactType<NotSupportedException>();

        private static RebuildReadSideCqrsPostgresTransactionManagerWithoutSessions transactionManager;
        private static Exception exception;
    }
}