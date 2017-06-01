import * as debounce from "lodash/debounce"
import * as forEach from "lodash/foreach"
import * as groupBy from "lodash/groupby"
import Vue from "vue"
import { apiCaller } from "../api"
import { safeStore } from "../errors"
import { batchedAction } from "../helpers"

declare interface ISidebarState {
    panels: ISidebarPanel[],
    sidebarHidden: boolean
}

export default safeStore({
    state: {
        panels: {
            // organized by parentId, that way it easier to search and request data
            // sectionId1: [sectiondata1, sectiondata2, sectiondata3], root: [section1, section2], ... etc
        },
        sidebarHidden: false
    },

    actions: {

        fetchSidebar: batchedAction(async ({ commit }, ids) => {
            const sideBar = await apiCaller<ISidebar>(api => api.getSidebarChildSectionsOf(ids))
            commit("SET_SIDEBAR_STATE", sideBar)
        }, null, null),

        toggleSidebar({ commit, dispatch, state }, { panel, collapsed }) {
            commit("SET_SIDEBAR_TOGGLE", { panel, collapsed })

            if (collapsed === false) {
                dispatch("fetchSidebar", panel.id)
            }
        },
        toggleSidebarPanel({ commit, state }, newState = null): void {
            commit("SET_SIDEBAR_HIDDEN", newState == null ? !state.sidebarHidden : newState)
        }
    },

    mutations: {
        SET_SIDEBAR_STATE(state: ISidebarState, sideBar: ISidebar) {
            const byParentId = groupBy(sideBar.groups, "parentId")
            forEach(byParentId, (panels, id) => {
                Vue.set(state.panels, id, panels)
            })
        },
        SET_SIDEBAR_TOGGLE(state: ISidebarState, { panel, collapsed }) {
            panel.collapsed = collapsed
        },
        SET_SIDEBAR_HIDDEN(state, sidebarHidden: boolean) {
            state.sidebarHidden = sidebarHidden
        }
    },

    getters: {
        hasSidebarData(state: ISidebarState, getters) {
            return getters.rootSections.length > 0
        },
        rootSections(state: ISidebarState) {
            /* tslint:disable:no-string-literal */
            if (state.panels["null"]) {
                return state.panels["null"]
            }
            /* tslint:enable:no-string-literal */

            return []
        }
    }
})
