import Vue from 'vue'
import Router from 'vue-router'
import Context from './views/Context/ContextView.vue'
import DataPoint from './views/DataPoint/DataPointView.vue'
import ExecuteDataPoint from './views/DataPoint/ExecuteDataPointView.vue'
import Devices from './views/Devices/DevicesView.vue'
import Transactions from './views/Transactions/TransactionsView.vue'
import Workers from './views/Workers/WorkersView.vue'

Vue.use(Router);

export default new Router({
    routes: [
        // dynamic segments start with a colon
        {
            path: '/context',
            name: 'context',
            component: Context
        },
        {
            path: '/dataPoint/:context',
            name: 'dataPoint',
            component: DataPoint
        },
        {
            path: '/executeDataPoint',
            name: 'executeDataPoint',
            component: ExecuteDataPoint
        },
        {
            path: '/devices/:context',
            name: 'devices',
            component: Devices
        },
        {
            path: '/transactions/:context',
            name: 'transactions',
            component: Transactions
        },
        {
            path: '/workers/:context',
            name: 'workers',
            component: Workers
        },
    ]
})