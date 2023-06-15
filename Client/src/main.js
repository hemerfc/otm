import Vue from 'vue'
import App from './App.vue'
import VueMaterial from 'vue-material'
import 'vue-material/dist/vue-material.min.css'
import 'vue-material/dist/theme/default.css'
import VueRouter from './router'
import VueAxios from 'vue-axios'
import VueSweetalert2 from 'vue-sweetalert2';
import 'sweetalert2/dist/sweetalert2.min.css';

import Element from 'element-ui'
import 'element-ui/lib/theme-chalk/index.css'
import 'vue-cron-generator/src/styles/global.css'
import i18n from './lang' // Internationalization


import { library } from '@fortawesome/fontawesome-svg-core'
import { fas  } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'

library.add(fas)
Vue.component('font-awesome-icon', FontAwesomeIcon)

Vue.config.productionTip = false
const Loader = {
  showLoader: false
};

const Auth = {
  loggedIn: false
}

Vue.mixin({ 
  data(){
    return{
      Loader,
      Auth
    }
  }
});

Vue.use(VueMaterial);
Vue.use(VueAxios);
Vue.use(VueSweetalert2);
Vue.use(Element, {
  size: localStorage.getItem('size') || 'small', // set element-ui default size
  i18n: (key, value) => i18n.t(key, value)
})

new Vue({
  router : VueRouter,
  i18n,
  render: h => h(App),
}).$mount('#app')
