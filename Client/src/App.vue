<template>
    <div id="app">
        <div v-if="$route.name != 'login'">
            <loader/>
            <NavBar><router-view slot="content" /></NavBar>
            <md-content>

            </md-content>
        </div>
        <div v-else>
            <router-view slot="content" />
        </div>

    </div>
</template>

<script>
    import NavBar from './layout/Navbar.vue'
    import loader  from "./components/Loader.vue";

    export default {
        components: {
            NavBar,
            loader
        }
    }
</script>

<style>
    @import url("https://fonts.googleapis.com/css?family=Material+Icons");

    .style-choser .vs__dropdown-toggle{
        border: none;
    }

    /* .spinner { 
        height: 105px; 
        width: 105px;
        overflow: hidden;
    }

    .spinner img { 
        animation: spin-cycle 1900ms steps(46) infinite; 
        width:105px;
        height: auto;
    }

    @keyframes spin-cycle { 
        0% {transform: translateY(0px); } 
        100% {transform: translateY(-100%); } 
    }

    .spinner {
        position: absolute;
        top: 50%; left: 50%;
        margin: (105px*-.5) 0 0 (105px*-.5);
    } */

</style>
