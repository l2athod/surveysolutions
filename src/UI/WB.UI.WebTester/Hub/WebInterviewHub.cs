﻿using Microsoft.AspNet.SignalR.Hubs;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Enumerator.Native.WebInterview;
using WB.Enumerator.Native.WebInterview.Models;
using WB.UI.WebTester.Services;

namespace WB.UI.WebTester.Hub
{
    [HubName(@"interview")]
    public class WebInterviewHub : WebInterview
    {
        private readonly IWebInterviewNotificationService webInterviewNotificationService;
        private readonly IEvictionObserver evictionNotify;

        public WebInterviewHub(IStatefulInterviewRepository statefulInterviewRepository, 
            ICommandService commandService, 
            IQuestionnaireStorage questionnaireRepository, 
            IWebInterviewNotificationService webInterviewNotificationService, 
            IWebInterviewInterviewEntityFactory interviewEntityFactory,
            IEvictionObserver evictionNotify) : 
            base(statefulInterviewRepository, commandService, questionnaireRepository, webInterviewNotificationService, interviewEntityFactory)
        {
            this.webInterviewNotificationService = webInterviewNotificationService;
            this.evictionNotify = evictionNotify;
        }

        public override void CompleteInterview(CompleteInterviewRequest completeInterviewRequest)
        {
            evictionNotify.Evict(GetCallerInterview().Id);
            webInterviewNotificationService.ShutDownInterview(base.GetCallerInterview().Id);
        }
    }
}