import Vue from 'vue'

import Typeahead from './components/Typeahead'
import Layout from "./components/Layout"
import Filters from "./components/Filters"
import FilterBlock from "./components/FilterBlock"
import DataTables from "./components/DataTables"
import ModalFrame from "./components/ModalFrame"
import Confirm from './components/Confirm'
import locale from "./plugins/locale"
import common_store from "./store"

Vue.use(locale)

Vue.component("Layout", Layout)
Vue.component("Filters", Filters)
Vue.component("FilterBlock", FilterBlock)
Vue.component("Typeahead", Typeahead)
Vue.component("DataTables", DataTables)
Vue.component("ModalFrame", ModalFrame)
Vue.component("Confirm", Confirm)

export default ({ app, options}) => {
    const store = require("./store").default

    const vueApp = new Vue(_.assign({
        el: "#vueApp",
        render: h => h(app),
        components: { app },
        store
    }, options));
}
