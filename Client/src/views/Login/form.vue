<template>
    <div>
        <form novalidate class="md-layout" @submit.prevent="validateForm">
            <div class="md-layout">
                <div class="md-layout-item md-size-100 md-small-size-100 mt-1">                          
                    <div class="md-layout">
                        <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                            <md-field :class="getValidationClass('User')">
                                <label>User</label>
                                <md-input name="User" id="User" autocomplete="Usuário de acesso" v-model="form.User" :disabled="sending" />
                                <span class="md-error" v-if="!$v.form.User.required">User é um campo obrigatório</span>
                                <span class="md-error" v-else-if="!$v.form.User.minlength">Tamanho Inválido</span>
                            </md-field>
                        </div>

                        <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                            <md-field :class="getValidationClass('Password')">
                                <label>Password</label>
                                <md-input name="Password" id="Password" autocomplete="Senha de acesso" v-model="form.Password" :disabled="sending" />
                                <span class="md-error" v-if="!$v.form.Password.required">Password é um campo obrigatório</span>
                                <span class="md-error" v-else-if="!$v.form.Password.minlength">Tamanho Inválido</span>
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
        required
    } from 'vuelidate/lib/validators';

    export default {
        mixins: [validationMixin],       
        data: () => ({
            form: {
                User:null,
                Password:null
            },
            loader: false,
        }),
        validations: {
            form: {
                User: {
                    required
                },
                Password: {
                    required
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
            Submit(){
                if(this.validateForm()){
                    this.Loader.showLoader = true;
                    let storedProcedure = this.storedProcedures.find(e => e.object_id == this.form.Name)
                    this.form.Config = "Server=" + this.form.Server +"; Database=" + this.form.Password +"; User ID="+ this.form.User +";Password="+ this.form.Password +";";
                    this.form.Params = this.Dynamic_params;
                    this.form.Name = !storedProcedure.name? this.form.Name : storedProcedure.name;
                    this.form.ContextName= this.$route.params.context;

                    axios
                        .post('/api/DataPoint',JSON.stringify(this.form),{
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
                                    text: "Falha ao cadastrar data point.",
                                })
                            }                           
                        });
                }
            }
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
