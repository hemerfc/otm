<template>
    <div class="container">
        <div v-for="(device,index) in devices" :key="index">
            <md-card class="md-layout-item md-size-90 md-small-size-100 mt-1 md-elevation-14">
                <md-card-header>
                    <div class="md-title">{{device.name}}</div>
                </md-card-header>

                <md-card-content>
                    <div class="md-layout">

                        <div class="md-layout-item md-size-50 md-small-size-100 ">
                            <md-field>
                                <label>Nome</label>
                                <md-input name="Name" :value=device.name id="Name" disabled />
                            </md-field>
                        </div>

                        <div class="md-layout-item md-size-25 md-small-size-100">
                            <md-field>
                                <label>Driver</label>
                                <md-input name="Driver" :value=device.driver id="driver" disabled />
                            </md-field>
                        </div>

                    </div>
                    <div class="md-layout">
                        <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                            <md-field>
                                <label>Conexão com o PTL</label>
                                <md-input name="Config" :value=device.config id="Config" disabled />
                            </md-field>
                        </div>
                    </div>

                </md-card-content>

                <md-card-actions class="md-bottom-right">
                    <div>
                        <md-button class="md-raised md-primary" @click="edit(index)"><md-icon>edit</md-icon></md-button>
                        <md-button class="md-raised md-accent" @click="deleteDevice(device.id)"><md-icon>delete</md-icon></md-button>
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
                    <deviceForm :edit="dataEdit" ref="deviceForm"/>
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
    import deviceForm from './form.vue'

    export default {
        components: {
            modal,
            deviceForm
        },
        data: () => ({
            devices: [],
            deviceEdit: null,
            modal: false,
            titleModal:null
        }),
        methods: {
            getAllDevice() {
                axios
                    .get('/api/Device?name='+this.$route.params.context)
                    .then(response => {
                        this.devices = response.data
                    })
            },
            create() {
                this.titleModal = "Inserir"
                this.dataEdit = null;
                this.modal = true;              
            },
            edit(index) {
                this.titleModal = "Editar"
                this.dataEdit = this.devices[index]
                this.modal = true
            },
            deleteDevice(Id) {
                this.Loader.showLoader = true;
                axios
                    .post('/api/Device/Delete',{Id:Id,ContextName: this.$route.params.context},{
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
                this.$refs.deviceForm.Submit()
            }
        },
        mounted() {
            this.getAllDevice();            
        },
        watch: {
            '$route.params.context': function (){
                this.getAllDevice();
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