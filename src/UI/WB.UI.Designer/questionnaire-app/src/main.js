import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import { vuetify, i18n /*, install, i18n */ } from './plugins/vuetify';

import '../../questionnaire/content/markup.less';
//import 'bootstrap/dist/css/bootstrap.min.css';
import * as uiv from 'uiv';

import './directives';

/** Register Vue */
const pinia = createPinia();
const vue = createApp(App);
vue.use(router);
vue.use(pinia);
vue.use(vuetify); //reqired by options component. consider either remove or use.
vue.use(i18n);
vue.use(uiv);

//import { useQuestionnaireStore } from './store'
//vue.provide('store', useQuestionnaireStore)

// Run!
router.isReady().then(() => {
    vue.mount('#app');
});
