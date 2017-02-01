import * as debounce from "lodash/debounce"
import * as map from "lodash/map"
import * as Vue from "vue"
import { apiCaller } from "../api"
import router from "./../router"

import { batchedAction } from "./helpers"

export default {
    async loadQuestionnaire({ commit }, questionnaireId) {
        const questionnaireInfo = await apiCaller<IQuestionnaireInfo>(api => api.questionnaireDetails(questionnaireId))
        commit("SET_QUESTIONNAIRE_INFO", questionnaireInfo)
    },

    fetchEntity: batchedAction(async ({commit, dispatch}, ids) => {
        const details = await apiCaller(api => api.getEntitiesDetails(map(ids, "id")))
        dispatch("fetch", { ids, done: true })
        commit("SET_ENTITIES_DETAILS", details)
    }, "fetch", /* limit */ 100),

    answerSingleOptionQuestion({ dispatch }, answerInfo) {
        dispatch("fetch", { id: answerInfo.questionId })
        apiCaller(api => api.answerSingleOptionQuestion(answerInfo.answer, answerInfo.questionId))
    },
    answerTextQuestion({ dispatch }, { identity, text }) {
        dispatch("fetch", { id: identity })
        apiCaller(api => api.answerTextQuestion(identity, text))
    },
    answerMultiOptionQuestion({ dispatch }, { answer, questionId }) {
        dispatch("fetch", { id: questionId })
        apiCaller(api => api.answerMultiOptionQuestion(answer, questionId))
    },
    answerYesNoQuestion({ dispatch }, { questionId, answer }) {
        dispatch("fetch", { id: questionId })
        apiCaller(api => api.answerYesNoQuestion(questionId, answer))
    },
    answerIntegerQuestion({ dispatch }, { identity, answer }) {
        dispatch("fetch", { id: identity })
        apiCaller(api => api.answerIntegerQuestion(identity, answer))
    },
    answerDoubleQuestion({ dispatch }, { identity, answer }) {
        dispatch("fetch", { id: identity })
        apiCaller(api => api.answerDoubleQuestion(identity, answer))
    },
    answerGpsQuestion({ dispatch }, { identity, answer }) {
        dispatch("fetch", { id: identity })
        apiCaller(api => api.answerGpsQuestion(identity, answer))
    },
    answerDateQuestion({ dispatch }, { identity, date }) {
        dispatch("fetch", { id: identity })
        apiCaller(api => api.answerDateQuestion(identity, date))
    },
    answerTextListQuestion({dispatch}, {identity, rows}) {
        dispatch("fetch", { id: identity })
        apiCaller(api => api.answerTextListQuestion(identity, rows))
    },
    answerLinkedSingleOptionQuestion({dispatch}, {questionIdentity, answer}) {
        dispatch("fetch", { id: questionIdentity })
        apiCaller(api => api.answerLinkedSingleOptionQuestion(questionIdentity, answer))
    },
    answerLinkedMiltiOptionQuestion({dispatch}, {questionIdentity, answer}) {
        dispatch("fetch", { id: questionIdentity })
        apiCaller(api => api.answerLinkedSingleOptionQuestion(questionIdentity, answer))
    },
    answerLinkedToListMultiQuestion({dispatch}, {questionIdentity, answer}) {
        dispatch("fetch", { id: questionIdentity })
        apiCaller(api => api.answerLinkedToListMultiQuestion(questionIdentity, answer))
    },
    answerLinkedToListSingleQuestion({dispatch}, {questionIdentity, answer}) {
        dispatch("fetch", { id: questionIdentity })
        apiCaller(api => api.answerLinkedToListSingleQuestion(questionIdentity, answer))
    },
    removeAnswer({ dispatch }, questionId: string) {
        dispatch("fetch", { id: questionId })
        apiCaller(api => api.removeAnswer(questionId))
    },
    setAnswerAsNotSaved({ commit }, entity) {
        commit("SET_ANSWER_NOT_SAVED", entity)
    },

    fetchSectionEntities: debounce(async ({ commit }) => {
        const id = (router.currentRoute.params as any).sectionId
        const section = id == null
            ? await apiCaller(api => api.getPrefilledEntities())
            : await apiCaller(api => api.getSectionEntities(id))

        commit("SET_SECTION_DATA", section)
    }, 200),

    // called by server side. refresh
    refreshEntities({ state, dispatch }, questions: string[]) {
        let needSectionUpdate = false

        questions.forEach(id => {
            if (state.entityDetails[id]) { // do not fetch entity that is no in the visible list
                needSectionUpdate = true

                dispatch("fetchEntity", { id, source: "server" })
            }
        })

        dispatch("refreshSectionState", null)
    },

    refreshSectionState: debounce(({ dispatch }) => {
        dispatch("fetchBreadcrumbs")
        dispatch("fetchEntity", { id: "NavigationButton", source: "server" })
        dispatch("fetchSidebar")
    }, 200),

    fetchBreadcrumbs: debounce(async ({ commit }) => {
        const crumps = await apiCaller(api => api.getBreadcrumbs())
        commit("SET_BREADCRUMPS", crumps)
    }, 200),

    cleanUpEntity({ commit }, id) {
        commit("CLEAR_ENTITY", id)
    }
}
