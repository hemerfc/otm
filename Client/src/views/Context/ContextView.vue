<template>
    <div class="container">
        <div v-for="(context,index) in contexts" :key="index">
            <md-card class="md-layout-item md-size-90 md-small-size-100 mt-1 md-elevation-14">
                <md-card-header>
                    <div class="md-title">{{context.name}}</div>
                </md-card-header>

                <md-card-content>
                    <div class="md-layout">

                        <!-- <div class="md-layout-item md-size-50 md-small-size-100 ">
                            <md-field>
                                <label>Nome</label>
                                <md-input name="Name" :value=context.name id="Name" disabled />
                            </md-field>
                        </div> -->

                        <!-- <div class="md-layout-item md-size-25 md-small-size-100">
                            <md-field>
                                <label>Status</label>
                                <md-input :value=context.status disabled />
                            </md-field>
                        </div> -->

                        <div class="md-layout-item md-size-100 md-small-size-100">
                            <md-switch v-model="context.status" class="md-primary" @change="alterContextState(context.name,context.status,index)">Estado do contexto <small v-if="context.status">(Ativo)</small><small v-else>(Inativo)</small></md-switch>
                        </div>                      
                    </div>
                </md-card-content>

                <md-card-actions class="md-bottom-right">
                    <div>
                        <!-- <md-button class="md-raised md-primary" @click="edit(index)"><md-icon>edit</md-icon></md-button> -->
                        <md-button class="md-raised md-accent" @click="deleteContext(context.name,index)"><md-icon>delete</md-icon></md-button>
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
                    <contextForm :edit="dataEdit" ref="dataPointForm"/>
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
    import contextForm from './form.vue'

    export default {
        components: {
            modal,
            contextForm
        },
        data: () => ({
            contexts: [],
            dataEdit: null,
            modal: false,
            titleModal:null,
            StateContext:false
        }),
        methods: {
            getAllContext() {
                this.Loader.showLoader = true;
                axios
                    .get('/api/Context')
                    .then(response => {
                        this.Loader.showLoader = false;
                        this.contexts = response.data
                    })
            },
            create() {
                this.titleModal = "Inserir"
                this.dataEdit = null;
                this.modal = true;              
            },
            edit(index) {
                this.titleModal = "Editar"
                this.dataEdit = this.contexts[index]
                this.modal = true
            },
            deleteContext(name,index) {
                if(this.contexts[index].status){
                    this.$swal({
                        position: 'top-end',
                        icon: 'error',
                        title: 'Falha ao deletar...',
                        text: 'Impossível deletar contexto que está ativo!',
                        timer: 3500
                    })
                }else{
                    this.Loader.showLoader = true;
                    axios
                        .post('/api/Context/Delete',{Name:name},{
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
                }
            },
            alterContextState(name,status,index){
                this.Loader.showLoader = true;
                let method = "";
                if(status){
                    method = "ActivedContext"
                }else{
                    method = "disableContext"
                }

                axios
                    .post('/api/Context/' + method,{Name:name},{
                        headers:{
                            'Content-Type': 'application/json',
                        }
                    })
                    .then(response => {
                        this.Loader.showLoader = false;
                        if(response.data.result == false){
                            this.contexts[index].status = false
                            this.$swal({
                                position: 'top-end',
                                icon: 'error',
                                title: 'Falha ao alterar contexto.',
                                text: response.data.message,
                            })
                        }
                    });
            },
            submit(){
                this.$refs.dataPointForm.Submit()
            }
        },
        mounted(){
            this.getAllContext();            
        },
        watch: {
            '$route.params.context': function (){
                this.getAllContext()
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