﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.ChapterInfo;
using WB.Core.SharedKernels.Questionnaire.Documents;
using WB.Core.SharedKernels.QuestionnaireEntities;
using WB.Infrastructure.Native.Storage.Postgre;

namespace WB.Core.BoundedContexts.Designer.Views.Questionnaire.Search
{
    public class QuestionnaireSearchStorage : IQuestionnaireSearchStorage
    {
        public const string TableName = "questionnairesearchentities";
        public const string TableNameWithSchema = "plainstore." + TableName;

        private readonly IUnitOfWork unitOfWork;

        public QuestionnaireSearchStorage(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public void AddOrUpdateEntity(Guid questionnaireId, IComposite composite)
        {
            if (!(composite is IQuestion)
                && !(composite is IVariable)
                && !(composite is IGroup)
                && !(composite is IStaticText))
            {
                throw new ArgumentException("AddOrUpdateEntity type is not supported " + composite.GetType());
            }

            var sql = $"INSERT INTO {TableNameWithSchema} (title, questionnaireid, entityid, entitytype, searchtext)" +
                      $"VALUES(@title, @questionnaireId, @entityid, @entityType, to_tsvector(@searchtext)) " +
                      $"ON CONFLICT (questionnaireid, entityid) DO UPDATE " +
                      $"SET questionnaireid = @questionnaireId," +
                      $"    title           = @title," +
                      $"    entityid        = @entityId," +
                      $"    entitytype      = @entityType," +
                      $"    searchtext      = to_tsvector(@searchText)";

            unitOfWork.Session.Connection.Execute(sql, new
            {
                title = composite.GetTitle(),
                questionnaireId = questionnaireId,
                entityId = composite.PublicKey,
                entityType = GetEntityType(composite),
                searchText = GetTextUsedForSearch(composite)
            });
        }

        private string GetEntityType(IComposite composite)
        {
            if (composite is IQuestion)
                return ChapterItemType.Question.ToString();
            if (composite is IVariable)
                return ChapterItemType.Variable.ToString();
            if (composite is IStaticText)
                return ChapterItemType.StaticText.ToString();
            if (composite is IGroup)
                return ChapterItemType.Group.ToString();

            throw new ArgumentException("Unsupported entity type: " + composite.GetType().Name);
        }

        private string GetTextUsedForSearch(IComposite composite)
        {
            var textUsedForSearch = composite.GetTitle();
            if (composite is IQuestion question)
            {
                if (question.QuestionType == QuestionType.SingleOption
                    || question.QuestionType == QuestionType.MultyOption)
                {
                    question.Answers.Aggregate(textUsedForSearch, (text, answer)  => text + Environment.NewLine + answer.AnswerText);
                }
            }
            return textUsedForSearch;
        }

        public void Remove(Guid questionnaireId, Guid entityId)
        {
            var sql = $"DELETE from {TableNameWithSchema} s " +
                      $"WHERE s.questionnaireid = @questionnaireId " +
                      $"  AND s.entityid        = @entityId";

            unitOfWork.Session.Connection.Execute(sql, new
            {
                questionnaireId = questionnaireId,
                entityId = entityId,
            });
        }

        public void RemoveAllEntities(Guid questionnaireId)
        {
            var sql = $"DELETE from {TableNameWithSchema} s " +
                      $"WHERE s.questionnaireid = @questionnaireId ";
            unitOfWork.Session.Connection.Execute(sql, new
            {
                questionnaireId = questionnaireId,
            });
        }

        public SearchResult Search(SearchInput input)
        {
            var sqlSelect = $"SELECT s.title, s.questionnaireid, s.entityid, s.entitytype, " +
                      $"       li.folderid, li.title as questionnairetitle, f.title as foldername" +
                      $" FROM {TableNameWithSchema} s " +
                      $"    INNER JOIN plainstore.questionnairelistviewitems li ON s.questionnaireid = li.publicid" +
                      $"     LEFT JOIN plainstore.questionnairelistviewfolders f ON f.id = li.folderid" +
                      $" WHERE (@query IS NULL OR s.searchtext @@ phraseto_tsquery(@query))" +
                      $"   AND (@folderid IS NULL OR li.folderid = @folderid OR f.path like @folderpathquery) " +
                      $" ORDER BY @order ASC" +
                      $" LIMIT @pageSize" +
                      $" OFFSET @offset ";

            var searchResultEntities = unitOfWork.Session.Connection.Query<SearchResultEntity>(sqlSelect, new
            {
                query = input.Query,
                folderid = input.FolderId,
                folderpathquery = "%" +input.FolderId + "%",
                pageSize = input.PageSize,
                order = input.OrderBy ?? "title",
                offset = input.PageIndex * input.PageSize
            }).ToList();

            var sqlCount = $"SELECT COUNT(s.entityid) " +
                      $" FROM {TableNameWithSchema} s " +
                      $"    INNER JOIN plainstore.questionnairelistviewitems li ON s.questionnaireid = li.publicid" +
                      $"     LEFT JOIN plainstore.questionnairelistviewfolders f ON f.id = li.folderid" +
                      $" WHERE (@query IS NULL OR s.searchtext @@ phraseto_tsquery(@query))" +
                      $"   AND (@folderid IS NULL OR li.folderid = @folderid OR f.path like @folderpathquery) ";
            var count = unitOfWork.Session.Connection.ExecuteScalar<int>(sqlCount, new
            {
                query = input.Query,
                folderid = input.FolderId,
                folderpathquery = "%" + input.FolderId + "%",
            });

            var searchResult = new SearchResult();
            searchResult.Items = searchResultEntities;
            searchResult.TotalCount = count;
            return searchResult;
        }
    }
}
