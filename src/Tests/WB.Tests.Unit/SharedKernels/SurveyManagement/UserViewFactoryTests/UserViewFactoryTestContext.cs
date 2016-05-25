﻿using Machine.Specifications;
using Moq;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection.Views;
using It = Machine.Specifications.It;
using it = Moq.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.UserViewFactoryTests
{
    [Subject(typeof(UserViewFactory))]
    class UserViewFactoryTestContext
    {
        protected static UserViewFactory CreateUserViewFactory(IPlainStorageAccessor<UserDocument> users)
        {
            return new UserViewFactory(users ?? Mock.Of<IPlainStorageAccessor<UserDocument>>());
        }
    }
}
