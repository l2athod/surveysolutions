﻿using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using WB.Core.Infrastructure.PlainStorage;

namespace WB.Core.BoundedContexts.Headquarters.Invitations
{
    [PlainStorage]
    public class InvitationMap : ClassMapping<Invitation>
    {
        public InvitationMap()
        {
            Id(x => x.Id, mapper => mapper.Generator(Generators.Identity));
            DynamicUpdate(true);
            Property(x => x.AssignmentId);
            Property(x => x.InterviewId);
            Property(x => x.Token);
            Property(x => x.AccessToken);
            Property(x => x.SentOnUtc);
            Property(x => x.EmailId);

            ManyToOne(x => x.Assignment, mto =>
            {
                mto.Column("AssignmentId");
                mto.Cascade(Cascade.None);
                mto.Update(false);
                mto.Insert(false);
            });
        }
    }
}
