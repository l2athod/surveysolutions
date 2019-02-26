﻿using System.Collections.Generic;
using WB.Core.BoundedContexts.Headquarters.Invitations;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;

namespace WB.Core.BoundedContexts.Headquarters.WebInterview
{
    public class WebInterviewConfig
    {
        public WebInterviewConfig()
        {
            this.CustomMessages = new Dictionary<WebInterviewUserMessages, string>();
            this.EmailTemplates = new Dictionary<EmailTextTemplateType, EmailTextTemplate>();
        }

        public QuestionnaireIdentity QuestionnaireId { get; set; }
        public bool Started { get; set; }
        public bool UseCaptcha { get; set; }
        public Dictionary<WebInterviewUserMessages, string> CustomMessages { get; set; }

        public static Dictionary<WebInterviewUserMessages, string> DefaultMessages => 
            new Dictionary<WebInterviewUserMessages, string> 
            {
                { WebInterviewUserMessages.FinishInterview, Enumerator.Native.Resources.WebInterview.FinishInterviewText },
                { WebInterviewUserMessages.Invitation,      Enumerator.Native.Resources.WebInterview.InvitationText },
                { WebInterviewUserMessages.ResumeInvitation,Enumerator.Native.Resources.WebInterview.Resume_InvitationText },
                { WebInterviewUserMessages.ResumeWelcome,   Enumerator.Native.Resources.WebInterview.Resume_WelcomeText },
                { WebInterviewUserMessages.SurveyName,      Enumerator.Native.Resources.WebInterview.SurveyFormatText },
                { WebInterviewUserMessages.WebSurveyHeader, Enumerator.Native.Resources.WebInterview.WebSurvey },
                { WebInterviewUserMessages.WelcomeText,     Enumerator.Native.Resources.WebInterview.WelcomeText }
            };

        public Dictionary<EmailTextTemplateType, EmailTextTemplate> EmailTemplates { get; set; }

        public static Dictionary<EmailTextTemplateType, EmailTextTemplate> DefaultEmailTemplates =>
        new Dictionary<EmailTextTemplateType, EmailTextTemplate>()
        {
            { EmailTextTemplateType.InvitationTemplate, new EmailTextTemplate(EmailTemplateTexts.InvitationTemplate.Subject, EmailTemplateTexts.InvitationTemplate.Message) },
            { EmailTextTemplateType.Reminder_NoResponse, new EmailTextTemplate(EmailTemplateTexts.Reminder_NoResponse.Subject, EmailTemplateTexts.Reminder_NoResponse.Message) },
            { EmailTextTemplateType.Reminder_PartialResponse, new EmailTextTemplate(EmailTemplateTexts.Reminder_PartialResponse.Subject, EmailTemplateTexts.Reminder_PartialResponse.Message) },
            { EmailTextTemplateType.RejectEmail, new EmailTextTemplate(EmailTemplateTexts.RejectEmail.Subject, EmailTemplateTexts.RejectEmail.Message) }
        };

        public int? ReminderAfterDaysIfNoResponse { get; set; } = 3;
        public int? ReminderAfterDaysIfPartialResponse { get; set; } = 3;

        public WebInterviewEmailTemplate GetEmailTemplate(EmailTextTemplateType type)
        {
            var template = EmailTemplates.ContainsKey(type)
                ? EmailTemplates[type]
                : DefaultEmailTemplates[type];

            return new WebInterviewEmailTemplate(template.Subject, template.Message);
        }
    }

    public class EmailTextTemplate
    {
        public EmailTextTemplate() { }

        public EmailTextTemplate(string subject, string message)
        {
            Subject = subject;
            Message = message;
        }

        public string Subject { get; set; }
        public string Message { get; set; }
    }

    public enum EmailTextTemplateType
    {
        InvitationTemplate,
        Reminder_NoResponse,
        Reminder_PartialResponse,
        RejectEmail,
    }

    public class EmailTemplateTexts
    {
        public class InvitationTemplate
        {
            public static string Subject => "Invitation to take part in %SURVEYNAME%";
            public static string Message => @"Welcome to %SURVEYNAME%!

To take the survey click on the following link: %SURVEYLINK% and enter your password: %PASSWORD%
 
Thank you for your cooperation!";
        }

        public class Reminder_NoResponse
        {
            public static string Subject => "Reminder, don’t forget to take part in %SURVEYNAME%";
            public static string Message => @"You are receiving this reminder because you haven’t started responding to %SURVEYNAME%!
 
To take the survey click on the following link: %SURVEYLINK% and enter your password: %PASSWORD%
 
Thank you for your cooperation!";
        }

        public class Reminder_PartialResponse
        {
            public static string Subject => "Reminder, please finish your response to %SURVEYNAME%";
            public static string Message => @"You are receiving this reminder because you have started responding to %SURVEYNAME%, but haven’t completed the process.
 
To continue the survey click on the following link: %SURVEYLINK% and enter your password: %PASSWORD%.
 
Please answer all applicable questions and click the ‘COMPLETE’ button to submit your responses.
 
Thank you for your cooperation!";
        }

        public class RejectEmail
        {
            public static string Subject => "Your action is required in %SURVEYNAME%";
            public static string Message => @"Thank you for taking part in %SURVEYNAME%!
 
While processing your response our staff has found some issues, which you are hereby asked to review.
 
To continue the survey click on the following link: %SURVEYLINK% and enter your password: %PASSWORD%.
 
We would appreciate if you try addressing all issues marked in your response and click the ‘COMPLETE’ button to submit your responses.
 
Thank you for your cooperation!";
        }

    }
}
