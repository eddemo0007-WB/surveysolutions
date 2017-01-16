import { apiCaller } from "../api"
import { prefilledSectionId } from "./../config"
import router from "./../router"

export default {
    async loadQuestionnaire({ commit }, questionnaireId) {
        const questionnaireInfo = await apiCaller<IQuestionnaireInfo>(api => api.questionnaireDetails(questionnaireId))
        commit("SET_QUESTIONNAIRE_INFO", questionnaireInfo);
    },
    async startInterview({ commit }, questionnaireId: string) {
        const interviewId = await apiCaller(api => api.createInterview(questionnaireId)) as string;
        const loc = { name: "prefilled", params: { interviewId } };
        router.push(loc)
    },
    async fetchEntity({ commit }, { id }) {
        const entityDetails = await apiCaller(api => api.getEntityDetails(id))
        commit("SET_ENTITY_DETAILS", entityDetails);
    },
    async loadSection({ commit }, sectionId) {
        if (sectionId == null) {
            // tslint:disable-next-line:no-string-literal
            sectionId = router.currentRoute.params["sectionId"] || "prefilled"
        }
        const section = await apiCaller(api => api.getSectionDetails(sectionId))
        commit("SET_SECTION_DATA", section)
    },
    answerSingleOptionQuestion({ }, answerInfo) {
        apiCaller(api => api.answerSingleOptionQuestion(answerInfo.answer, answerInfo.questionId))
    },
    answerTextQuestion({ }, entity) {
        apiCaller(api => api.answerTextQuestion(entity.identity, entity.text))
    },
    answerMutliOptionQuestion({ commit }, answerInfo) {
        apiCaller(api => api.answerMutliOptionQuestion(answerInfo.answer, answerInfo.questionId))
    },
    answerIntegerQuestion({ }, entity) {
        apiCaller(api => api.answerIntegerQuestion(entity.identity, entity.answer))
    },
    answerDoubleQuestion({ }, entity) {
        apiCaller(api => api.answerDoubleQuestion(entity.identity, entity.answer))
    },
    removeAnswer({ }, questionId: string) {
        apiCaller(api => api.removeAnswer(questionId))
    },
    setAnswerAsNotSaved({commit}, entity) {
        commit("SET_ANSWER_NOT_SAVED", entity)
    }
}
