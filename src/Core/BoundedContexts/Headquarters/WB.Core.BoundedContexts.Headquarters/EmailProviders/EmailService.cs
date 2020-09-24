﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;
using WB.Core.BoundedContexts.Headquarters.Invitations;
using WB.Core.BoundedContexts.Headquarters.ValueObjects;
using WB.Core.BoundedContexts.Headquarters.Views;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.PlainStorage;

namespace WB.Core.BoundedContexts.Headquarters.EmailProviders
{
    public class EmailService : IEmailService
    {
        private readonly IPlainKeyValueStorage<EmailProviderSettings> emailProviderSettingsStorage;
        private readonly ISerializer serializer; 

        public EmailService(IPlainKeyValueStorage<EmailProviderSettings> emailProviderSettingsStorage, 
            ISerializer serializer)
        {
            this.emailProviderSettingsStorage = emailProviderSettingsStorage;
            this.serializer = serializer;
        }

        public async Task<string> SendEmailAsync(string to, string subject, string htmlBody, string textBody, List<EmailAttachment> attachments = null)
        {
            EmailProviderSettings settings = emailProviderSettingsStorage.GetById(AppSetting.EmailProviderSettings);
            if (!IsConfigured())
                throw new Exception("Email provider was not set up properly");

            switch (settings.Provider)
            {
                case EmailProvider.Amazon:
                    return await SendEmailWithAmazon(to, subject, htmlBody, textBody, attachments, settings).ConfigureAwait(false);
                case EmailProvider.SendGrid:
                    return await SendEmailWithSendGrid(to, subject, htmlBody, textBody, attachments, settings).ConfigureAwait(false);
                default:
                    throw new Exception("Email provider wasn't set up");
            }
        }

        public bool IsConfigured()
        {
            var settings = emailProviderSettingsStorage.GetById(AppSetting.EmailProviderSettings);
            if (settings == null)
                return false;

            switch (settings.Provider)
            {
                case EmailProvider.None: return false;
                case EmailProvider.Amazon:
                    return !string.IsNullOrWhiteSpace(settings.AwsAccessKeyId) &&
                           !string.IsNullOrWhiteSpace(settings.AwsSecretAccessKey);
                case EmailProvider.SendGrid:
                    return !string.IsNullOrWhiteSpace(settings.SendGridApiKey);
                default:
                    return false;
            }
        }

        public ISenderInformation GetSenderInfo()
        {
            var settings = emailProviderSettingsStorage.GetById(AppSetting.EmailProviderSettings);
            return settings;
        }

        private async Task<string> SendEmailWithSendGrid(string to, string subject, string htmlBody, string textBody, List<EmailAttachment> attachments, ISendGridEmailSettings settings)
        {
            var client = new SendGridClient(settings.SendGridApiKey);
            var msg = new SendGridMessage
            {
                From = new EmailAddress(settings.SenderAddress),
                Subject = subject,
                PlainTextContent = textBody,
                HtmlContent = htmlBody,
            };

            if (attachments != null)
            {
                msg.Attachments = attachments.Select(a =>
                    new Attachment()
                    {
                        ContentId = a.ContentId ?? Guid.NewGuid().ToString(),
                        Disposition = a.Disposition == EmailAttachmentDisposition.Inline ? "inline" : "attachment",
                        Filename = a.Filename,
                        Content = Convert.ToBase64String(a.Content.ToArray()),
                        Type = a.ContentType,
                    }).ToList();
            }

            if(!string.IsNullOrWhiteSpace(settings.ReplyAddress))
                msg.ReplyTo = new EmailAddress(settings.ReplyAddress);

            msg.AddTo(new EmailAddress(to));
            var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.NoContent)
            {
                var headers = response.DeserializeResponseHeaders(response.Headers);
                var messageIdHeader = headers.FirstOrDefault(x => x.Key.Equals("X-Message-Id", StringComparison.InvariantCultureIgnoreCase));
                return messageIdHeader.Value;
            }

            string body = await response.Body.ReadAsStringAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(body))
            {
                var responseErrors = serializer.Deserialize<SendGridResponseErrors>(body);
                if (responseErrors != null)
                {
                    var errors = responseErrors.Errors.Select(x => $"{x.Message} For more information go to: {x.Help}").ToArray();
                    throw new EmailServiceException(to, response.StatusCode, null, errors);
                }
            }

            throw new EmailServiceException(to, response.StatusCode);
        }

        private class SendGridResponseErrors
        {
            public SendGridResponseError[] Errors { get; set; }
        }

        private class SendGridResponseError
        {
            public string Message { get; set; }
            public string Help { get; set; }
            public string Field { get; set; }
        }

        private async Task<string> SendEmailWithAmazon(string to, string subject, string htmlBody, string textBody, List<EmailAttachment> attachments, IAmazonEmailSettings settings)
        {
            var credentials = new BasicAWSCredentials(settings.AwsAccessKeyId, settings.AwsSecretAccessKey);
            using var client = new AmazonSimpleEmailServiceClient(credentials, RegionEndpoint.USEast1);
            
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(string.Empty, settings.SenderAddress));
            message.To.Add(new MailboxAddress(string.Empty, to));
            message.Subject = subject;
            
            var body = new BodyBuilder()
            {
                HtmlBody = htmlBody,
                TextBody = textBody,
            };
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    var bytes = attachment.Content.ToArray();
                    var attachmentEntity = body.Attachments.Add(attachment.Filename, bytes, ContentType.Parse(attachment.ContentType));
                    attachmentEntity.ContentDisposition = attachment.Disposition == EmailAttachmentDisposition.Inline 
                        ? new ContentDisposition(ContentDisposition.Inline)
                        : new ContentDisposition(ContentDisposition.Attachment);
                    attachmentEntity.ContentId = attachment.ContentId ?? Guid.NewGuid().ToString();
                }
            }
            
            message.Body = body.ToMessageBody();

            await using var messageStream = new MemoryStream();
            await message.WriteToAsync(messageStream);
            
            var sendRequest = new SendRawEmailRequest
            {
                RawMessage = new RawMessage(messageStream),
            };
            
            try
            {
                var response = await client.SendRawEmailAsync(sendRequest).ConfigureAwait(false);
                return response.MessageId;
            }
            catch (AggregateException ae)
            {
                throw new EmailServiceException(to, HttpStatusCode.Accepted, ae, 
                    ae.UnwrapAllInnerExceptions().Select(x => x.Message).ToArray());
            }
            catch (Exception ex)
            {
                throw new EmailServiceException(to, HttpStatusCode.Accepted, ex, ex.Message);
            }
        }
    }

    public interface IEmailService
    {
        Task<string> SendEmailAsync(string to, string subject, string htmlBody, string textBody, List<EmailAttachment> attachments = null);
        bool IsConfigured();
        ISenderInformation GetSenderInfo();
    }
}
