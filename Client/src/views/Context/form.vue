<template>
    <div>
        <form novalidate class="md-layout" @submit.prevent="validateForm">
            <div class="md-layout">
                <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                    <div class="md-layout"> 

                        <div class="md-layout-item md-size-100 md-small-size-100 mf-1">
                            <md-field :class="getValidationClass('Server')">
                                <label>Nome</label>
                                <md-input name="name" id="name" v-model="form.Name" :disabled="sending" />
                                <span class="md-error" v-if="!$v.form.Name.required">nome é um campo obrigatório</span>
                                <span class="md-error" v-else-if="!$v.form.Name.minlength">Tamanho Inválido</span>
                            </md-field>
                        </div>

                        <div class="md-layout-item md-size-100 md-small-size-100">
                            <md-field :class="getValidationClass('Mode')">
                                <select v-model="form.Mode" id="Mode" class="md-input">
                                    <option value="DataPoint" selected>Data Point</option>
                                    <option value="ScheluderDataPoint">Scheluder Data Point</option>
                                </select>
                            </md-field>
                        </div>

                        <div class="md-layout-item md-size-100 md-small-size-100 mf-1">
                            <md-field :class="getValidationClass('Logger')">
                                <label>Logger</label>
                                <md-input name="Logger" id="Logger" autocomplete="Nome do Logger" v-model="form.Logger" :disabled="sending" />
                                <span class="md-error" v-if="!$v.form.Logger.required">Logger é um campo obrigatório</span>
                                <span class="md-error" v-else-if="!$v.form.Logger.minlength">Tamanho Inválido</span>
                            </md-field>
                        </div>
                    </div>
                </div>
            </div>
        </form>
    </div>
</template>

<script>
    // import { loader } from "@/components";
    import { validationMixin } from 'vuelidate';
    import axios from 'axios';
    import {
        required,
        minLength
    } from 'vuelidate/lib/validators';

    export default {
        mixins: [validationMixin],
        props: ['edit'],       
        data: () => ({
            form: {
                Name: null,
                Enabled: false,
                Mode: "DataPoint",
                Logger: null
            },
            loader: false,
            sending: false

        }),
        validations: {
            form: {
                Name: {
                    required,
                    minLength: minLength(3)
                },
                Logger: {
                    required,
                    minLength: minLength(2)
                }
            }

        },
        methods: { 
            getValidationClass(fieldName) {
                const field = this.$v.form[fieldName]

                if (field) {
                    return {
                        'md-invalid': field.$invalid && field.$dirty
                    }
                }
            },
            validateForm() {
                this.$v.$touch()

                if (!this.$v.$invalid) {
                    return true;
                }
            },
            EditProps(){
                if(this.edit){
                    this.form.Name =  this.edit.name;
                    this.form.Enabled =  this.edit.status;
                    this.form.Mode =  this.edit.mode;
                    this.form.Logger =  this.edit.logger;
                }
            },
            Submit(){
                if(this.validateForm()){
                    this.Loader.showLoader = true;
                    axios
                        .post('/api/Context',JSON.stringify(this.form),{
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
                                    title: 'Sucesso...',
                                    text: "Cadastrado com sucesso.",
                                    willClose: () => {
                                        location.reload();
                                    }
                                })
                            }else{
                                this.$swal({
                                    position: 'top-end',
                                    icon: 'error',
                                    title: 'Erro...',
                                    text: "Falha ao cadastrar context.",
                                })
                            }                           
                        });
                }
            },
        },
        created: function(){
            this.EditProps();
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

    .mt-2 {
        margin-top: 30px;
    }

    .mf-1 {
        margin-left: 15px;
    }

    .style-choser{
        height: 35px !important;
    }
</style>
