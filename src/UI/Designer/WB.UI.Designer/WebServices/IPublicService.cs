﻿using System;
using System.ServiceModel;
using WB.UI.Designer.WebServices.Questionnaire;

namespace WB.UI.Designer.WebServices
{
    /// <summary>
    ///     The PublicService interface.
    /// </summary>
    [ServiceContract]
    public interface IPublicService
    {
        #region Public Methods and Operators

        /// <summary>
        /// The download questionnaire.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <returns>
        /// The <see cref="RemoteFileInfo"/>.
        /// </returns>
        [OperationContract]
        RemoteFileInfo DownloadQuestionnaire(DownloadQuestionnaireRequest request);

        /// <summary>
        /// The dummy.
        /// </summary>
        [OperationContract]
        void Dummy();

        /// <summary>
        /// The get questionnaire list.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <returns>
        /// The <see cref="QuestionnaireListViewMessage"/>.
        /// </returns>
        [OperationContract]
        QuestionnaireListViewMessage GetQuestionnaireList(QuestionnaireListRequest request);

        #endregion
    }
}