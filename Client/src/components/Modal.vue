<template>
    <div>
        <md-dialog :md-active.sync="modal">
            <md-dialog-title><slot name="header"></slot></md-dialog-title>

            <md-divider></md-divider>

            <md-dialog-content>
                <slot name="body"></slot>
            </md-dialog-content>

            <md-divider></md-divider>

            <md-dialog-actions class="mt-1">
                <slot name="footer"></slot>
            </md-dialog-actions>
        </md-dialog>
    </div>
</template>

<script>
    export default {
        name: "modal",
        props: ['modal'],
        data() {
            return {
                event: false,
            }
        },
        methods: {

        },
    };
</script>

<style scoped>
    .md-layout-item {
        padding-left: 15px;
        padding-right: 15px;
    }

    .mt-1 {
        margin-top: 25px;
    }
</style>