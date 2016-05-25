﻿using System;
using System.Collections.Generic;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using NHibernate.Util;
using WB.Core.BoundedContexts.Headquarters.DataExport.Denormalizers;
using WB.Core.BoundedContexts.Headquarters.Views.DataExport;
using WB.Core.SharedKernels.DataCollection.ValueObjects;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Factories.ExportViewFactoryTests
{
    internal class when_creating_export_structure_from_questionnaire_containing_gps_question: ExportViewFactoryTestsContext
    {
        Establish context = () =>
        {
            gpsQuestionId = Guid.Parse("BBF000AAA111EE2DD2EE111AAA000AAA");

            questionnaireDocument =
                Create.Entity.QuestionnaireDocument(children: Create.Entity.GpsCoordinateQuestion(questionId: gpsQuestionId, variable:"gps", label:"gps label"));

            exportViewFactory = CreateExportViewFactory();
        };

        Because of = () =>
            gpsExportedHeaderItem = exportViewFactory.CreateQuestionnaireExportStructure(questionnaireDocument, 1).HeaderToLevelMap[new ValueVector<Guid>()].HeaderItems[gpsQuestionId];

        It should_create_header_with_5_columns_wich_corresponds_to_gps_properties = () =>
            gpsExportedHeaderItem.ColumnNames.ShouldEqual(new[] { "gps__Latitude", "gps__Longitude", "gps__Accuracy", "gps__Altitude" , "gps__Timestamp"});

        It should_create_header_with_5_columns_with_valid_labels = () =>
            gpsExportedHeaderItem.Titles.ShouldEqual(new[] { "gps label: Latitude", "gps label: Longitude", "gps label: Accuracy", "gps label: Altitude", "gps label: Timestamp" });

        private static ExportViewFactory exportViewFactory;
        private static QuestionnaireDocument questionnaireDocument;
        private static Guid gpsQuestionId;
        private static ExportedHeaderItem gpsExportedHeaderItem;
    }
}