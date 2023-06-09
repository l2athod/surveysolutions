using WB.Core.BoundedContexts.Headquarters.Views.Reposts.InputModels;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Views;

namespace WB.Core.BoundedContexts.Headquarters.Views.Reposts
{
    public interface ITeamsAndStatusesReport : IReport<TeamsAndStatusesInputModel>
    {
        TeamsAndStatusesReportView GetBySupervisors(TeamsAndStatusesByHqInputModel input);
        TeamsAndStatusesReportView GetBySupervisorAndDependentInterviewers(TeamsAndStatusesInputModel input);
    }
}