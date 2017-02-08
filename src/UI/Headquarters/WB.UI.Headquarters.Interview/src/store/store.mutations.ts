import * as forEach from "lodash/foreach"
import * as Vue from "vue"
import * as Vuex from "vuex"

export default {
    SET_ENTITIES_DETAILS(state, {entities, lastActivityTimestamp}) {
        state.lastActivityTimestamp = lastActivityTimestamp

        forEach(entities, entity => {
            if (entity != null) {
                Vue.set(state.entityDetails, entity.id, entity)
            }
        })
    },
    SET_SECTION_DATA(state, sectionData: IInterviewEntityWithType[]) {
        state.entities = sectionData
    },
    CLEAR_ENTITY(state, id) {
        Vue.delete(state.entityDetails, id)
    },
    SET_ANSWER_NOT_SAVED(state, { id, message }) {
        const validity = state.entityDetails[id].validity
        Vue.set(validity, "errorMessage", true)
        validity.messages = [message]
        validity.isValid = false
    },
    SET_BREADCRUMPS(state, crumps) {
        Vue.set(state, "breadcrumbs", crumps)
    },
    SET_LANGUAGE_INFO(state, languageInfo) {
        Vue.set(state, "originalLanguageName", languageInfo.originalLanguageName)
        Vue.set(state, "currentLanguage", languageInfo.currentLanguage)
        Vue.set(state, "languages", languageInfo.languages)
    },
    SET_INTERVIEW_INFO(state, interviewInfo) {
        Vue.set(state, "questionnaireTitle", interviewInfo.questionnaireTitle)
        Vue.set(state, "interviewId", interviewInfo.interviewId)
        Vue.set(state, "firstSectionId", interviewInfo.firstSectionId)
    },
    SET_COMPLETE_INFO(state, completeInfo) {
        Vue.set(state, "completeInfo", completeInfo)
    },
}
