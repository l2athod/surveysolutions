﻿define('vm.questionnaire',
    ['ko', 'underscore', 'config', 'utils', 'datacontext', 'router', 'model', 'bootbox'],
    function (ko, _, config, utils, datacontext, router, model, bootbox) {
        var filter = ko.observable(''),
            isFilterMode = ko.observable(false),
            selectedGroup = ko.observable(),
            selectedQuestion = ko.observable(),
            questionnaire = ko.observable(model.Questionnaire.Nullo),
            chapters = ko.observableArray(),
            verificationMessages = ko.observableArray(),
            saveMessages = ko.observableArray(),
            searchResult = ko.observableArray(),
            isVerificationSucceeded = ko.observable(),
            statistics = new model.Statistic(),
            isInitialized = false,
            isVerificationRunning = ko.observable(false),
            selectedMessageTab = ko.observable(config.messageTabs.saveMessagesTab),            
            cloneQuestion = function(question) {
                if (question.isNew())
                    return;
                var parent = question.parent();
                var index = question.index();
                var clonedQuestion = question.clone();

                datacontext.questions.add(clonedQuestion);

                parent.childrenID.splice(index + 1, 0, { type: clonedQuestion.type(), id: clonedQuestion.id() });
                parent.fillChildren();
                router.navigateTo(clonedQuestion.getHref());
                calcStatistics();

                clonedQuestion.attachValidation();
            },
            cloneGroup = function(group) {
                if (group.isNew())
                    return;

                var clonedGroup = group.clone();
                datacontext.groups.add(clonedGroup);
                clonedGroup.fillChildren();

                if (group.hasParent()) {
                    var parent = group.parent();
                    var index = group.index();
                    parent.childrenID.splice(index + 1, 0, { type: clonedGroup.type(), id: clonedGroup.id() });
                    parent.fillChildren();
                } else {
                    var item = utils.findById(datacontext.questionnaire.childrenID(), group.id());
                    datacontext.questionnaire.childrenID.splice(item.index + 1, 0, { type: clonedGroup.type(), id: clonedGroup.id() });
                    chapters(datacontext.groups.getChapters());
                }
                router.navigateTo(clonedGroup.getHref());
                calcStatistics();
            },
            activate = function(routeData, callback) {
                if (!isInitialized) {
                    getChapters();
                    questionnaire(datacontext.questionnaire);
                    calcStatistics();
                }
                if (!_.isUndefined(selectedGroup())) {
                    selectedGroup().isSelected(false);
                }
                if (!_.isUndefined(selectedQuestion())) {
                    selectedQuestion().isSelected(false);
                }
                questionnaire().isSelected(false);

                if (routeData.has('questionnaire')) {
                    editQuestionnaire(routeData.questionnaire);
                }
                if (routeData.has('question')) {
                    editQuestion(routeData.question);
                }
                if (routeData.has('group')) {
                    editGroup(routeData.group);
                }
                isInitialized = true;

                $("a[data-toggle=popover]")
                    .popover()
                    .click(function(e) {
                        e.preventDefault();
                    });
            },
            getChapters = function() {
                if (!chapters().length) {
                    chapters(datacontext.groups.getChapters());
                }
            },
            editQuestionnaire = function() {
                questionnaire().isSelected(true);
                openDetails("show-questionnaire");
            },
            editQuestion = function (id) {
                var question = datacontext.questions.getLocalById(id);
                if (_.isNull(question) || question.isNullo) {
                    return;
                }
                
                question.isSelected(true);

                question.alias.valueHasMutated();
                question.qtype.valueHasMutated();
                question.selectedLinkTo.valueHasMutated();
                question.answerOptions.valueHasMutated();
                question.validationExpression.valueHasMutated();
                question.condition.valueHasMutated();
                question.title.valueHasMutated();
                question.isFeatured.valueHasMutated();
             
                question.localQuestionsFromProragatedGroups(datacontext.groups.getQuestionsFromPropagatableGroups());
                selectedQuestion(question);
                selectedQuestion.valueHasMutated();
                openDetails("show-question");
                $('#alias').focus();
            },
            editGroup = function(id) {
                var group = datacontext.groups.getLocalById(id);
                if (_.isNull(group) || group.isNullo) {
                    return;
                }
                group.isSelected(true);
                group.allowedQuestions(datacontext.questions.getAllAllowedQuestionsForSelect());
                group.rosterSizeQuestion.valueHasMutated();
                selectedGroup(group);
                openDetails("show-group");
            },
            openDetails = function(style) {
                $('#stacks').removeClass("show-question").removeClass("show-group");
                $('#stacks').addClass('detail-visible').addClass(style);
            },
            closeDetails = function() {
                $('#stacks').removeClass("show-question").removeClass("show-group");
                $('#stacks').removeClass('detail-visible');
            },
            isOutputVisible = ko.observable(false),
            toggleOutput = function() {
                isOutputVisible(!isOutputVisible());
            },
            addQuestion = function(parent) {
                var question = new model.Question();
                question.parent(parent);

                datacontext.questions.add(question);

                parent.childrenID.push({ type: question.type(), id: question.id() });
                parent.fillChildren();
                router.navigateTo(question.getHref());
                calcStatistics();

                question.attachValidation();
            },
            addChapter = function() {
                var group = new model.Group();
                group.level(0);
                group.title('New Chapter');
                group.parent(null);
                datacontext.groups.add(group);
                datacontext.questionnaire.childrenID.push({ type: group.type(), id: group.id() });
                chapters.push(group);
                group.children.id = group.id();
                router.navigateTo(group.getHref());
                calcStatistics();
            },
            addGroup = function(parent) {
                var group = new model.Group();
                group.parent(parent);
                datacontext.groups.add(group);
                parent.childrenID.push({ type: group.type(), id: group.id() });
                parent.fillChildren();
                router.navigateTo(group.getHref());
                calcStatistics();
            },
            deleteGroup = function(item) {
                bootbox.confirm("Are you sure you want to delete this question?", function(result) {
                    if (result == false)
                        return;

                    if (item.isNew()) {
                        deleteGroupSuccessCallback(item);
                    } else {
                        datacontext.sendCommand(
                            config.commands.deleteGroup,
                            item,
                            {
                                success: function() {
                                    deleteGroupSuccessCallback(item);
                                    clearSaveMessages();
                                },
                                error: function(d) {
                                    showSaveMessage(d);
                                }
                            });
                    }
                });
            },            
            deleteGroupSuccessCallback = function(item) {
                datacontext.groups.removeGroup(item.id());

                var parent = item.parent();
                if (!(_.isUndefined(parent) || (_.isNull(parent)))) {
                    var child = _.find(parent.childrenID(), { 'id': item.id() });
                    parent.childrenID.remove(child);

                    _.each(datacontext.groups.getAllLocal(), function(group) {
                        group.fillChildren();
                    });
                    datacontext.questions.cleanTriggers(child);
                    router.navigateTo(parent.getHref());
                } else {
                    var child = _.find(datacontext.questionnaire.childrenID(), { 'id': item.id() });
                    datacontext.questionnaire.childrenID.remove(child);

                    chapters(datacontext.groups.getChapters());
                    router.navigateTo(config.hashes.details);
                }
                calcStatistics();
                isOutputVisible(false);
            },
            deleteQuestion = function(item) {
                bootbox.confirm("Are you sure you want to delete this question?", function(result) {
                    if (result == false)
                        return;

                    if (item.isNew()) {
                        deleteQuestionSuccessCallback(item);
                    } else {
                        datacontext.sendCommand(
                            config.commands.deleteQuestion,
                            item,
                            {
                                success: function() {
                                    deleteQuestionSuccessCallback(item);
                                    clearSaveMessages();
                                },
                                error: function(d) {
                                    showSaveMessage(d);
                                }
                            });
                    }
                });
            },
            deleteQuestionSuccessCallback = function(item) {
                datacontext.questions.removeById(item.id());

                var parent = item.parent();
                var child = _.find(parent.childrenID(), { 'id': item.id() });
                parent.childrenID.remove(child);
                parent.fillChildren();
                calcStatistics();

                if (isFilterMode() == true) {
                    filter.valueHasMutated();
                } else {
                    router.navigateTo(parent.getHref());
                }

                isOutputVisible(false);
            },
            saveGroup = function(group) {

                if (group.hasParent() && group.parent().isNew()) {
                    config.logger(config.warnings.saveParentFirst);
                    return;
                }

                var command = '';
                if (group.isNew()) {
                    if (group.isClone()) {
                        command = config.commands.cloneGroup;
                    } else {
                        command = config.commands.createGroup;
                    }
                } else {
                    command = config.commands.updateGroup;
                }

                group.canUpdate(false);

                datacontext.sendCommand(
                    command,
                    group,
                    {
                        success: function() {
                            group.isNew(false);
                            group.dirtyFlag().reset();
                            group.fillChildren();
                            calcStatistics();
                            group.canUpdate(true);
                            group.commit();
                            clearSaveMessages();
                        },
                        error: function(d) {
                            showSaveMessage(d);
                            group.canUpdate(true);
                        }
                    });
            },
            saveQuestion = function(question) {

                if (question.hasParent() && question.parent().isNew()) {
                    config.logger(config.warnings.saveParentFirst);
                    return;
                }

                var command = '';
                switch (question.qtype()) {
                case config.questionTypes.AutoPropagate:
                case config.questionTypes.Numeric:
                    if (question.isNew()) {
                        if (question.isClone()) {
                            command = config.commands.cloneNumericQuestion;
                        } else {
                            command = config.commands.createNumericQuestion;
                        }
                    } else {
                        command = config.commands.updateNumericQuestion;
                    }
                    break;
                case config.questionTypes.TextList:
                    if (question.isNew()) {
                        if (question.isClone()) {
                            command = config.commands.cloneTextListQuestion;
                        } else {
                            command = config.commands.createTextListQuestion;
                        }
                    } else {
                        command = config.commands.updateTextListQuestion;
                    }
                    break;
                case config.questionTypes.QRBarcode:
                    if (question.isNew()) {
                        if (question.isClone()) {
                            command = config.commands.cloneQRBarcodeQuestion;
                        } else {
                            command = config.commands.addQRBarcodeQuestion;
                        }
                    } else {
                        command = config.commands.updateQRBarcodeQuestion;
                    }
                    break;
                default:
                    if (question.isNew()) {
                        if (question.isClone()) {
                            command = config.commands.cloneQuestion;
                        } else {
                            command = config.commands.createQuestion;
                        }
                    } else {
                        command = config.commands.updateQuestion;
                    }
                }

                question.canUpdate(false);

                datacontext.sendCommand(
                    command,
                    question,
                    {
                        success: function() {
                            question.isNew(false);
                            question.dirtyFlag().reset();
                            calcStatistics();
                            question.canUpdate(true);
                            question.commit();
                            clearSaveMessages();
                        },
                        error: function(d) {
                            showSaveMessage(d);
                            question.canUpdate(true);
                        }
                    });
            },
            saveQuestionnaire = function(questionnaire) {

                questionnaire.canUpdate(false);

                datacontext.sendCommand(
                    config.commands.updateQuestionnaire,
                    questionnaire,
                    {
                        success: function() {
                            questionnaire.dirtyFlag().reset();
                            questionnaire.canUpdate(true);
                            clearSaveMessages();
                        },
                        error: function(d) {
                            showSaveMessage(d);
                            questionnaire.canUpdate(true);
                        }
                    });
            },
            addSharedPerson = function(sharedUser) {
                sharedUser.check(function() {
                    datacontext.sendCommand(
                        config.commands.addSharedPersonToQuestionnaire,
                        sharedUser,
                        {
                            success: function() {
                                questionnaire().addSharedPerson();
                                clearSaveMessages();
                            },
                            error: function(d) {
                                showSaveMessage(d);
                            }
                        });
                });
            },
            removeSharedPerson = function(sharedUser) {
                datacontext.sendCommand(
                    config.commands.removeSharedPersonFromQuestionnaire,
                    sharedUser,
                    {
                        success: function() {
                            questionnaire().removeSharedPerson(sharedUser);
                            clearSaveMessages();
                        },
                        error: function(d) {
                            showSaveMessage(d);
                        }
                    });
            },
            clearFilter = function() {
                filter('');
                focusOnSearch();
            },
            filterContent = function() {
                var query = filter().trim().toLowerCase();
                isFilterMode(query !== '');
                searchResult.removeAll();
                if (query != '') {
                    searchResult(datacontext.questions.search(query));
                }
            },
            isMovementPossible = function(arg, event, ui) {

                var fromId = arg.sourceParent.id;
                var toId = arg.targetParent.id;
                var moveItemType = arg.item.type().replace('View', '').toLowerCase();
                var isItemFeaturedQuestion = false;
                var isItemRosterTitleQuestion = false;
                var canMoveRosterTitleQuestionToTarget = false;
                var isItemRosterSizeQuestion = false;
                var isItemLinkedQuestion = false;
                var targetGroupIsRoster = false;
                if (moveItemType == "question") {
                    isItemRosterTitleQuestion = datacontext.questions.isRosterTitleQuestion(arg.item.id());
                    isItemRosterSizeQuestion = datacontext.questions.isRosterSizeQuestion(arg.item.id());
                    isItemFeaturedQuestion = arg.item.isFeatured();
                    isItemLinkedQuestion = datacontext.questions.isLinkedQuestion(arg.item);
                }

                var isDropedOutsideAnyChapter = $(ui.item).parent('#chapters-list').length > 0;
                var isDropedInQuestionnaire = (_.isNull(toId) || _.isUndefined(toId));
                var isDraggedFromChapter = (_.isNull(fromId) || _.isUndefined(fromId));

                if (arg.item.isNew()) {
                    arg.cancelDrop = true;
                    config.logger(config.warnings.cantMoveUnsavedItem);
                    return;
                }

                if (isDropedOutsideAnyChapter) {
                    arg.cancelDrop = true;
                    config.logger(config.warnings.cantMoveAutoPropagatedGroupOutsideGroup);
                    return;
                }

                if (isDropedOutsideAnyChapter && moveItemType == "question") {
                    arg.cancelDrop = true;
                    config.logger(config.warnings.cantMoveQuestionOutsideGroup);
                    return;
                }
                var source = datacontext.groups.getLocalById(fromId);
                    
                if (isDropedInQuestionnaire) {
                    toId = null;
                } else {
                    var target = datacontext.groups.getLocalById(toId);

                    targetGroupIsRoster = datacontext.groups.isRosterOrInsideRoster(target);
                    
                    
                    if (isItemRosterTitleQuestion && targetGroupIsRoster) {
                        canMoveRosterTitleQuestionToTarget = datacontext.questions.isRosterTitleInRosterByRosterSize(arg.item, target);
                    }

                    if (target.isNew()) {
                        arg.cancelDrop = true;
                        config.logger(config.warnings.cantMoveIntoUnsavedItem);
                        return;
                    }

                    if (isItemFeaturedQuestion && targetGroupIsRoster) {
                        arg.cancelDrop = true;
                        config.logger(config.warnings.cantMoveFeaturedQuestionIntoAutoGroup);
                        return;
                    }

                    if (isItemRosterTitleQuestion && !canMoveRosterTitleQuestionToTarget) {
                        arg.cancelDrop = true;
                        config.logger(config.warnings.cantMoveRosterTitleQuestion);
                        return;
                    }
                    
                    if (isItemLinkedQuestion && !targetGroupIsRoster) {
                        arg.cancelDrop = true;
                        config.logger(config.warnings.cantMoveLinkedQuestionOutsideRoster);
                        return;
                    }

                    if (isDropedInQuestionnaire && moveItemType == "group" && arg.item.isRoster()) {
                        arg.cancelDrop = true;
                        config.logger(config.warnings.propagatedGroupCantBecomeChapter);
                        return;
                    }
                    
                    if (moveItemType == "group" && !arg.item.isRoster() && datacontext.groups.hasRosterTitleQuestion(arg.item)) {
                        arg.cancelDrop = true;
                        config.logger(config.warnings.cantMoveGroupWithRosterTitleQuestion);
                        return;
                    }
                    
                    if (moveItemType == "group" && !arg.item.isRoster() && !targetGroupIsRoster && datacontext.groups.hasLinkedQuestion(arg.item)) {
                        arg.cancelDrop = true;
                        config.logger(config.warnings.cantMoveGroupWithLinkedQuestion);
                        return;
                    }
                }
                var item = arg.item;

                var moveCommand = {
                    targetGroupId: toId,
                    targetIndex: arg.targetIndex
                };
                moveCommand[moveItemType + 'Id'] = item.id();

                datacontext.sendCommand(
                    config.commands[moveItemType + "Move"],
                    moveCommand,
                    {
                        success: function(d) {
                            saveMessages.removeAll();
                            if (isDraggedFromChapter) {
                                var child = _.find(datacontext.questionnaire.childrenID(), { 'id': item.id() });
                                datacontext.questionnaire.childrenID.remove(child);
                                chapters(datacontext.groups.getChapters());
                            } else {
                                var child = _.find(source.childrenID(), { 'id': item.id() });
                                source.childrenID.remove(child);
                                source.fillChildren();
                            }

                            if (isDropedInQuestionnaire) {
                                item.level(0);
                                datacontext.questionnaire.childrenID.splice(arg.targetIndex, 0, { type: item.type(), id: item.id() });
                                chapters(datacontext.groups.getChapters());
                            } else {
                                if (moveItemType == "group") {
                                    item.level(target.level() + 1);
                                }
                                target.childrenID.splice(arg.targetIndex, 0, { type: item.type(), id: item.id() });
                                target.fillChildren();
                            }
                            item.parent(target);
                        },
                        error: function(d) {
                            _.each(datacontext.groups.getAllLocal(), function(group) {
                                group.fillChildren();
                            });

                            chapters(datacontext.groups.getChapters());

                            showSaveMessage(d);
                        }
                    });
            },
            calcStatistics = function() {
                var questions = datacontext.questions.getAllLocal();
                var groups = datacontext.groups.getAllLocal();
                statistics.questions(questions.length);
                statistics.groups(groups.length);
                var counter = _.countBy(questions, function(q) { return q.isNew(); });
                statistics.unsavedQuestion(_.isUndefined(counter['true']) ? 0 : counter['true']);
                counter = _.countBy(groups, function(g) { return g.isNew(); });
                statistics.unsavedGroups(_.isUndefined(counter['true']) ? 0 : counter['true']);
            },
            init = function() {
                filter.subscribe(filterContent);
                ko.bindingHandlers.sortable.options.start = function(arg, ui) {
                    if ($(ui.item).children('.ui-expander').length > 0) {
                        var button = $(ui.item).children('.ui-expander').children('.ui-expander-head');
                        if ($(button).hasClass('ui-expander-head-collapsed') == false) {
                            button.click();
                        }
                    }
                };

                if (datacontext.questions.getAllLocal().length <= 500) {
                    _.each(datacontext.questions.getAllLocal(), function(question) {
                        question.attachValidation();
                    });
                }
                
                _.each(datacontext.groups.getAllLocal(), function (group) {
                    group.attachValidation();
                });
            },
            isAllChaptersExpanded = ko.computed(function() {
                return _.some(chapters(), function(chapter) {
                    return chapter.isExpanded();
                });
            }),
            toggleAllChapters = function() {
                if (isAllChaptersExpanded()) {
                    _.each(chapters(), function(chapter) {
                        chapter.isExpanded(false);
                    });
                } else {
                    _.each(chapters(), function(chapter) {
                        chapter.isExpanded(true);
                    });
                }
            },
            toggleAllChaptersTooltip = ko.computed(function() {
                var tooltip = {
                    title: (isAllChaptersExpanded() == true ? 'Collapse' : 'Expand') + ' all chapters',
                    placement: 'right'
                };
                return tooltip;
            }).extend({ throttle: 400 }),
            focusOnSearch = function() {
                $('#filter input').get(0).focus();
            },
            showSaveMessage = function(message) {
                saveMessages.removeAll();

                saveMessages.push(new model.Error(
                    _.isUndefined(message.error) ? message : message.error
                ));

                isVerificationSucceeded(false);
                isOutputVisible(true);
                selectedMessageTab(config.messageTabs.saveMessagesTab);

            },
            clearSaveMessages = function() {
                isOutputVisible(false);
                saveMessages.removeAll();
                selectedMessageTab(config.messageTabs.verificationMessagesTab);
            },
            showVerificationMessages = function(messages) {
                _.each(messages, function(message) {
                    verificationMessages.push(message);
                });
                isOutputVisible(true);
            },
            getErrorWithUnsavedItems = function() {
                var unsavedQuestionReferences = _.filter(datacontext.questions.getAllLocal(), function(q) {
                    return q.dirtyFlag().isDirty() || q.isNew();
                }).map(function(q) {
                    return {
                        id: q.id(),
                        type: config.verificationReferenceType.question
                    };
                });
                var unsavedGroupsReferences = _.filter(datacontext.groups.getAllLocal(), function(g) {
                    return g.dirtyFlag().isDirty() || g.isNew();
                }).map(function(g) {
                    return {
                        id: g.id(),
                        type: config.verificationReferenceType.group
                    };
                });
                var message = "Following items are not saved, please save them before proceeding with verification:";
                var code = "WB0000";
                var references = _.union(unsavedQuestionReferences, unsavedGroupsReferences);
                return _.isEmpty(references) ? null : new model.Error(message, code, references);
            },
            runVerifier = function() {
                var unsavedItemsError = getErrorWithUnsavedItems();
                if (_.isNull(unsavedItemsError)) {
                    saveMessages.removeAll();
                    verificationMessages.removeAll();
                    isVerificationRunning(true);
                    datacontext.runRemoteVerification({
                        success: function(response) {
                            isVerificationSucceeded(response.length == 0);
                            showVerificationMessages(response);
                            isVerificationRunning(false);
                        },
                        error: function(response) {
                            showSaveMessage(response.error);
                            isVerificationRunning(false);
                        }
                    });
                } else {
                    isVerificationSucceeded(false);
                    verificationMessages.removeAll();
                    showVerificationMessages([unsavedItemsError]);
                }
                selectedMessageTab(config.messageTabs.verificationMessagesTab);
            },
            isToggleMessagesButtonIsVisible = ko.computed(function () {
                return (saveMessages().length + verificationMessages().length) + (isVerificationSucceeded() ? 1 : 0) > 0;
            });

        init();

        return {
            cloneQuestion: cloneQuestion,
            cloneGroup: cloneGroup,
            activate: activate,
            questionnaire: questionnaire,
            chapters: chapters,
            selectedGroup: selectedGroup,
            selectedQuestion: selectedQuestion,
            closeDetails: closeDetails,
            addQuestion: addQuestion,
            addGroup: addGroup,
            addChapter: addChapter,
            clearFilter: clearFilter,
            filter: filter,
            isFilterMode: isFilterMode,
            isMovementPossible: isMovementPossible,
            saveGroup: saveGroup,
            deleteGroup: deleteGroup,
            saveQuestion: saveQuestion,
            deleteQuestion: deleteQuestion,
            isOutputVisible: isOutputVisible,
            toggleOutput: toggleOutput,
            verificationMessages: verificationMessages,
            saveMessages: saveMessages,
            statistics: statistics,
            searchResult: searchResult,
            saveQuestionnaire: saveQuestionnaire,
            isAllChaptersExpanded: isAllChaptersExpanded,
            toggleAllChapters: toggleAllChapters,
            toggleAllChaptersTooltip: toggleAllChaptersTooltip,
            addSharedPerson: addSharedPerson,
            removeSharedPerson: removeSharedPerson,
            runVerifier: runVerifier,
            switchMessageTab: selectedMessageTab,
            selectedMessageTab: selectedMessageTab,
            isVerificationSucceeded: isVerificationSucceeded,
            isVerificationRunning: isVerificationRunning,
            isToggleMessagesButtonIsVisible: isToggleMessagesButtonIsVisible
        };
    });