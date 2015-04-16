﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Microsoft.Practices.ServiceLocation;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration.Model;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration.Templates;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration.Versions;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.GenericSubdomains.Utils;
using WB.Core.GenericSubdomains.Utils.Implementation;
using WB.Core.SharedKernels.DataCollection;

namespace WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration
{
    public class CodeGenerator : ICodeGenerator
    {
        public CodeGenerator(IQuestionnaireVersionProvider questionnaireVersionProvider)
        {
            this.questionnaireVersionProvider = questionnaireVersionProvider;
        }

        private const string InterviewExpressionStatePrefix = "InterviewExpressionState";
        private readonly IQuestionnaireVersionProvider questionnaireVersionProvider;
        private IExpressionProcessor ExpressionProcessor
        {
            get { return ServiceLocator.Current.GetInstance<IExpressionProcessor>(); }
        }

        public string Generate(QuestionnaireDocument questionnaire)
        {
            QuestionnaireExecutorTemplateModel questionnaireTemplateStructure =
                CreateQuestionnaireExecutorTemplateModel(questionnaire, true);
            var template = new InterviewExpressionStateTemplate(questionnaireTemplateStructure, GetCodeVersion(questionnaireVersionProvider.GetCurrentEngineVersion()));

            return template.TransformText();
        }

        public Dictionary<string, string> GenerateEvaluator(QuestionnaireDocument questionnaire)
        {
            return GenerateEvaluatorForVersion(questionnaire, questionnaireVersionProvider.GetCurrentEngineVersion());
        }

        public Dictionary<string, string> GenerateEvaluatorForVersion(QuestionnaireDocument questionnaire, QuestionnaireVersion version)
        {
            var generatedClasses = new Dictionary<string, string>();

            QuestionnaireExecutorTemplateModel questionnaireTemplateStructure =
                CreateQuestionnaireExecutorTemplateModel(questionnaire, false);
            var template = new InterviewExpressionStateTemplate(questionnaireTemplateStructure, GetCodeVersion(version));

            generatedClasses.Add(new ExpressionLocation
            {
                ItemType = ExpressionLocationItemType.Questionnaire,
                ExpressionType = ExpressionLocationType.General,
                Id = questionnaire.PublicKey
            }.ToString(), template.TransformText());

            //generating partial classes
            GenerateQuestionnaireLevelExpressionClasses(questionnaireTemplateStructure, generatedClasses);
            GenerateRostersPartialClasses(questionnaireTemplateStructure, generatedClasses);

            return generatedClasses;
        }

        private IVersionParameters GetCodeVersion(QuestionnaireVersion version)
        {
            if (version.Major < 5)
                throw new VersionNotFoundException(string.Format("version '{0}' is not found", version));
            if (version.Major == 5)
                return new V1Parameters();
            return new V2Parameters();
        }

        private static void GenerateRostersPartialClasses(QuestionnaireExecutorTemplateModel questionnaireTemplateStructure,
            Dictionary<string, string> generatedClasses)
        {
            foreach (var groupedRosters in questionnaireTemplateStructure.RostersGroupedByScope)
            {
                foreach (QuestionTemplateModel questionTemplateModel in groupedRosters.Value.SelectMany(roster => roster.Questions))
                {
                    if (!string.IsNullOrWhiteSpace(questionTemplateModel.Conditions))
                    {
                        var methodTemplate = new ExpressionMethodTemplate(new ExpressionMethodModel
                        {
                            ExpressionString = questionTemplateModel.Conditions,
                            GeneratedClassName = groupedRosters.Key,
                            GeneratedMethodName = questionTemplateModel.GeneratedConditionsMethodName
                        });

                        generatedClasses.Add(
                            new ExpressionLocation
                            {
                                ItemType = ExpressionLocationItemType.Question,
                                ExpressionType = ExpressionLocationType.Condition,
                                Id = questionTemplateModel.Id
                            }.ToString(), methodTemplate.TransformText());
                    }

                    if (!string.IsNullOrWhiteSpace(questionTemplateModel.Validations))
                    {
                        var methodTemplate = new ExpressionMethodTemplate(new ExpressionMethodModel
                        {
                            ExpressionString = questionTemplateModel.Validations,
                            GeneratedClassName = groupedRosters.Key,
                            GeneratedMethodName = questionTemplateModel.GeneratedValidationsMethodName
                        });

                        generatedClasses.Add(new ExpressionLocation
                        {
                            ItemType = ExpressionLocationItemType.Question,
                            ExpressionType = ExpressionLocationType.Validation,
                            Id = questionTemplateModel.Id
                        }.ToString(), methodTemplate.TransformText());
                    }
                }

                foreach (GroupTemplateModel groupTemplateModel in groupedRosters.Value.SelectMany(roster => roster.Groups))
                {
                    if (!string.IsNullOrWhiteSpace(groupTemplateModel.Conditions))
                    {
                        var methodTemplate = new ExpressionMethodTemplate(new ExpressionMethodModel
                        {
                            ExpressionString = groupTemplateModel.Conditions,
                            GeneratedClassName = groupedRosters.Key,
                            GeneratedMethodName = groupTemplateModel.GeneratedConditionsMethodName
                        });

                        generatedClasses.Add(
                            new ExpressionLocation
                            {
                                ItemType = ExpressionLocationItemType.Group,
                                ExpressionType = ExpressionLocationType.Condition,
                                Id = groupTemplateModel.Id
                            }.ToString(), methodTemplate.TransformText());
                    }
                }

                foreach (RosterTemplateModel rosterTemplateModel in groupedRosters.Value)
                {
                    if (!string.IsNullOrWhiteSpace(rosterTemplateModel.Conditions))
                    {
                        var methodTemplate = new ExpressionMethodTemplate(new ExpressionMethodModel
                        {
                            ExpressionString = rosterTemplateModel.Conditions,
                            GeneratedClassName = groupedRosters.Key,
                            GeneratedMethodName = rosterTemplateModel.GeneratedConditionsMethodName
                        });

                        generatedClasses.Add(
                            new ExpressionLocation
                            {
                                ItemType = ExpressionLocationItemType.Roster,
                                ExpressionType = ExpressionLocationType.Condition,
                                Id = rosterTemplateModel.Id
                            }.ToString(), methodTemplate.TransformText());
                    }
                }
            }
        }

        private static void GenerateQuestionnaireLevelExpressionClasses(
            QuestionnaireExecutorTemplateModel questionnaireTemplateStructure, Dictionary<string, string> generatedClasses)
        {
            foreach (QuestionTemplateModel questionTemplateModel in questionnaireTemplateStructure.QuestionnaireLevelModel.Questions)
            {
                if (!string.IsNullOrWhiteSpace(questionTemplateModel.Conditions))
                {
                    var methodTemplate = new ExpressionMethodTemplate(new ExpressionMethodModel
                    {
                        ExpressionString = questionTemplateModel.Conditions,
                        GeneratedClassName = questionnaireTemplateStructure.QuestionnaireLevelModel.GeneratedTypeName,
                        GeneratedMethodName = questionTemplateModel.GeneratedConditionsMethodName
                    });

                    generatedClasses.Add(
                        new ExpressionLocation
                        {
                            ItemType = ExpressionLocationItemType.Question,
                            ExpressionType = ExpressionLocationType.Condition,
                            Id = questionTemplateModel.Id
                        }.ToString(), methodTemplate.TransformText());
                }

                if (!string.IsNullOrWhiteSpace(questionTemplateModel.Validations))
                {
                    var methodTemplate = new ExpressionMethodTemplate(new ExpressionMethodModel
                    {
                        ExpressionString = questionTemplateModel.Validations,
                        GeneratedClassName = questionnaireTemplateStructure.QuestionnaireLevelModel.GeneratedTypeName,
                        GeneratedMethodName = questionTemplateModel.GeneratedValidationsMethodName
                    });

                    generatedClasses.Add(
                        new ExpressionLocation
                        {
                            ItemType = ExpressionLocationItemType.Question,
                            ExpressionType = ExpressionLocationType.Validation,
                            Id = questionTemplateModel.Id
                        }.ToString(), methodTemplate.TransformText());
                }
            }

            foreach (GroupTemplateModel groupTemplateModel in questionnaireTemplateStructure.QuestionnaireLevelModel.Groups)
            {
                if (!string.IsNullOrWhiteSpace(groupTemplateModel.Conditions))
                {
                    var methodTemplate = new ExpressionMethodTemplate(new ExpressionMethodModel
                    {
                        ExpressionString = groupTemplateModel.Conditions,
                        GeneratedClassName = questionnaireTemplateStructure.QuestionnaireLevelModel.GeneratedTypeName,
                        GeneratedMethodName = groupTemplateModel.GeneratedConditionsMethodName
                    });

                    generatedClasses.Add(
                        new ExpressionLocation
                        {
                            ItemType = ExpressionLocationItemType.Group,
                            ExpressionType = ExpressionLocationType.Condition,
                            Id = groupTemplateModel.Id
                        }.ToString(), methodTemplate.TransformText());
                }
            }
        }

        public QuestionnaireExecutorTemplateModel CreateQuestionnaireExecutorTemplateModel(
            QuestionnaireDocument questionnaire, bool generateExpressionMethods)
        {
            var template = new QuestionnaireExecutorTemplateModel();
            template.GenerateEmbeddedExpressionMethods = generateExpressionMethods;
            var questionnaireLevelModel = new QuestionnaireLevelTemplateModel(template, generateExpressionMethods);
            string generatedClassName = string.Format("{0}_{1}", InterviewExpressionStatePrefix,
                Guid.NewGuid().FormatGuid());

            Dictionary<string, string> generatedScopesTypeNames;
            List<QuestionTemplateModel> allQuestions;
            List<GroupTemplateModel> allGroups;
            List<RosterTemplateModel> allRosters;

            BuildStructures(questionnaire, questionnaireLevelModel, out generatedScopesTypeNames, out allQuestions,
                out allGroups, out allRosters);
            Dictionary<string, List<RosterTemplateModel>> rostersGroupedByScope =
                allRosters.GroupBy(r => r.GeneratedTypeName).ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<Guid>> structuralDependencies = questionnaire
                .GetAllGroups()
                .ToDictionary(group => @group.PublicKey, group => @group.Children.Select(x => x.PublicKey).ToList());

            Dictionary<string, Guid> variableNames = allQuestions.ToDictionary(q => q.VariableName, q => q.Id);
            foreach (RosterTemplateModel roster in allRosters)
            {
                if (!variableNames.ContainsKey(roster.VariableName))
                {
                    variableNames.Add(roster.VariableName, questionnaire.PublicKey);
                }
            }

            Dictionary<Guid, List<Guid>> conditionalDependencies = BuildConditionalDependencies(questionnaire,
                variableNames);

            var mergedDependencies = new Dictionary<Guid, List<Guid>>();

            var allIdsInvolvedInExpressions = structuralDependencies.Select(x => x.Key)
                .Union(structuralDependencies.SelectMany(x => x.Value))
                .Union(conditionalDependencies.Select(x => x.Key))
                .Union(conditionalDependencies.SelectMany(x => x.Value))
                .Distinct();

            allIdsInvolvedInExpressions.ForEach(x => mergedDependencies.Add(x, new List<Guid>()));

            structuralDependencies.ForEach(x => mergedDependencies[x.Key].AddRange(x.Value));

            foreach (var conditionalDependency in conditionalDependencies)
            {
                foreach (var dependency in conditionalDependency.Value)
                {
                    if (mergedDependencies.ContainsKey(dependency) && !mergedDependencies[dependency].Contains(conditionalDependency.Key))
                    {
                        mergedDependencies[dependency].Add(conditionalDependency.Key);
                    }
                    if (!mergedDependencies.ContainsKey(dependency))
                    {
                        mergedDependencies.Add(dependency, new List<Guid> { conditionalDependency.Key });
                    }
                }
            }

            var sorter = new TopologicalSorter<Guid>();
            IEnumerable<Guid> listOfOrderedContitions = sorter.Sort(mergedDependencies.ToDictionary(x => x.Key, x => x.Value.ToArray()));

            template.Id = questionnaire.PublicKey;
            template.AllQuestions = allQuestions;
            template.AllGroups = allGroups;
            template.AllRosters = allRosters;
            template.GeneratedClassName = generatedClassName;
            template.GeneratedScopesTypeNames = generatedScopesTypeNames;
            template.RostersGroupedByScope = rostersGroupedByScope;
            template.ConditionalDependencies = conditionalDependencies;
            template.StructuralDependencies = structuralDependencies;
            template.ConditionsPlayOrder = listOfOrderedContitions.ToList();
            template.QuestionnaireLevelModel = questionnaireLevelModel;
            template.VariableNames = variableNames;
            return template;
        }

        private void BuildStructures(QuestionnaireDocument questionnaireDoc,
            QuestionnaireLevelTemplateModel questionnaireLevelModel,
            out Dictionary<string, string> generatedScopesTypeNames,
            out List<QuestionTemplateModel> allQuestions, out List<GroupTemplateModel> allGroups,
            out List<RosterTemplateModel> allRosters)
        {
            generatedScopesTypeNames = new Dictionary<string, string>();
            allQuestions = new List<QuestionTemplateModel>();
            allGroups = new List<GroupTemplateModel>();
            allRosters = new List<RosterTemplateModel>();
            
            var rostersToProcess = new Queue<Tuple<IGroup, RosterScopeBaseModel>>();
            rostersToProcess.Enqueue(new Tuple<IGroup, RosterScopeBaseModel>(questionnaireDoc, questionnaireLevelModel));

            while (rostersToProcess.Count != 0)
            {
                Tuple<IGroup, RosterScopeBaseModel> rosterScope = rostersToProcess.Dequeue();
                RosterScopeBaseModel currentScope = rosterScope.Item2;

                var childrenOfCurrentRoster = new Queue<IComposite>();

                foreach (IComposite childGroup in rosterScope.Item1.Children)
                {
                    childrenOfCurrentRoster.Enqueue(childGroup);
                }

                while (childrenOfCurrentRoster.Count != 0)
                {
                    IComposite child = childrenOfCurrentRoster.Dequeue();

                    var childAsIQuestion = child as IQuestion;
                    if (childAsIQuestion != null)
                    {
                        string varName = !String.IsNullOrEmpty(childAsIQuestion.StataExportCaption)
                            ? childAsIQuestion.StataExportCaption
                            : "__" + childAsIQuestion.PublicKey.FormatGuid();

                        var question = new QuestionTemplateModel
                        {
                            Id = childAsIQuestion.PublicKey,
                            VariableName = varName,
                            Conditions = childAsIQuestion.CascadeFromQuestionId.HasValue
                                ? GetConditionForCascadingQuestion(questionnaireDoc, childAsIQuestion.PublicKey)
                                : childAsIQuestion.ConditionExpression,
                            Validations = childAsIQuestion.ValidationExpression,
                            QuestionType = childAsIQuestion.QuestionType,
                            GeneratedTypeName = GenerateQuestionTypeName(childAsIQuestion),
                            GeneratedMemberName = "@__" + varName,
                            GeneratedStateName = "@__" + varName + "_state",
                            GeneratedIdName = "@__" + varName + "_id",
                            GeneratedConditionsMethodName = "IsEnabled_" + varName,
                            GeneratedValidationsMethodName = "IsValid_" + varName,
                            GeneratedMandatoryMethodName = "IsManadatoryValid_" + varName,
                            IsMandatory = childAsIQuestion.Mandatory,
                            RosterScopeName = currentScope.GeneratedRosterScopeName
                        };

                        currentScope.Questions.Add(question);

                        if (allQuestions.All(x => x.VariableName != question.VariableName))
                        {
                            allQuestions.Add(question);
                        }

                        continue;
                    }

                    var childAsIGroup = child as IGroup;
                    if (childAsIGroup != null)
                    {
                        if (IsRosterGroup(childAsIGroup))
                        {
                            Guid currentScopeId = childAsIGroup.RosterSizeSource == RosterSizeSourceType.FixedTitles
                                ? childAsIGroup.PublicKey
                                : childAsIGroup.RosterSizeQuestionId.Value;

                            List<Guid> currentRosterScope = currentScope.RosterScope.ToList();
                            currentRosterScope.Add(currentScopeId);

                            string varName = !String.IsNullOrWhiteSpace(childAsIGroup.VariableName)
                                ? childAsIGroup.VariableName
                                : "__" + childAsIGroup.PublicKey.FormatGuid();

                            var roster = new RosterTemplateModel
                            {
                                Id = childAsIGroup.PublicKey,
                                Conditions = childAsIGroup.ConditionExpression,
                                VariableName = varName,
                                GeneratedTypeName =
                                    GenerateTypeNameByScope(currentRosterScope, generatedScopesTypeNames),
                                GeneratedStateName = "@__" + varName + "_state",
                                ParentScope = currentScope,
                                GeneratedIdName = "@__" + varName + "_id",
                                GeneratedConditionsMethodName = "IsEnabled_" + varName,
                                RosterScope = currentRosterScope,
                                GeneratedRosterScopeName = "@__" + varName + "_scope",
                            };

                            rostersToProcess.Enqueue(new Tuple<IGroup, RosterScopeBaseModel>(childAsIGroup, roster));

                            if (allRosters.All(x => x.VariableName != roster.VariableName))
                            {
                                allRosters.Add(roster);
                            }

                            currentScope.Rosters.Add(roster);
                        }
                        else
                        {
                            string varName = childAsIGroup.PublicKey.FormatGuid();
                            var group =
                                new GroupTemplateModel
                                {
                                    Id = childAsIGroup.PublicKey,
                                    Conditions = childAsIGroup.ConditionExpression,
                                    VariableName = "@__" + varName, //generating variable name by publicKey
                                    GeneratedStateName = "@__" + varName + "_state",
                                    GeneratedIdName = "@__" + varName + "_id",
                                    GeneratedConditionsMethodName = "IsEnabled_" + varName,
                                    RosterScopeName = currentScope.GeneratedRosterScopeName
                                };

                            currentScope.Groups.Add(group);
                            allGroups.Add(group);
                            foreach (IComposite childGroup in childAsIGroup.Children)
                            {
                                childrenOfCurrentRoster.Enqueue(childGroup);
                            }
                        }
                    }
                }
            }
        }

        private string GetConditionForCascadingQuestion(QuestionnaireDocument questionnaireDocument, Guid cascadingQuestionId)
        {
            var childQuestion = questionnaireDocument.Find<SingleQuestion>(cascadingQuestionId);
            var parentQuestion = questionnaireDocument.Find<SingleQuestion>(childQuestion.CascadeFromQuestionId.Value);

            string childQuestionCondition = (string.IsNullOrWhiteSpace(childQuestion.ConditionExpression)
                ? ""
                : string.Format(" && {0}", childQuestion.ConditionExpression));

            return string.Format("!IsAnswerEmpty({0})", parentQuestion.StataExportCaption) + childQuestionCondition;
        }

        private string GenerateQuestionTypeName(IQuestion question)
        {
            switch (question.QuestionType)
            {
                case QuestionType.Text:
                    return "string";

                case QuestionType.AutoPropagate:
                    return "long?";

                case QuestionType.Numeric:
                    return (question as NumericQuestion).IsInteger ? "long?" : "double?";

                case QuestionType.QRBarcode:
                    return "string";

                case QuestionType.MultyOption:
                    return (question.LinkedToQuestionId == null) ? "decimal[]" : "decimal[][]";

                case QuestionType.DateTime:
                    return "DateTime?";

                case QuestionType.SingleOption:
                    return (question.LinkedToQuestionId == null) ? "decimal?" : "decimal[]";

                case QuestionType.TextList:
                    return "Tuple<decimal, string>[]";

                case QuestionType.GpsCoordinates:
                    return "GeoLocation";

                case QuestionType.Multimedia:
                    return "string";

                default:
                    throw new ArgumentException("Unknown question type.");
            }
        }

        private Dictionary<Guid, List<Guid>> BuildConditionalDependencies(QuestionnaireDocument questionnaireDocument,
            Dictionary<string, Guid> variableNames)
        {
            var groupsWithConditions = questionnaireDocument.GetAllGroups().Where(x => !string.IsNullOrWhiteSpace(x.ConditionExpression));
            var questionsWithCondition = questionnaireDocument
                .GetEntitiesByType<IQuestion>()
                .Where(x => !string.IsNullOrWhiteSpace(x.ConditionExpression));

            Dictionary<Guid, List<Guid>> dependencies = groupsWithConditions.ToDictionary(
                x => x.PublicKey,
                x => GetIdsOfQuestionsInvolvedInExpression(x.ConditionExpression, variableNames));

            questionsWithCondition.ToDictionary(
                x => x.PublicKey,
                x => GetIdsOfQuestionsInvolvedInExpression(x.ConditionExpression, variableNames))
                .ToList()
                .ForEach(x => dependencies.Add(x.Key, x.Value));

            var cascadingQuestions = questionnaireDocument
                .GetEntitiesByType<SingleQuestion>()
                .Where(x => x.CascadeFromQuestionId.HasValue);

            foreach (var cascadingQuestion in cascadingQuestions)
            {
                if (dependencies.ContainsKey(cascadingQuestion.PublicKey))
                {
                    dependencies[cascadingQuestion.PublicKey].Add(cascadingQuestion.CascadeFromQuestionId.Value);
                }
                else
                {
                    dependencies.Add(cascadingQuestion.PublicKey, new List<Guid>{ cascadingQuestion.CascadeFromQuestionId.Value});
                }
            }

            return dependencies;
        }

        private List<Guid> GetIdsOfQuestionsInvolvedInExpression(string conditionExpression,
            Dictionary<string, Guid> variableNames)
        {
            var identifiersUsedInExpression = ExpressionProcessor.GetIdentifiersUsedInExpression(conditionExpression);

            return new List<Guid>(
                from variable in identifiersUsedInExpression
                where variableNames.ContainsKey(variable)
                select variableNames[variable]);
        }

        private static bool IsRosterGroup(IGroup group)
        {
            return group.IsRoster;
        }

        private string GenerateTypeNameByScope(IEnumerable<Guid> currentRosterScope,
            Dictionary<string, string> generatedScopesTypeNames)
        {
            string scopeStringKey = String.Join("$", currentRosterScope);
            if (!generatedScopesTypeNames.ContainsKey(scopeStringKey))
                generatedScopesTypeNames.Add(scopeStringKey, "@__" + Guid.NewGuid().FormatGuid());

            return generatedScopesTypeNames[scopeStringKey];
        }
    }
}