using System.Web.Http;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.SynchronizationLog;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.UI.Headquarters.Code;

namespace WB.Core.SharedKernels.SurveyManagement.Web.Api.Interviewer
{
    public class UsersControllerBase : ApiController
    {
        protected readonly IAuthorizedUser authorizedUser;
        protected readonly IUserViewFactory userViewFactory;

        public UsersControllerBase(
            IAuthorizedUser authorizedUser,
            IUserViewFactory userViewFactory)
        {
            this.authorizedUser = authorizedUser;
            this.userViewFactory = userViewFactory;
        }
        
        [WriteToSyncLog(SynchronizationLogType.GetInterviewer)]
        public virtual InterviewerApiView Current()
        {
            var user = this.userViewFactory.GetUser(new UserViewInputModel(this.authorizedUser.Id));

            return new InterviewerApiView()
            {
                Id = user.PublicKey,
                SupervisorId = user.Supervisor.Id
            };
        }
        
        [WriteToSyncLog(SynchronizationLogType.HasInterviewerDevice)]
        public virtual bool HasDevice()=> !string.IsNullOrEmpty(this.authorizedUser.DeviceId);
    }
}