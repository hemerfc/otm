<template>
    <div class="container">
        <div v-for="(transaction,index) in Transactions" :key="index">
            <md-card class="md-layout-item md-size-90 md-small-size-100 mt-1 md-elevation-14">
                <md-card-header>
                    <div class="md-title">{{transaction.name}}</div>
                </md-card-header>

                <md-card-content>
                    <div class="md-layout">

                        <div class="md-layout-item md-size-50 md-small-size-100 ">
                            <md-field>
                                <label>Nome</label>
                                <md-input name="Name" :value=transaction.name id="Name" disabled />
                            </md-field>
                        </div>

                        <div class="md-layout-item md-size-25 md-small-size-100">
                            <md-field>
                                <label>Data Point Name</label>
                                <md-input name="DataPointName" :value=transaction.dataPointName id="DataPointName" disabled />
                            </md-field>
                        </div>

                    </div>

                </md-card-content>

                <md-card-actions class="md-bottom-right">
                    <div>
                        <md-button class="md-raised md-primary" @click="edit(index)"><md-icon>edit</md-icon></md-button>
                        <md-button class="md-raised md-accent" @click="deleteTransaction(transaction.id)"><md-icon>delete</md-icon></md-button>
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
                    <TransactionForm :edit="dataEdit" ref="TransactionForm"/>
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
    import TransactionForm from './form.vue'

    export default {
        components: {
            modal,
            TransactionForm
        },
        data: () => ({
            Transactions: [],
            dataEdit: null,
            modal: false,
            titleModal:null
        }),
        methods: {
            getAllTransaction() {
                axios
                    .get('/api/Transaction?name='+this.$route.params.context)
                    .then(response => {
                        this.Transactions = response.data
                    })
            },
            create() {
                this.titleModal = "Inserir"
                this.dataEdit = null;
                this.modal = true;              
            },
            edit(index) {
                this.titleModal = "Editar"
                this.dataEdit = this.Transactions[index]
                this.modal = true
            },
            deleteTransaction(Id) {
                this.Loader.showLoader = true;
                axios
                    .post('/api/Transaction/Delete',{Id:Id,ContextName:this.$route.params.context},{
                        headers:{
                            'Content-Type': 'application/json',
                        }
                    })
                    .then(response => {
                        this.Loader.showLoader = false;
                        if(response.data.resutl){
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
                this.$refs.TransactionForm.Submit()
            }
        },
        mounted() {
            this.getAllTransaction();            
        },
        watch: {
            '$route.params.context': function (){
                this.getAllTransaction();
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