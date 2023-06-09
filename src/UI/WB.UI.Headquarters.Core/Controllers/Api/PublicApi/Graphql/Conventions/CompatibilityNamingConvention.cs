﻿using System;
using GeoJSON.Net;
using HotChocolate;
using HotChocolate.Types.Descriptors;

namespace WB.UI.Headquarters.Controllers.Api.PublicApi.Graphql
{
    //preserve old way mapping for enum values
    //should be removed with the next braking change 
    public class CompatibilityNamingConvention : DefaultNamingConventions
    {
        public override NameString GetEnumValueName(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            
            return value is GeoJSONObjectType ? value.ToString() : 
                value.ToString().ToUpperInvariant();
        }
    }
}
