import { map, debounce, uniq } from "lodash"
import Vue from "vue"

import { batchedAction } from "../helpers"

import modal from "../components/modal"

function getAnswer(state, questionId){
    const question = state.entityDetails[questionId]
    if(question == null) return null;
    return question.answer;
}

export default {
    async loadInterview({ commit, rootState }) {
        const interviewId = rootState.route.params.interviewId
        const info = await Vue.$api.get('getInterviewDetails', { interviewId })
        commit("SET_INTERVIEW_INFO", info)
        const flag = await Vue.$api.get('hasCoverPage', { interviewId })
        commit("SET_HAS_COVER_PAGE", flag)
    },

    async getLanguageInfo({ commit, rootState }) {
        const interviewId = rootState.route.params.interviewId
        const languageInfo = await Vue.$api.get('getLanguageInfo', { interviewId })
        commit("SET_LANGUAGE_INFO", languageInfo)
    },

    fetchEntity: batchedAction(async ({ commit, dispatch, rootState }, ids) => {
        const interviewId = rootState.route.params.interviewId
        const details = await Vue.$api.get('getEntitiesDetails', { interviewId: interviewId, ids: uniq(map(ids, "id")) } )
        dispatch("fetch", { ids, done: true })

        commit("SET_ENTITIES_DETAILS", {
            entities: details,
            lastActivityTimestamp: new Date()
        })
    }, "fetch", /* limit */ 100),

    answerSingleOptionQuestion({ state, rootState }, { answer, questionId }) {
        const storedAnswer = getAnswer(state, questionId)
        if(storedAnswer != null && storedAnswer.value == answer) return; // skip same answer on same question
        
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(questionId, 'answerSingleOptionQuestion', {interviewId, answer, questionId})
    },
    answerTextQuestion({ state, commit, rootState }, { identity, text }) {
        if(getAnswer(state, identity) == text) return; // skip same answer on same question

        commit("SET_ANSWER", {identity, answer: text}) // to prevent answer blinking in TableRoster
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(identity, 'answerTextQuestion', {interviewId, identity, text})
    },
    answerMultiOptionQuestion({ dispatch, rootState }, { answer, questionId }) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(questionId, 'answerMultiOptionQuestion', {interviewId, answer, questionId})
    },
    answerYesNoQuestion({ dispatch }, { questionId, answer }) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(questionId, 'answerYesNoQuestion', {interviewId, questionId, answer})
    },
    answerIntegerQuestion({ commit, rootState }, { identity, answer }) {
        commit("SET_ANSWER", {identity, answer: answer}) // to prevent answer blinking in TableRoster
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(identity, 'answerIntegerQuestion', {interviewId, identity, answer})
    },
    answerDoubleQuestion({ commit, rootState }, { identity, answer }) {
        commit("SET_ANSWER", {identity, answer: answer}) // to prevent answer blinking in TableRoster
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(identity, 'answerDoubleQuestion', {interviewId, identity, answer})
    },
    answerGpsQuestion({ dispatch, rootState }, { identity, answer }) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(identity, 'answerGpsQuestion', {interviewId, identity, answer})
    },
    answerDateQuestion({ state, rootState }, { identity, date }) {
        if(getAnswer(state, identity) == date) return; // skip answer on same question
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(identity, 'answerDateQuestion', {interviewId, identity, date})
    },
    answerTextListQuestion({ dispatch, rootState }, { identity, rows }) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(identity, 'answerTextListQuestion', {interviewId, identity, rows})
    },
    answerLinkedSingleOptionQuestion({ dispatch, rootState }, { questionIdentity, answer }) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(questionIdentity, 'answerLinkedSingleOptionQuestion', {interviewId, questionIdentity, answer})
    },
    answerLinkedMultiOptionQuestion({ dispatch, rootState }, { questionIdentity, answer }) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(questionIdentity, 'answerLinkedMultiOptionQuestion', {interviewId, questionIdentity, answer})
    },
    answerLinkedToListMultiQuestion({ dispatch, rootState }, { questionIdentity, answer }) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(questionIdentity, 'answerLinkedToListMultiQuestion', {interviewId, questionIdentity, answer})
    },
    answerLinkedToListSingleQuestion({ dispatch, rootState }, { questionIdentity, answer }) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(questionIdentity, 'answerLinkedToListSingleQuestion', {interviewId, questionIdentity, answer})
    },
    answerMultimediaQuestion({ dispatch, rootState }, { id, file }) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(id, 'answerPictureQuestion', {interviewId, id, file})
    },
    answerAudioQuestion({ dispatch, rootState }, { id, file }) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(id, 'answerAudioQuestion', {interviewId, id, file})
    },
    answerQRBarcodeQuestion({ dispatch, rootState }, { identity, text }) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(identity, 'answerQRBarcodeQuestion', {interviewId, identity, text})
    },
    removeAnswer({ dispatch, rootState }, questionId) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.answer(questionId, 'removeAnswer', {interviewId, questionId})
    },
    async sendNewComment({ dispatch, commit, rootState }, { questionId, comment }) {
        commit("POSTING_COMMENT", { questionId: questionId })
        const interviewId = rootState.route.params.interviewId
        await Vue.$api.post('sendNewComment', {interviewId, questionId, comment})
    },
    async resolveComment({ dispatch, rootState }, { questionId, commentId }) {
        const interviewId = rootState.route.params.interviewId
        await Vue.$api.post('resolveComment', {interviewId, questionId})
    },

    setAnswerAsNotSaved({ commit }, { id, message }) {
        commit("SET_ANSWER_NOT_SAVED", { id, message })
    },

    clearAnswerValidity({ commit }, { id }) {
        commit("CLEAR_ANSWER_VALIDITY", { id })
    },

    // called by server side. reload interview
    reloadInterview() {
        location.reload(true)
    },

    // called by server side. navigate to finish page
    finishInterview() { },

    navigeToRoute() { },

    closeInterview({ dispatch }) {
        modal.alert({
            title: Vue.$t("WebInterviewUI.CloseInterviewTitle"),
            message: Vue.$t("WebInterviewUI.CloseInterviewMessage"),
            callback: () => {
                dispatch("reloadInterview")
            },
            onEscape: false,
            closeButton: false,
            buttons: {
                ok: {
                    label: Vue.$t("WebInterviewUI.Reload"),
                    className: "btn-success"
                }
            }
        })
    },

    shutDownInterview({ state, commit }) {
        if (!state.interviewShutdown) {
            commit("SET_INTERVIEW_SHUTDOWN")
            window.close();
        }
    },

    // called by server side. refresh
    refreshEntities({ state, dispatch, getters }, questions) {
        questions.forEach(id => {
            if (state.entityDetails[id]) { // do not fetch entity that is no in the visible list
                dispatch("fetchEntity", { id, source: "server" })
            }
        })

        dispatch("refreshSectionState", null)

        if (getters.isReviewMode) {
            dispatch("refreshReviewSearch");
        }
    },

    refreshReviewSearch: debounce(({dispatch}) => {
        dispatch("fetchSearchResults")
    }, 200),

    refreshSectionState({ commit, dispatch }) {
        commit("SET_LOADING_PROGRESS", true);
        dispatch("_refreshSectionState");
    },

    _refreshSectionState: debounce(({ dispatch, commit }) => {
        try {
            dispatch("fetchSectionEnabledStatus");
            dispatch("fetchBreadcrumbs");
            dispatch("fetchEntity", { id: "NavigationButton", source: "server" });
            dispatch("fetchSidebar");
            dispatch("fetchInterviewStatus");
        } finally {
            commit("SET_LOADING_PROGRESS", false);
        }
    }, 200),

    fetchSectionEntities: debounce(async ({ dispatch, commit, rootState }) => {
        const sectionId = rootState.route.params.sectionId
        const interviewId = rootState.route.params.interviewId

        const id = sectionId
        const isPrefilledSection = id === undefined

        if (isPrefilledSection) {
            const prefilledPageData = await Vue.$api.get('getPrefilledEntities', { interviewId })
            if (!prefilledPageData.hasAnyQuestions) {
                const loc = {
                    name: "section",
                    params: {
                        interviewId: interviewId,
                        sectionId: prefilledPageData.firstSectionId
                    }
                }

                dispatch("navigeToRoute", loc)
            } else {
                commit("SET_SECTION_DATA", prefilledPageData.entities)
            }
        } else {
            try {
                commit("SET_LOADING_PROGRESS", true)

                const section = await Vue.$api.get('getFullSectionInfo', {interviewId, id})

                commit("SET_SECTION_DATA", section.entities)
                commit("SET_ENTITIES_DETAILS", {
                    entities: section.details,
                    lastActivityTimestamp: new Date()
                })
            } finally {
                commit("SET_LOADING_PROGRESS", false)
            }
        }
    }, 200),

    fetchSectionEnabledStatus: debounce(async ({ dispatch, state, rootState }) => {
        const sectionId = rootState.route.params.sectionId
        const interviewId = rootState.route.params.interviewId

        const isPrefilledSection = sectionId === undefined

        if (!isPrefilledSection) {
            const isEnabled = await Vue.$api.get('isEnabled', {interviewId, sectionId})
            if (!isEnabled) {
                const firstSectionId = state.firstSectionId
                const firstSectionLocation = {
                    name: "section",
                    params: {
                        interviewId: interviewId,
                        sectionId: firstSectionId
                    }
                }

                dispatch("navigeToRoute", firstSectionLocation)
            }
        }
    }, 200),

    fetchBreadcrumbs: debounce(async ({ commit, rootState }) => {
        const interviewId = rootState.route.params.interviewId
        const sectionId = rootState.route.params.sectionId
        const crumps = await Vue.$api.get('getBreadcrumbs', {interviewId, sectionId})
        commit("SET_BREADCRUMPS", crumps)
    }, 200),

    fetchCompleteInfo: debounce(async ({ commit, rootState }) => {
        const interviewId = rootState.route.params.interviewId
        const completeInfo = await Vue.$api.get('getCompleteInfo', {interviewId})
        commit("SET_COMPLETE_INFO", completeInfo)
    }, 200),

    fetchInterviewStatus: debounce(async ({ commit, rootState }) => {
        const interviewId = rootState.route.params.interviewId
        const interviewState = await Vue.$api.get('getInterviewStatus', {interviewId})
        commit("SET_INTERVIEW_STATUS", interviewState)
    }, 200),

    fetchCoverInfo: debounce(async ({ commit, rootState }) => {
        const interviewId = rootState.route.params.interviewId
        const coverInfo = await Vue.$api.get('getCoverInfo', {interviewId})
        commit("SET_COVER_INFO", coverInfo)
    }, 200),

    completeInterview({ state, commit, rootState }, comment) {
        if (state.interviewCompleted) return;

        commit("COMPLETE_INTERVIEW");

        const interviewId = rootState.route.params.interviewId
        Vue.$api.post('completeInterview', {interviewId, comment})
    },

    cleanUpEntity: batchedAction(({ commit }, ids) => {
        commit("CLEAR_ENTITIES", { ids })
    }, null, /* limit */ 100),

    changeLanguage({ rootState }, language) {
        const interviewId = rootState.route.params.interviewId
        Vue.$api.post('changeLanguage', {interviewId, language})
    },

    stop() {
        Vue.$api.stop()
    },

    changeSection(ctx, sectionId) {
        return Vue.$api.setState((state) => state.sectionId = sectionId)
    }
}
