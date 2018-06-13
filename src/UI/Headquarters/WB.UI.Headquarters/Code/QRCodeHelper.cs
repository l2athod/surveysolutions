﻿using System;
using WB.Core.GenericSubdomains.Portable.Implementation;
using WB.UI.Shared.Web.Configuration;

namespace WB.UI.Headquarters.Code
{
    public class QRCodeHelper : IQRCodeHelper
    {
        private readonly IConfigurationManager configurationManager;

        public QRCodeHelper(IConfigurationManager configurationManager)
        {
            this.configurationManager = configurationManager;
        }

        public string BaseUrl
        {
            get { return configurationManager.AppSettings["BaseUrl"]; }
            set { throw new NotImplementedException(); }
        }

        public string GetBaseUrl() => BaseUrl;

        public string GetFullUrl(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
                return string.Empty;

            var fullUrl = new Url(BaseUrl, relativeUrl, null);

            return fullUrl.ToString();
        }

        public string GetQRCodeAsBase64StringSrc(string content, int height = 250, int width = 250, int margin = 0)
        {
            return $"data:image/png;base64,{QRCodeBuilder.GetQRCodeAsBase64String(content, height, width)}";
        }



        public bool SupportQRCodeGeneration()
        {
            return !string.IsNullOrWhiteSpace(BaseUrl);
        }
    }

    public interface IQRCodeHelper
    {
        string GetQRCodeAsBase64StringSrc(string relativeUrl, int height, int width, int margin = 0);
        bool SupportQRCodeGeneration();

        string GetFullUrl(string relativeUrl);

        string GetBaseUrl();
    }
}
