﻿define('model.mapper',
    ['model', 'config'],
    function (model, config) {
        var getType = function (intType) {
            return intType === 1 ? "QuestionView" : "GroupView";
        },
            // public mapping methods
            error = {
                getDtoId: function (dto) { return dto.Code; },
                fromDto: function (dto) {
                    return new model.Error(dto.Message, dto.Code, _.map(dto.References, function (reference) {
                        return {
                            type: reference.Type,
                            id: reference.Id
                        };
                    }));
                }
            },
            questionnaire = {
                getDtoId: function (dto) { return dto.Id; },
                fromDto: function (dto, item) {
                    item = item || new model.Questionnaire();
                    item.id(this.getDtoId(dto));
                    item.title(dto.Title);
                    item.isPublic(dto.IsPublic);
                    item.childrenID(_.map(dto.Chapters, function (c) {
                        return { type: getType(c.Type), id: c.Id };
                    }));
                    item.dirtyFlag().reset();
                    return item;
                }
            },
            group = {
                getDtoId: function (dto) { return dto.Id; },
                fromDto: function (dto, item) {
                    item = item || new model.Group();
                    item.id(this.getDtoId(dto));
                    item.level(dto.Level);
                    item.title(dto.Title);
                    item.parent(dto.ParentId);
                    item.description(dto.Description);
                    item.condition(dto.ConditionExpression);
                    item.isRoster(dto.IsRoster);
                    item.rosterSizeQuestion(dto.RosterSizeQuestionId);

                    item.childrenID(_.map(dto.Children, function (c) {
                        return { type: getType(c.Type), id: c.Id };
                    }));

                    item.isNew(false);
                    item.dirtyFlag().reset();
                    item.commit();
                    return item;
                }
            },
            question = {
                getDtoId: function (dto) { return dto.Id; },
                fromDto: function (dto, item, otherData) {
                    var groups = otherData.groups;
                    item = item || new model.Question();
                    item.id(this.getDtoId(dto));
                    item.title(dto.Title);
                    item.parent(null);
                    if (!_.isEmpty(dto.ParentId)) {
                        item.parent(groups.getLocalById(dto.ParentId));
                    }
                    item.qtype(dto.QuestionType);

                    if (dto.Featured == false) {
                        item.scope(dto.QuestionScope);
                    }
                  
                    item.isHead(dto.Capital);
                    item.isFeatured(dto.Featured);
                    item.isMandatory(dto.Mandatory);
                    item.condition(dto.ConditionExpression);
                    item.instruction(dto.Instructions);

                    item.alias(dto.Alias);

                    item.validationExpression(dto.ValidationExpression);
                    item.validationMessage(dto.ValidationMessage);
                   
                   
                    item.isInteger(_.isEmpty(dto.IsInteger) ? 0 : (dto.IsInteger ? 1 : 0));

                    if (!_.isEmpty(dto.Settings)) {
                        var settings = dto.Settings;
                        
                        item.isLinked(_.isEmpty(settings.LinkedToQuestionId) == false ? 1 : 0);
                        item.selectedLinkTo(settings.LinkedToQuestionId);
                        item.answerOrder(settings.AnswerOrder);

                        item.answerOptions(_.map(settings.Answers, function (answer) {
                            return new model.AnswerOption()
                                .id(answer.Id)
                                .title(answer.Title)
                                .value(answer.AnswerValue);
                        }));

                      
                        item.maxValue(_.isEmpty(settings.MaxValue) ? null : settings.MaxValue);
                        item.countOfDecimalPlaces(_.isEmpty(settings.CountOfDecimalPlaces) ? null : settings.CountOfDecimalPlaces);
                        item.areAnswersOrdered(_.isEmpty(dto.Settings.AreAnswersOrdered) ? false : settings.AreAnswersOrdered);
                        item.maxAllowedAnswers(_.isEmpty(dto.Settings.MaxAllowedAnswers) ? null : settings.MaxAllowedAnswers);
                    }
                    item.isNew(false);
                    item.dirtyFlag().reset();
                    item.commit();

                    return item;
                }
            };

        return {
            questionnaire: questionnaire,
            question: question,
            group: group,
            error: error
        };
    });