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
            Property(x => x.ResumePassword);
            Property(x => x.SentOnUtc);
            Property(x => x.InvitationEmailId);
            Property(x => x.LastReminderSentOnUtc);
            Property(x => x.LastReminderEmailId);
            Property(x => x.NumberOfRemindersSent);

            ManyToOne(x => x.Assignment, mto =>
            {
                mto.Column("AssignmentId");
                mto.Cascade(Cascade.None);
                mto.Update(false);
                mto.Insert(false);
            });

            ManyToOne(x => x.Interview, mto =>
            {
                mto.Column("InterviewId");
                mto.Cascade(Cascade.None);
                mto.Update(false);
                mto.Insert(false);
            });
        }
    }
}
