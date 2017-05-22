﻿using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using WB.Core.Infrastructure.PlainStorage;

namespace WB.Core.BoundedContexts.Headquarters.Assignments
{
    [PlainStorage]
    public class AssignmentMap : ClassMapping<Assignment>
    {
        public AssignmentMap()
        {
            Id(x => x.Id, mapper => mapper.Generator(Generators.Identity));
            Property(x => x.ResponsibleId);
            Property(x => x.Capacity);
            Property(x => x.Archived);
            Property(x => x.CreatedAtUtc);
            Property(x => x.UpdatedAtUtc);

            Component(x => x.QuestionnaireId, cmp =>
            {
                cmp.Property(x => x.QuestionnaireId);
                cmp.Property(x => x.Version, ptp => ptp.Column("QuestionnaireVersion"));
            });

            List(x => x.PrefilledData, mapper =>
            {
                mapper.Table("AssignmentsPrefilledAnswers");
                mapper.Key(k => k.Column("AssignmentId"));
                mapper.Index(i => i.Column("Position"));
                mapper.Cascade(Cascade.All|Cascade.DeleteOrphans);
            }, r => r.Component(c =>
            {
                c.Property(x => x.Answer);
                c.Property(x => x.QuestionId);
                c.Property(x => x.Assignment);
            }));
        }
    }
}