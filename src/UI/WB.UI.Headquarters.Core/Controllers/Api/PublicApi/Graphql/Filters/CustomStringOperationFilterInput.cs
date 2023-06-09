﻿using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace WB.UI.Headquarters.Controllers.Api.PublicApi.Graphql.Filters
{
    public class CustomStringOperationFilterInput : StringOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Name("StringOperationFilterInputType");
            descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.NotEquals).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.Contains).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.NotContains).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.In).Type<ListType<StringType>>();
            descriptor.Operation(DefaultFilterOperations.NotIn).Type<ListType<StringType>>();
            descriptor.Operation(DefaultFilterOperations.StartsWith).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.NotStartsWith).Type<StringType>();
            
            //performance on PostgreSQL
            //descriptor.Operation(DefaultFilterOperations.EndsWith).Type<StringType>();
            //descriptor.Operation(DefaultFilterOperations.NotEndsWith).Type<StringType>();
        }
    }
}
