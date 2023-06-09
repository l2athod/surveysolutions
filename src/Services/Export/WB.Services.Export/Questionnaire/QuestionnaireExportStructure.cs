﻿using System;
using System.Collections.Generic;
using System.Linq;
using WB.Services.Export.Interview;

namespace WB.Services.Export.Questionnaire
{
    public class QuestionnaireExportStructure
    {
        private int? maxRosterDepthInQuestionnaire = null;

        public QuestionnaireExportStructure(string questionnaireId, Dictionary<ValueVector<Guid>, HeaderStructureForLevel>? headerMap = null)
        {
            QuestionnaireId = questionnaireId;
            this.HeaderToLevelMap = headerMap ?? new Dictionary<ValueVector<Guid>, HeaderStructureForLevel>();
        }

        public string QuestionnaireId { get; set; }

        public Dictionary<ValueVector<Guid>, HeaderStructureForLevel> HeaderToLevelMap { get; set; }

        public IEnumerable<string> GetAllParentColumnNamesForLevel(ValueVector<Guid> levelScopeVector)
        {

            if (levelScopeVector.Length != 0)
            {
                yield return ServiceColumns.InterviewId;
            }

            for (int i = levelScopeVector.Length; i > 1; i--)
            {
                var parentLevelScopeVector = ValueVector.Create(levelScopeVector.Take(levelScopeVector.Length - i + 1).ToArray());

                var parentLevel = this.HeaderToLevelMap.TryGetValue(parentLevelScopeVector, out var value) ? value : null;

                string parentLevelName = parentLevel?.LevelName ?? $"{ServiceColumns.ParentId}{i + 1}";

                yield return $"{parentLevelName}__id";
            }
        }

        public int MaxRosterDepth
        {
            get
            {
                maxRosterDepthInQuestionnaire ??= this.HeaderToLevelMap.Values.Max(x => x.LevelScopeVector.Count);

                return maxRosterDepthInQuestionnaire.Value;
            }
        }
    }
}
