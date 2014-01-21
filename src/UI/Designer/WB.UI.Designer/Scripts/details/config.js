﻿define('config',
    ['pnotify', 'ko', 'amplify'],
    function(toastr, ko, amplify) {

        var // properties
            //-----------------
            questionTypes = {
                "SingleOption": "SingleOption",
                "MultyOption": "MultyOption",
                "Numeric": "Numeric",
                "DateTime": "DateTime",
                "Text": "Text",
                "GpsCoordinates": "GpsCoordinates",
                "TextList": "TextList"
            },
            rosterSizeSourceTypes = {
                "FixedTitles": "FixedTitles",
                "Question": "Question"
            },
            verificationReferenceType = {
                question: 1,
                group: 10,
            },
            messageTabs = {
                saveMessagesTab: 1,
                verificationMessagesTab: 2
            },
            questionTypeOptions = [
                {
                    key: "SingleOption",
                    value: "Categorical: one answer"
                },
                {
                    key: "MultyOption",
                    value: "Categorical: multiple answers"
                },
                {
                    key: "Numeric",
                    value: "Numeric"
                },
                {
                    key: "DateTime",
                    value: "Date"
                },
                {
                    key: "Text",
                    value: "Text"
                },
                {
                    key: "GpsCoordinates",
                    value: "Geo Location"
                },
                {
                    key: "TextList",
                    value: "List"
                }
            ],
            questionScopes = {
                interviewer: "Interviewer",
                supervisor: "Supervisor",
                headquarters: "Headquarters"
            },
            commands = {
                updateQuestionnaire: "UpdateQuestionnaire",
                createGroup: "AddGroup",
                cloneGroup: "CloneGroupWithoutChildren",
                updateGroup: "UpdateGroup",
                deleteGroup: "DeleteGroup",
                groupMove: "MoveGroup",
                createQuestion: "AddQuestion",
                createNumericQuestion: "AddNumericQuestion",
                createListQuestion: "AddListQuestion",
                cloneQuestion: "CloneQuestion",
                cloneNumericQuestion: "CloneNumericQuestion",
                cloneListQuestion: "CloneListQuestion",
                updateQuestion: "UpdateQuestion",
                updateNumericQuestion: "UpdateNumericQuestion",
                updateListQuestion: "UpdateListQuestion",
                deleteQuestion: "DeleteQuestion",
                questionMove: "MoveQuestion",
                addSharedPersonToQuestionnaire: "AddSharedPersonToQuestionnaire",
                removeSharedPersonFromQuestionnaire: "RemoveSharedPersonFromQuestionnaire"
            },
            hashes = {
                details: '#/details',
                detailsGroup: '#/details/group',
                detailsQuestion: '#/details/question',
                detailsQuestionnaire: '#/details/questionnaire'
            },
            viewIds = {
                details: '#stacks'
            },
            messages = {
                viewModelActivated: 'viewmodel-activation'
            },
            stateKeys = {
                lastView: 'state.active-hash'
            },
            tips = {
                newGroup: {
                    title: "Save this group",
                    content: "You should save this group to perform any actions with it",
                    placement: "top",
                    trigger: "hover"
                }
            },
            logger = $.pnotify, // use pnotify for the logger

            storeExpirationMs = (1000 * 60 * 60 * 24), // 1 day
            throttle = 400,
            loggerTmeout = 2000,
            warnings = {
                propagatedGroupCantBecomeChapter: {
                    title: "Can't move",
                    text: "Roster can't become a chapter"
                },
                cantMoveQuestionOutsideGroup: {
                    title: "Can't move",
                    text: "You can't move a question outside of any group"
                },
                cantMoveRosterIntoRoster: {
                    title: "Can't move",
                    text: "You can't move a roster inside another roster"
                },
                cantMoveGroupWithLinkedQuestion: {
                    title: "Can't move",
                    text: "You can't move a group that contains question used as linked question inside another group"
                },
                cantMoveLinkedQuestionOutsideRoster: {
                    title: "Can't move",
                    text: "You can't move a question that used as linked question outside roster"
                },
                cantMoveGroupWithRosterSizeQuestionIntoRoster: {
                    title: "Can't move",
                    text: "You can't move a group with roster size question into a roster"
                },
                cantMoveGroupWithRosterTitleQuestion: {
                    title: "Can't move",
                    text: "You can't move a group with roster title question"
                },
                cantMoveUnsavedItem: {
                    title: "Can't move",
                    text: "You can't move unsaved items"
                },
                cantMoveIntoUnsavedItem: {
                    title: "Can't move",
                    text: "You can't move items to unsaved groups or chapters"
                },
                saveParentFirst: {
                    title: "Can't move",
                    text: "Save the parent item first"
                },
                cantMoveAutoPropagatedGroupOutsideGroup: {
                    title: "Can't move group",
                    text: "You can't move a roster outside any chapter"
                },
                cantMoveFeaturedQuestionIntoAutoGroup: {
                    title: "Can't move question",
                    text: "You can't move a pre-filled question into a roster"
                },
                cantMoveRosterSizeQuestionIntoRosterGroup: {
                    title: "Can't move question",
                    text: "You can't move roster size question into a roster group"
                },
                cantMoveRosterTitleQuestion: {
                    title: "Can't move question",
                    text: "You can move a roster title question only to a roster group that has a roster size question the same as parent group of roster title question"
                },
                savedData: 'Data saved successfully',
                weWillClearCondition: {
                    message: "Pre-filled questions can't be conditionally enabled. Would you like to erase the condition expression?",
                    okBtn: "Yes, erase the condition",
                    cancelBtn: "No, keep the condition"
                },
                weWillClearValidation: {
                    message: "Questions filled in by the supervisor don't support validation. Would you like to erase validation expression?",
                    okBtn: "Yes, erase the expressions",
                    cancelBtn: "No, keep the expressions"
                },
                weWillClearSupervisorFlag: {
                    message: "If a question is pre-filled, it can't at the same time be marked as answered by the supervisor. Would you like to disable the 'answered by the supervisor' option for this question?",
                    okBtn: "Yes, disable it",
                    cancelBtn: "No, don't disable it"
                },
            },
            // methods
            //-----------------

            init = function() {
                logger.defaults.delay = loggerTmeout;

                ko.validation.configure({
                    messagesOnModified: true,
                    parseInputAttributes: true,
                    insertMessages: true,
                    decorateElement: true,
                    errorElementClass: 'error',
                    errorMessageClass: "help-inline"
                });

                ko.bindingHandlers.sortable.options = { cursor: "move", handle: ".handler", axis: "y", placeholder: "ui-state-highlight" };
                ko.bindingHandlers.draggable.options = { cursor: "move", handle: ".handler", axis: "y" };
            };

        init();

        return {
            logger: logger,
            storeExpirationMs: storeExpirationMs,
            throttle: throttle,
            warnings: warnings,
            hashes: hashes,
            viewIds: viewIds,
            messages: messages,
            stateKeys: stateKeys,
            questionTypes: questionTypes,
            questionTypeOptions: questionTypeOptions,
            questionScopes: questionScopes,
            commands: commands,
            tips: tips,
            verificationReferenceType: verificationReferenceType,
            messageTabs: messageTabs,
            rosterSizeSourceTypes: rosterSizeSourceTypes
        };
    });
