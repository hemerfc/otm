<template>
    <div class="container">
        <div v-for="(dataPoint,index) in dataPoints" :key="index">
            <md-card class="md-layout-item md-size-90 md-small-size-100 mt-1 md-elevation-14">
                <md-card-header>
                    <div class="md-title">{{dataPoint.name}}</div>
                </md-card-header>

                <md-card-content>
                    <div class="md-layout">

                        <div class="md-layout-item md-size-50 md-small-size-100 ">
                            <md-field>
                                <label>Nome</label>
                                <md-input name="Name" :value=dataPoint.name id="Name" disabled />
                            </md-field>
                        </div>

                        <div class="md-layout-item md-size-25 md-small-size-100">
                            <md-field>
                                <label>Driver</label>
                                <md-input name="Driver" :value=dataPoint.driver id="driver" disabled />
                            </md-field>
                        </div>

                        <div class="md-layout-item md-size-25 md-small-size-100">
                            <md-field>
                                <md-input name="debugMessages" :value=dataPoint.debugMessages id="debugMessages" disabled />
                            </md-field>
                        </div>

                    </div>
                    <div class="md-layout md-gutter">
                        <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                            <md-field>
                                <label>Conexão com o Banco de dados</label>
                                <md-input name="Config" :value=dataPoint.config id="Config" disabled />
                            </md-field>
                        </div>
                    </div>

                </md-card-content>

                <md-card-actions class="md-bottom-right">
                    <div>
                        <md-button class="md-raised md-primary" @click="edit(index)"><md-icon>edit</md-icon></md-button>
                        <md-button class="md-raised md-accent" @click="deleteDataPoint(dataPoint.id)"><md-icon>delete</md-icon></md-button>
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
                    <dataPointForm :edit="dataEdit" ref="dataPointForm"/>
                </template>

                <template slot="footer">
                    <md-button class="md-raised" @click="modal = false">Fechar</md-button>
                    <md-button class="md-raised md-primary" @click="submit">Salvar</md-button>
                </template>
            </modal>
    </div>
</template>

<script>
    import { modal } from "@/components";
    import axios from 'axios';
    import dataPointForm from './form.vue'

    export default {
        components: {
            modal,
            dataPointForm
        },
        data: () => ({

            dataPoints: [],
            dataEdit: null,
            modal: false,
            titleModal:null
        }),
        methods: {
            getAllDataPoint() {
                axios
                    .get('/api/DataPoint?name='+this.$route.params.context)
                    .then(response => {
                        this.dataPoints = response.data
                    })
            },
            create() {
                this.titleModal = "Inserir"
                this.dataEdit = null;
                this.modal = true;              
            },
            edit(index) {
                this.titleModal = "Editar"
                this.dataEdit = this.dataPoints[index]
                this.modal = true
            },
            deleteDataPoint(id) {
                this.Loader.showLoader = true;
                axios
                    .post('/api/DataPoint/Delete',{id:id,ContextName:this.$route.params.context},{
                        headers:{
                            'Content-Type': 'application/json',
                        }
                    })
                    .then(response => {
                        this.Loader.showLoader = false;
                        if(response.data.result){
                            this.$swal({
                                position: 'top-end',
                                icon: 'success',
                                title: 'Deletado com sucesso...',
                                timer: 3500,
                                willClose: () => {
                                    location.reload();
                                }
                            })
                        }else{
                            this.$swal({
                                position: 'top-end',
                                icon: 'error',
                                title: 'Falha ao deletar...',
                                timer: 3500
                            })
                        }
                    })
            },
            submit(){
                this.$refs.dataPointForm.Submit()
            }
        },
        mounted(){
            this.getAllDataPoint();            
        },
        watch: {
            '$route.params.context': function (){
                this.getAllDataPoint()
            }
        },
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
</style>