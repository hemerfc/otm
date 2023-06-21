<template>
    <div class="container">
            <div v-for="(scheluderDataPoint,index) in ScheluderDataPoint" :key="index">
                <md-card class="md-layout-item md-size-90 md-small-size-100 mt-1 md-elevation-14">
                    <md-card-header>
                        <div class="md-title">{{scheluderDataPoint.name}}</div>
                    </md-card-header>

                    <md-card-content>
                        <div class="md-layout">

                            <div class="md-layout-item md-size-50 md-small-size-100 ">
                                <md-field>
                                    <label>Nome</label>
                                    <md-input name="Name" :value=scheluderDataPoint.name id="Name" disabled />
                                </md-field>
                            </div>

                            <div class="md-layout-item md-size-25 md-small-size-100">
                                <md-field>
                                    <label>Driver</label>
                                    <md-input name="Driver" :value=scheluderDataPoint.driver id="driver" disabled />
                                </md-field>
                            </div>

                        </div>
                        <div class="md-layout md-gutter">
                            <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                                <md-field>
                                    <label>Conexão com o Banco de dados</label>
                                    <md-input name="Config" :value=scheluderDataPoint.config id="Config" disabled />
                                </md-field>
                            </div>
                        </div>

                    </md-card-content>

                    <md-card-actions class="md-bottom-right">
                        <div>
                            <md-button class="md-raised md-primary" @click="edit(index)"><md-icon>edit</md-icon></md-button>
                            <md-button class="md-raised md-accent" @click="deleteScheluderDataPoint(scheluderDataPoint.id)"><md-icon>delete</md-icon></md-button>
                        </div>
                    </md-card-actions>
                </md-card>
            </div>


            <md-button class="md-fab md-primary float" @click="create()">
                <md-icon>add</md-icon>
            </md-button>

            <modal v-if="modal" :modal="true">
                <template slot="header">
                    <span class="md-title">{{titleModal}}</span>
                </template>

                <template slot="body">                 
                    <scheluderDataPointForm :edit="dataEdit" ref="scheluderDataPointForm"/>
                </template>

                <template slot="footer">
                    <md-button class="md-raised" @click="modal = false">Fechar</md-button>
                    <md-button class="md-raised md-primary" @click="submit">Salvar</md-button>
                </template>
            </modal>
    </div>
</template>

<script>
    import axios from 'axios';
    import { modal } from "@/components";
    import scheluderDataPointForm from './form.vue'

    export default {
        components: {
            modal,
            scheluderDataPointForm
        },
        data: () => ({
            contexts: [],
            status: false,
            dataEdit: null,
            modal: false,
            titleModal:null,
            StateContext:false,
            ScheluderDataPoint:[]
        }),
        methods: {
            create() {
                this.titleModal = "Inserir"
                this.dataEdit = null;
                this.modal = true;              
            },
            edit(index) {
                this.titleModal = "Editar"
                this.dataEdit = this.ScheluderDataPoint[index]
                this.modal = true
            },
            submit(){
                this.$refs.scheluderDataPointForm.Submit()
            },
            stop() {
                this.Loader.showLoader = true;
                this.status = false;
                axios
                    .get('/api/Worker/Stop')
                    .then(response => {
                        this.Loader.showLoader = false;
                        this.contexts = response.data
                    })
            },
            start() {
                this.Loader.showLoader = true;
                this.status = true;
                axios
                    .get('/api/Worker/Start')
                    .then(response => {
                        this.Loader.showLoader = false;
                        this.contexts = response.data
                    })
            },
            save(){
                this.Loader.showLoader = true;
                axios
                    .post('/api/Worker/PostWorker',JSON.stringify(this.form),{
                        headers:{
                            'Content-Type': 'application/json',
                        }
                    })
                    .then(response => {
                        this.Loader.showLoader = false;
                        this.storedProcedures = response.data
                        if(this.edit){
                            let valorStoredProcedure =  this.storedProcedures.find(e => e.name == this.edit.name);
                            this.form.Name =  valorStoredProcedure.object_id;
                        }
                    })
            },
            getAllScheluderDataPoint() {
                axios
                    .get('/api/DataPoint?name='+this.$route.params.context)
                    .then(response => {
                        this.ScheluderDataPoint = response.data
                    })
            },
        },
        computed: {
            codemirror() {
            return this.$refs.cmEditor.codemirror
            }
        },
        mounted() {
            this.getAllScheluderDataPoint();
        }
    }

</script>

<style scoped>
    .md-layout-item {
        padding-left: 15px;
        padding-right: 15px;
    }

    .mt-1 {
        margin-top: 15px;
    }

    .mf-1 {
        margin-left: 15px;
    }

    .float {
        position: fixed;
        width: 60px;
        height: 60px;
        bottom: 40px;
        right: 40px;
        color: #FFF;
        border-radius: 50px;
        text-align: center;
        font-size: 30px;
        box-shadow: 2px 2px 3px #999;
        z-index: 100;
    }

    .md-radio {
        display: flex;
    }   

    .play{
        background-color: #06BE51 !important;
    }
</style>