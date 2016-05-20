﻿using System.Net.Http;
using Machine.Specifications;
using WB.Core.BoundedContexts.Supervisor;
using WB.Core.BoundedContexts.Supervisor.Extensions;

namespace WB.Tests.Unit.BoundedContexts.Supervisor.WebClientExtensionsTests
{
    [Subject(typeof(WebClientExtensions), "AppendAuthToken")]
    internal class when_auth_token_provided
    {
        Establish context = () =>
        {
            headquartersSettings = Create.Other.HeadquartersSettings(accessToken: "authToken");
            client = new HttpClient();
        };

        Because of = () => client.AppendAuthToken(headquartersSettings);

        It should_not_change_auth_headers_of_client = () => client.DefaultRequestHeaders.Authorization.Scheme.ShouldEqual("authToken");

        static IHeadquartersSettings headquartersSettings;
        static HttpClient client; 
    }
}