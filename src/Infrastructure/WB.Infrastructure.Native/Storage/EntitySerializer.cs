﻿using System.Collections.Generic;
using Newtonsoft.Json;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Utils;

namespace WB.Infrastructure.Native.Storage
{
    public class EntitySerializer<TEntity> : IEntitySerializer<TEntity> where TEntity: class
    {
        public string Serialize(TEntity entity)
        {
            var serializedValue = JsonConvert.SerializeObject(entity, Formatting.None, BackwardCompatibleJsonSerializerSettings);
            return serializedValue;
        }

        public TEntity Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<TEntity>(json, BackwardCompatibleJsonSerializerSettings);
        }
        
        private static readonly JsonSerializerSettings BackwardCompatibleJsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            FloatParseHandling = FloatParseHandling.Decimal,
            MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
            Converters = new List<JsonConverter> { new IdentityJsonConverter(), new RosterVectorConverter() },
            SerializationBinder = new OldToNewAssemblyRedirectSerializationBinder()
        };
    }
}
