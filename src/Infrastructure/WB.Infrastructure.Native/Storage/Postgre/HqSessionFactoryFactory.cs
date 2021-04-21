﻿using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Humanizer;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Npgsql;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.Modularity;
using WB.Core.Infrastructure.Services;
using WB.Infrastructure.Native.Storage.Postgre.NhExtensions;
using WB.Infrastructure.Native.Utils;
using WB.Infrastructure.Native.Workspaces;
using Configuration = NHibernate.Cfg.Configuration;

namespace WB.Infrastructure.Native.Storage.Postgre
{
    public class HqSessionFactoryFactory
    {
        private readonly UnitOfWorkConnectionSettings connectionSettings;

        public HqSessionFactoryFactory(UnitOfWorkConnectionSettings connectionSettings)
        {
            this.connectionSettings = connectionSettings;
        }
        
        public ISessionFactory SessionFactoryBinder(IModuleContext context)
        {
            var workspace = context.Resolve<IWorkspaceContextAccessor>().CurrentWorkspace();

            return sessionFactories.GetOrAdd(workspace?.Name ?? WorkspaceConstants.SchemaName,
                space => new Lazy<ISessionFactory>(() => BuildSessionFactory(workspace?.SchemaName ?? WorkspaceConstants.SchemaName))).Value;
        }

        private static readonly ConcurrentDictionary<string, Lazy<ISessionFactory>> sessionFactories
            = new ConcurrentDictionary<string, Lazy<ISessionFactory>>();

        public ISessionFactory BuildSessionFactory(string workspaceSchema)
        {
            var cfg = new Configuration();
            cfg.DataBaseIntegration(db =>
            {
                if (string.IsNullOrWhiteSpace(this.connectionSettings.ConnectionString))
                {
                    throw new Exception("Connection string to database is not configured. [ConnectionStrings] DefaultConnection value");
                }

                var connectionStringBuilder = new NpgsqlConnectionStringBuilder(this.connectionSettings.ConnectionString)
                {
                    SearchPath = workspaceSchema,
                };

                connectionStringBuilder.SetApplicationPostfix(workspaceSchema);

                var workspaceConnectionString = connectionStringBuilder.ToString();

                db.ConnectionString = workspaceConnectionString;
                db.Dialect<PostgreSQL91Dialect>();
                db.KeywordsAutoImport = Hbm2DDLKeyWords.Keywords;
            });

            var maps = this.GetWorkspaceMappings();
            var usersMaps = this.GetUsersMappings();

            cfg.AddDeserializedMapping(maps, "maps");
            cfg.AddDeserializedMapping(usersMaps, "users");
            cfg.SetProperty(NHibernate.Cfg.Environment.WrapResultSets, "true");

            // File.WriteAllText(@"D:\Temp\Mapping.xml" , Serialize(maps)); // Can be used to check mappings

            cfg.SessionFactory().GenerateStatistics();

            var sessionFactory = cfg.BuildSessionFactory();

            return sessionFactory;
        }

        private HbmMapping GetUsersMappings()
        {
            var mapper = new ModelMapper();
            var readSideMappingTypes = this.connectionSettings.ReadSideMappingAssemblies
                .SelectMany(x => x.GetExportedTypes())
                .Where(x => x.GetCustomAttribute<UsersAttribute>() != null &&
                            x.IsSubclassOfRawGeneric(typeof(ClassMapping<>)));

            mapper.BeforeMapProperty += (inspector, member, customizer) =>
            {
                var propertyInfo = (PropertyInfo)member.LocalMember;

                customizer.Column('"' + propertyInfo.Name + '"');
            };

            mapper.AddMappings(readSideMappingTypes);

            return mapper.CompileMappingForAllExplicitlyAddedEntities();
        }

        /// <summary>
        /// Generates XML string from <see cref="NHibernate"/> mappings. Used just to verify what was generated by ConfOrm to make sure everything is correct.
        /// </summary>
        protected static string Serialize(HbmMapping hbmElement)
        {
            var setting = new XmlWriterSettings { Indent = true };
            var serializer = new XmlSerializer(typeof(HbmMapping));
            using var memStream = new MemoryStream();
            using var xmlWriter = XmlWriter.Create(memStream, setting);
            serializer.Serialize(xmlWriter, hbmElement);
            memStream.Flush();
            byte[] streamContents = memStream.ToArray();

            string result = Encoding.UTF8.GetString(streamContents);
            return result;
        }

        private HbmMapping GetWorkspaceMappings()
        {
            var mapper = new ModelMapper();
            var readSideMappingTypes = this.connectionSettings.ReadSideMappingAssemblies
                .SelectMany(x => x.GetExportedTypes())
                .Where(x => x.GetCustomAttribute<UsersAttribute>() == null &&
                            x.IsSubclassOfRawGeneric(typeof(ClassMapping<>)));

            mapper.AddMappings(readSideMappingTypes);
            CustomizeMappings(mapper);

            return mapper.CompileMappingForAllExplicitlyAddedEntities();
        }

        private static void CustomizeMappings(ModelMapper mapper)
        {
            mapper.BeforeMapProperty += (inspector, member, customizer) =>
            {
                var propertyInfo = (PropertyInfo)member.LocalMember;
                if (propertyInfo.PropertyType == typeof(string))
                {
                    customizer.Type(NHibernateUtil.StringClob);
                }

                // Someone decided that its a good idea to break existing conventions for column namings for this 2 tables
                // But I don't want to rename them since portal and powershell scripts are reading them
                if (member.LocalMember.DeclaringType.Name == "DeviceSyncInfo" ||
                    member.LocalMember.DeclaringType.Name == "SyncStatistics")
                {
                    customizer.Column('"' + propertyInfo.Name + '"');
                }
            };

            mapper.BeforeMapClass += (inspector, type, customizer) =>
            {
                var tableName = type.Name.Pluralize();
                customizer.Table(tableName);
            };
        }

    }
}
