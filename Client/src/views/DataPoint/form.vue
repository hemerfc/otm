<template>
    <div>
        <form novalidate class="md-layout" @submit.prevent="validateForm">
            <div class="md-layout">
                <md-tabs md-dynamic-height>
                    <md-tab md-label="Conexão com o banco de dados">
                        <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                            
                            <div class="md-layout">
                                
                                <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                    <md-field :class="getValidationClass('Driver')">
                                        <label v-if="form.Driver == null">Driver</label>
                                        <select name="Driver" id="Driver" v-model="form.Driver" class="md-input" :disabled="sending">
                                            <option value="mssql">mssql</option>
                                        </select>
                                        <span class="md-error">Driver é obrigatório</span>
                                    </md-field>
                                </div>

                                <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                    <md-field :class="getValidationClass('Server')">
                                        <label>Server</label>
                                        <md-input name="Server" id="Server" autocomplete="Server para se conectar com o banco de dados" v-model="form.Server" :disabled="sending" />
                                        <span class="md-error" v-if="!$v.form.Server.required">Server é um campo obrigatório</span>
                                        <span class="md-error" v-else-if="!$v.form.Server.minlength">Tamanho Inválido</span>
                                    </md-field>
                                </div>

                                <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                    <md-field :class="getValidationClass('DataBase')">
                                        <label>DataBase</label>
                                        <md-input name="DataBase" id="DataBase" autocomplete="Database deve ser descrito" v-model="form.DataBase" :disabled="sending" />
                                        <span class="md-error" v-if="!$v.form.DataBase.required">Server é um campo obrigatório</span>
                                        <span class="md-error" v-else-if="!$v.form.DataBase.minlength">Tamanho Inválido</span>
                                    </md-field>
                                </div>

                                <div class="md-layout-item md-size-45 md-small-size-100 mf-1">
                                    <md-field :class="getValidationClass('User')">
                                        <label>Usuário</label>
                                        <md-input name="User" id="User" autocomplete="Usuário para se conectar com o DataBase" v-model="form.User" :disabled="sending" />
                                        <span class="md-error" v-if="!$v.form.User.required">Usuário é um campo obrigatório</span>
                                        <span class="md-error" v-else-if="!$v.form.User.minlength">Tamanho Inválido</span>
                                    </md-field>
                                </div>

                                <div class="md-layout-item md-size-45 md-small-size-100 mf-1">
                                    <md-field :class="getValidationClass('Password')">
                                        <label>Senha</label>
                                        <md-input name="Password" type="password" id="Password" autocomplete="Password para se conectar com o DataBase" v-model="form.Password" :disabled="sending" />
                                        <span class="md-error" v-if="!$v.form.Password.required">Usuário é um campo obrigatório</span>
                                        <span class="md-error" v-else-if="!$v.form.Password.minlength">Tamanho Inválido</span>
                                    </md-field>
                                </div>
                            </div>
                            
                            
                            
                            <!--<md-button class="md-icon-button md-raised md-primary md-fixed">
                                -->  
                            <!-- </md-button>-->    
                            <div class="md-layout-item md-size-100 md-small-size-100 mt-1"> 
                                <md-button class="md-raised md-primary" @click="TestConnection " >Testar conexão</md-button>                        
                                <md-icon class="mt-1"><font-awesome-icon icon="database" :color="GetColorIconDatabase()" /></md-icon>
                            </div>

                        </div>
                         
                    </md-tab>

                    <md-tab md-label="General">
                        <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                            <div class="md-layout">
                                <div class="md-layout-item md-size-50 md-small-size-100">
                                    <md-field :class="getValidationClass('Name')">
                                        <v-select
                                            v-if="storedProcedures"
                                            placeholder="Selecione Stored Procedure"
                                            :reduce="data => data.object_id"
                                            :options="storedProcedures"
                                            label="name"
                                            v-model="form.Name"
                                            class="md-input style-choser">
                                        </v-select>
                                        <span class="md-error" v-if="!$v.form.Name.required">Nome é um campo obrigatório</span>
                                        <span class="md-error" v-else-if="!$v.form.Name.minlength">Tamanho Inválido</span>
                                    </md-field>
                                </div>

                                <div class="md-layout-item md-size-25 md-small-size-100">
                                    <label>Debug mensagens</label>
                                    <div class="flex-column">
                                        <md-radio v-model="form.DebugMessages" :value="true">Sim</md-radio>
                                        <md-radio v-model="form.DebugMessages" :value="false">Não</md-radio>
                                    </div>
                                </div>

                                <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                                    <md-divider></md-divider>
                                    <md-subheader>Parâmetros</md-subheader>
                                </div>

                                <div class="md-layout-item md-size-100 md-small-size-100 ">
                                    <div class="md-layout" v-for="(Dynamic_param,index) in $v.Dynamic_params.$each.$iter" :key="index">

                                        <div class="md-layout-item md-size-20 md-small-size-100">
                                            <md-field :class="getValidationDynamicParamsClass('Name',index)">
                                                <label>Nome</label>
                                                <md-input v-model.trim="Dynamic_param.Name.$model" autocomplete="Nome do parêmetro"/>
                                                <span class="md-error" v-if="!Dynamic_param.Name.required">Nome é um campo obrigatório</span>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-20 md-small-size-100">
                                            <md-field :class="getValidationDynamicParamsClass('Mode',index)">
                                                <label v-if="Dynamic_param.Mode.$model == null">Modo</label>
                                                <select v-model="Dynamic_param.Mode.$model" id="Mode" class="md-input">
                                                    <option value="FromOTM" selected>FromOTM</option>
                                                    <option value="ToOTM">ToOTM</option>
                                                </select>
                                                <span class="md-error" v-if="!Dynamic_param.Mode.required">Modo é um campo obrigatório</span>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-20 md-small-size-100">
                                            <md-field :class="getValidationDynamicParamsClass('TypeCode',index)">
                                                <label v-if="Dynamic_param.TypeCode.$model == null">Formato</label>
                                                <select  v-model="Dynamic_param.TypeCode.$model" id="TypeCode" class="md-input">
                                                    <option v-for="typeCode in TypeCodes" :key="typeCode.code" :value="typeCode.code">{{typeCode.name}}</option>
                                                </select>
                                                <span class="md-error" v-if="!Dynamic_param.TypeCode.required">Formato é um campo obrigatório</span>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-15 md-small-size-100">
                                            <md-field :class="getValidationDynamicParamsClass('Length',index)">
                                                <label>Tamanho</label>
                                                <md-input v-model="Dynamic_param.Length.$model" id="Length" autocomplete="Length do parêmetro"/>
                                                 <span class="md-error" v-if="!Dynamic_param.TypeCode.required">Tamanho é um campo obrigatório</span>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-10 md-small-size-100">
                                            <md-field :class="getValidationDynamicParamsClass('Direction',index)">
                                                <label v-if="Dynamic_param.Direction.$model == null">Direction</label>
                                                <select v-model="Dynamic_param.Direction.$model" id="Direction" class="md-input">
                                                    <option value="1" selected>Input</option>
                                                    <option value="2">Output</option>
                                                </select>
                                                <span class="md-error" v-if="!Dynamic_param.Mode.required">Direction é um campo obrigatório</span>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-10 md-small-size-100 mt-1">
                                            <md-button class="md-raised md-accent" id="remove_param" @click="RemoveParam(index)"><md-icon>remove</md-icon></md-button>                                              
                                        </div>                                   
                                    </div>
                                    <dynamicButton color="color-green" icon="add" format="md-raised" v-on:click.native="addParam"/>
                                    <md-button class="md-raised md-primary" @click="GetParams">Gerar Parâmetros automaticamente</md-button>
                                </div>
                            </div>
                        </div>
                    </md-tab>
                </md-tabs>

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
        requiredIf,
        minLength
    } from 'vuelidate/lib/validators';
    import { dynamicButton } from "@/components";
    import vSelect from "vue-select";
    import "vue-select/dist/vue-select.css";

    export default {
        mixins: [validationMixin],
        components: {
            dynamicButton,
            vSelect,
            // loader
        },
        props: ['edit'],       
        data: () => ({
            form: {
                Name: null,
                Driver: null,
                DebugMessages: false,
                Server:null,
                DataBase:null,
                User:null,
                Password:null,
                Config: null,
                Params: null,
            },
            Dynamic_params:[
                {
                    Name:null,
                    Mode:"FromOTM",
                    TypeCode:18,
                    Length:"10",
                    Direction:1
                }
            ],
            loader: false,
            sending: false,
            storedProcedures:[],
            connection:false,
            TypeCodes:[
                {
                    name: 'Empty',
                    code: 0
                },
                {
                    name: 'Object',
                    code: 1
                },
                {
                    name: 'DBNull',
                    code: 2
                },
                {
                    name: 'Boolean',
                    code: 3
                },
                {
                    name: 'Char',
                    code: 4
                },
                {
                    name: 'SByte',
                    code: 5
                },
                {
                    name: 'Byte',
                    code: 6
                },
                {
                    name: 'Int16',
                    code: 7
                },
                {
                    name: 'UInt16',
                    code: 8
                },
                {
                    name: 'Int32',
                    code: 9
                },
                {
                    name: 'UInt32',
                    code: 10
                },
                { 
                    name: 'Int64',
                    code: 11
                },
                {
                    name: 'UInt64',
                    code: 12
                },
                {
                    name: 'Single',
                    code: 13
                },
                {
                    name: 'Double',
                    code: 14
                },
                {
                    name: 'Decimal',
                    code: 15
                },
                {
                    name: 'DateTime',
                    code: 16
                },
                {
                    name: 'String',
                    code: 18
                }
            ]

        }),
        validations: {
            form: {
                Name: {
                    required:requiredIf(function(){
                        return this.connection == true
                    }),
                    minLength: minLength(3)
                },
                Driver: {
                    required
                },
                Server: {
                    required,
                    minLength: minLength(3)
                },
                DataBase: {
                    required,
                    minLength: minLength(3)
                },
                User: {
                    required,
                    minLength: minLength(2)
                },
                Password: {
                    required,
                    minLength: minLength(2)
                }
            },
            Dynamic_params:{
                $each: {
                    Name: {
                        required:requiredIf(function(){
                            return this.connection == true
                        }),
                        minLength: minLength(2)
                    },
                    Mode:{
                        required:requiredIf(function(){
                            return this.connection == true
                        }),
                    },
                    TypeCode:{
                        required:requiredIf(function(){
                            return this.connection == true
                        }),
                    },
                    Length:{
                        required:requiredIf(function(){
                            return this.connection == true
                        }),
                    },
                    Direction:{
                        required:requiredIf(function(){
                            return this.connection == true
                        }),
                    }
                },
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
            getValidationDynamicParamsClass(fieldName,index) {
                const field = this.$v.Dynamic_params.$each.$iter[index][fieldName]

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
            addParam () {
                this.Dynamic_params.push({
                    Name:null,
                    Mode:"FromOTM",
                    TypeCode:18,
                    Length:"10",
                    Direction:1
                })
            },
            RemoveParam (index) {
                if(index != 0){
                    this.Dynamic_params.splice(index,1)
                }
            },
            EditProps(){
                if(this.edit){
                    let conection_string = this.edit.config.split(';');

                    this.form.Id =  this.edit.id;
                    this.form.Driver =  this.edit.driver;
                    this.form.DebugMessages =  this.edit.debugMessages;
                    this.form.Server = conection_string[0].split('=').pop();
                    this.form.DataBase = conection_string[1].split('=').pop();
                    this.form.User = conection_string[2].split('=').pop();
                    this.form.Password = conection_string[3].split('=').pop();

                    this.TestConnection();
                    

                    this.Dynamic_params.pop();
                    this.edit.params.forEach(element => {
                        let typeCode = this.TypeCodes.find(e => e.name == element.typeCode);
                        this.Dynamic_params.push({
                            Name: element.name,
                            Mode: element.mode,
                            TypeCode: typeCode.code,
                            Length: element.length,
                            Direction: !element.direction ? 1 : element.direction
                        })
                    });

                }
            },
            TestConnection(){
                if(this.validateForm()){
                    this.Loader.showLoader = true;
                    let data = JSON.stringify({
                        Config: "Server=" + this.form.Server +"; Database=" + this.form.DataBase +"; User ID="+ this.form.User +";Password="+ this.form.Password +";"
                    })
    
                    axios
                        .post('/api/DataPoint/TestConnectionString',data,{
                            headers:{
                               'Content-Type': 'application/json',
                            }
                        })
                        .then(response => {
                            this.Loader.showLoader = false;
                            if( response.data == "Open"){
                                this.connection = true;
                                this.GetStoredProcedures();
                                this.$swal({
                                    position: 'top-end',
                                    icon: 'success',
                                    title: 'Conectado...',
                                    timer: 3500
                                })
                            }else{
                                this.$swal({
                                        position: 'top-end',
                                        icon: 'error',
                                        title: 'Erro de conexão...',
                                        text: response.data,
                                    })
                            }
                        })
                }
            },
            GetStoredProcedures(){
                let data = JSON.stringify({
                    Config: "Server=" + this.form.Server +"; Database=" + this.form.DataBase +"; User ID="+ this.form.User +";Password="+ this.form.Password +";"
                })

                axios
                    .post('/api/DataPoint/GetStoredProcedure',data,{
                        headers:{
                            'Content-Type': 'application/json',
                        }
                    })
                    .then(response => {
                        this.storedProcedures = response.data
                        if(this.edit){
                            let valorStoredProcedure =  this.storedProcedures.find(e => e.name == this.edit.name);
                            this.form.Name =  valorStoredProcedure.object_id;
                        }
                    })
            },
            GetParams(){
                this.Loader.showLoader = true;
                let data = JSON.stringify({
                    Config: "Server=" + this.form.Server +"; Database=" + this.form.DataBase +"; User ID="+ this.form.User +";Password="+ this.form.Password +";",
                    object_id: this.form.Name
                })

                axios
                    .post('/api/DataPoint/GetParamsStoredProcedure',data,{
                        headers:{
                            'Content-Type': 'application/json',
                        }
                    })
                    .then(response => {
                         this.Loader.showLoader = false;
                        if(response.data.length > 0){
                            this.Dynamic_params = [];
                            response.data.forEach(element => {
                                let typeCode = this.TypeCodes.find(e => e.name == element.type);
                                this.Dynamic_params.push({
                                    Name: element.name.replace('@',''),
                                    Mode: "FromOTM",
                                    TypeCode: !typeCode ? 18 : typeCode,
                                    Length: element.length,
                                    Direction: 1
                                })
                            });
                        }else{
                            this.$swal({
                                position: 'top-end',
                                icon: 'error',
                                title: 'Erro...',
                                text: "Não foi possível localizar parêmetros",
                            })
                        }
                    })
            },
            Submit(){
                if(this.validateForm()){
                    this.Loader.showLoader = true;
                    let storedProcedure = this.storedProcedures.find(e => e.object_id == this.form.Name)
                    this.form.Config = "Server=" + this.form.Server +"; Database=" + this.form.DataBase +"; User ID="+ this.form.User +";Password="+ this.form.Password +";";
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
            },
            GetColorIconDatabase(){
                return this.connection == true ? '#0AF426' : '#F71903'
            }
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
