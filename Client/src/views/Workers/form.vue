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
                            
                            <div class="md-layout-item md-size-100 md-small-size-100 mt-1"> 
                                <md-button class="md-raised md-primary" @click="TestConnection " >Testar conexão</md-button>                        
                                <md-icon class="mt-1"><font-awesome-icon icon="database" :color="GetColorIconDatabase()" /></md-icon>
                            </div>

                        </div>
                        
                    </md-tab>

                    <md-tab md-label="Configuração">
                        <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                            <div class="md-layout">
                                <div class="md-layout-item md-size-25 md-small-size-100">
                                        <md-field>
                                            <label>Nome</label>
                                            <md-input v-model="form.Name"/>
                                        </md-field>
                                    </div>
                            </div> 
                        </div>
                        <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                            <div class="md-layout">
                                

                                <div class="md-layout-item md-size-25 md-small-size-100 ">
                                    <md-radio v-model="form.tipoTempo" value="diario">Diário</md-radio>
                                    <md-radio v-model="form.tipoTempo" value="semanal">Semanal</md-radio>
                                    <md-radio v-model="form.tipoTempo" value="mensalmente">Mensalmente</md-radio>
                                </div>

                                <!-- DIARIO -->
                                <div class="md-layout-item md-size-75" v-if="form.tipoTempo == 'diario'">
                                    <div class="md-layout-item md-size-25 md-small-size-100">
                                        <md-field>
                                            <label>Repetir a cada X horas</label>
                                            <md-input type="number" min="1" max="23" id="quantidade_tempo" v-model="dia.repetirCadaHora"/>
                                        </md-field>
                                    </div>
                                </div>

                                <!-- SEMANAL -->
                                <div class="md-layout-item md-size-75" v-if="form.tipoTempo == 'semanal'">
                                    <div class="md-layout-item md-size-25 md-small-size-100">
                                        <md-field>
                                            <label>Repedir a cada X semanas</label>
                                            <md-input type="number" v-model="semanal.repetircadaSemana" disabled/>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-25 md-small-size-100">
                                        <md-field>
                                            <label>Hora da execução</label>
                                            <md-input type="time" v-model="semanal.hora"/>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-45 md-small-size-100">
                                        <md-checkbox v-model="semanal.diaSemana" value="MON">Segunda</md-checkbox>
                                        <md-checkbox v-model="semanal.diaSemana" value="TUE">Terça</md-checkbox>
                                        <md-checkbox v-model="semanal.diaSemana" value="WED">Quarta</md-checkbox>
                                        <md-checkbox v-model="semanal.diaSemana" value="THU">Quinta</md-checkbox>
                                        <md-checkbox v-model="semanal.diaSemana" value="FRI">Sexta</md-checkbox>
                                        <md-checkbox v-model="semanal.diaSemana" value="SAT">Sabado</md-checkbox>
                                        <md-checkbox v-model="semanal.diaSemana" value="SUN">Domingo</md-checkbox>
                                    </div>
                                </div>

                                <!-- MENSAL -->
                                <div class="md-layout-item md-size-75" v-if="form.tipoTempo == 'mensalmente'">
                                    <div class="md-layout-item md-size-25 md-small-size-100">
                                        <md-field>
                                            <label>Dia do mês</label>
                                            <md-input type="number" min="1" max="31" id="diaExecucao" v-model="mensal.dia"/>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-25 md-small-size-100">
                                        <md-field>
                                            <label>Hora da execução</label>
                                            <md-input type="time" v-model="mensal.hora"/>
                                        </md-field>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </md-tab>

                    <md-tab md-label="Code SQL">
                        <div class="md-layout-item md-size-100 md-small-size-100 ">
                            <codemirror
                                v-model="form.script"
                                ref="cmEditor"
                                :options="cmOptions"
                            />
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
    import { codemirror } from 'vue-codemirror';

    // import base style
    import 'codemirror/lib/codemirror.css'
    import 'codemirror/theme/dracula.css'
    import 'codemirror/mode/sql/sql.js'

    export default {
        mixins: [validationMixin],
        components: {
            codemirror
        },
        props: ['edit'],       
        data: () => ({
            form: {
                Name: null,
                Driver: null,
                Server:null,
                DataBase:null,
                User:null,
                Password:null,
                Config: null,
                tipoTempo: 'diario',
                script: 'SELECT * FROM docto',
                cronExpression: '',
                ContextName:null
            },
            dia:{
                repetirCadaHora:1 
            },
            semanal:{
                diaSemana:'MON',
                repetircadaSemana:null,
                hora:'00:00'
            },
            mensal:{
                dia:1,
                hora:'00:00'
            },
            cmOptions: {
                tabSize: 4,
                mode: 'sql',
                theme: 'dracula',
                lineNumbers: true,
                line: true,
                // more CodeMirror options...
            },
            loader: false,
            sending: false,
            connection:false,

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
            }
        },
        methods: {
            generateCron(){
                // ┌───────────── minute                0-59              * , - /                      
                // │ ┌───────────── hour                0-23              * , - /                      
                // │ │ ┌───────────── day of month      1-31              * , - / L W ?                
                // │ │ │ ┌───────────── month           1-12 or JAN-DEC   * , - /                      
                // │ │ │ │ ┌───────────── day of week   0-6  or SUN-SAT   * , - / # L ?
                // │ │ │ │ │
                // * * * * *
                if(this.form.tipoTempo == 'diario'){
                    //"0 */8 * * *"  -> roda a cada 8 horas
                    this.form.cronExpression = '0 */'+ this.dia.repetirCadaHora +' * * *'
                    console.log(this.form.cronExpression)
                }
                if(this.form.tipoTempo == 'semanal'){
                    // "30 18 * * TUE" roda as 18:30 de toda terça-feira
                    let hora = this.semanal.hora.split(':');
                    this.form.cronExpression = hora[1] + ' ' + hora[0]+ ' * * ' + this.semanal.diaSemana
                    console.log(this.form.cronExpression)
                }
                if(this.form.tipoTempo == 'mensal'){
                    //0 15 11 * * -> roda as 15:00 de todo dia 11 de cada mês
                    let hora = this.mensal.hora.split(':');
                    this.form.cronExpression = hora[1] + ' ' + hora[0] + ' ' + this.mensal.dia + ' * *'
                    console.log(this.form.cronExpression)
                }
            },
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

                console.log(this.$v)
                if (!this.$v.$invalid) {
                    return true;
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
                    this.form.Name = this.edit.name

                    this.form.script = this.edit.script;

                    let cron = this.edit.cronExpression.split(' ');
                    if(this.edit.tipoTempo == 'dia'){
                        this.form.tipoTempo = 'dia'
                        this.dia.repetirCadaHora = cron[1].slice(2)
                    }
                    if(this.edit.tipoTempo == 'semanal')
                    {
                        this.form.tipoTempo = 'semanal'
                        this.semanal.diaSemana = cron[4];
                        this.semanal.repetircadaSemana = null;
                        this.semanal.hora = cron[1] + ':' + cron[0]        
                    }
                    if(this.edit.tipoTempo == 'mensal')
                    {
                        this.form.tipoTempo = 'mensal'
                        this.mensal.dia =  cron[2];
                        this.mensal.hora = cron[1] + ':' + cron[0];
                    }

                    this.TestConnection();
                    

                    // this.Dynamic_params.pop();
                    // this.edit.params.forEach(element => {
                    //     let typeCode = this.TypeCodes.find(e => e.name == element.typeCode);
                    //     this.Dynamic_params.push({
                    //         Name: element.name,
                    //         Mode: element.mode,
                    //         TypeCode: typeCode.code,
                    //         Length: element.length,
                    //         Direction: !element.direction ? 1 : element.direction
                    //     })
                    // });

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
            Submit(){
                this.generateCron();
                if(this.validateForm()){
                    this.Loader.showLoader = true;
                    this.form.Config = "Server=" + this.form.Server +"; Database=" + this.form.DataBase +"; User ID="+ this.form.User +";Password="+ this.form.Password +";";
                    this.form.ContextName = this.$route.params.context;

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
        },
        computed: {
            codemirror() {
                return this.$refs.cmEditor.codemirror
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

    .mt-2 {
        margin-top: 30px;
    }

    .mf-1 {
        margin-left: 15px;
    }

    .style-choser{
        height: 35px !important;
    }

    .md-radio {
        display: flex;
    }
    
</style>
