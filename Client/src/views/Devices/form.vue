<template>
    <div>
        <form novalidate class="md-layout" @submit.prevent="validateForm">
            <div class="md-layout">
                <md-tabs md-dynamic-height>
                    <md-tab md-label="Conexão com o device">
                        <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                            <div class="md-layout">
                                
                                <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                    <md-field :class="getValidationClass('Driver')">
                                        <label v-if="form.Driver == null">Driver</label>
                                        <select name="Driver" id="Driver" class="md-input" v-model="form.Driver">
                                            <option value="ptl">PTL</option>
                                            <option value="s7">S7</option>
                                            <option value="RabbitMq">RabbitMq</option>
                                            <option value="File">File</option>
                                        </select>
                                        <span class="md-error">Driver é obrigatório</span>
                                    </md-field>
                                </div>
                            </div>

                                <div class="md-layout" v-if="form.Driver == 'ptl'">
                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('TipoPtl')">
                                            <label v-if="form.TipoPtl == null">Tipo de ptl</label>
                                            <select name="Driver" id="TipoPtl" class="md-input" v-model="form.TipoPtl">
                                                <option value="Atop">Atop</option>
                                                <option value="Smart">Smart Picking</option>
                                            </select>
                                            <span class="md-error">Tipo de ptl é obrigatório</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('TipoPtl')">
                                            <label v-if="form.TipoPtl == null">Tipo de ptl</label>
                                            <select name="Driver" id="TipoPtl" class="md-input" v-model="form.TipoPtl">
                                                <option value="Atop">Atop</option>
                                                <option value="Smart">Smart Picking</option>
                                            </select>
                                            <span class="md-error">Tipo de ptl é obrigatório</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('Ip')">
                                            <label>IP</label>
                                            <md-input name="Ip" id="Ip" autocomplete="IP para conexão" v-model="form.Ip" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.Ip.required">IP é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.Ip.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('Port')">
                                            <label>Porta</label>
                                            <md-input name="Port" id="Porta" autocomplete="Porta para conexão" v-model="form.Port" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.Port.required">Porta é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.Port.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('MasterDevice')">
                                            <label>Master Device</label>
                                            <md-input name="MasterDevice" id="MasterDevice" autocomplete="Master Device para conexão" v-model="form.MasterDevice" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.MasterDevice.required">Master Device é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.MasterDevice.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('TestCardCode')">
                                            <label>Test card code</label>
                                            <md-input name="TestCardCode" id="TestCardCode" autocomplete="Test card code para conexão" v-model="form.TestCardCode" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.TestCardCode.required">Test card code é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.TestCardCode.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <label>Has read gate</label>
                                        <div class="flex-column">
                                            <md-radio v-model="form.HasReadGate" :value="true">Sim</md-radio>
                                            <md-radio v-model="form.HasReadGate" :value="false">Não</md-radio>
                                        </div>
                                    </div>
                                </div>

                                <div class="md-layout" v-if=" form.Driver == 's7' ">
                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('Host')">
                                            <label>Host</label>
                                            <md-input name="Host" id="Host" autocomplete="Host para conexão" v-model="form.Host" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.Host.required">Host é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.Host.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('Rack')">
                                            <label>Rack</label>
                                            <md-input name="Rack" id="Rack" autocomplete="Rack para conexão" v-model="form.Rack" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.Rack.required">Rack é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.Rack.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('Slot')">
                                            <label>Slot</label>
                                            <md-input name="Slot" id="Slot" autocomplete="Slot para conexão" v-model="form.Slot" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.Slot.required">Slot é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.Slot.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>
                                </div>

                                <div class="md-layout" v-if="form.Driver=='RabbitMq'">
                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('HostRabbit')">
                                            <label>Host</label>
                                            <md-input name="Host Rabbit" id="HostRabbit" autocomplete="Host Rabbit para conexão" v-model="form.HostRabbit" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.HostRabbit.required">Host é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.HostRabbit.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('PortRabbit')">
                                            <label>Porta</label>
                                            <md-input name="PortRabbit" id="PortRabbit" autocomplete="Porta Rabbit para conexão" v-model="form.PortRabbit" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.PortRabbit.required">Porta é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.PortRabbit.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('Exchange')">  
                                            <label>Exchange</label>
                                            <md-input name="Exchange" id="Exchange" autocomplete="Exchange para conexão" v-model="form.Exchange" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.Exchange.required">Exchange é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.Exchange.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>
                                </div>

                                <div class="md-layout" v-if="form.Driver == 'File'">

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('inputPath')">
                                            <label>Input Path</label>
                                            <md-input name="inputPath" id="inputPath" autocomplete="inputPath para conexão" v-model="form.inputPath" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.inputPath.required">inputPath é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.inputPath.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('outputPath')">
                                            <label>Output Path</label>
                                            <md-input name="outputPath" id="outputPath" autocomplete="outputPath para conexão" v-model="form.outputPath" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.outputPath.required">outputPath é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.outputPath.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('resultPath')">
                                            <label>Result Path</label>
                                            <md-input name="resultPath" id="resultPath" autocomplete="resultPath para conexão" v-model="form.resultPath" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.resultPath.required">resul tPath é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.resultPath.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>

                                    <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                        <md-field :class="getValidationClass('inputFileFilter')">
                                            <label>Input File Filter</label>
                                            <md-input name="inputFileFilter" id="inputFileFilter" autocomplete="inputFileFilter para conexão" v-model="form.inputFileFilter" :disabled="sending" />
                                            <span class="md-error" v-if="!$v.form.inputFileFilter.required">inputFileFilter é um campo obrigatório</span>
                                            <span class="md-error" v-else-if="!$v.form.inputFileFilter.minlength">Tamanho Inválido</span>
                                        </md-field>
                                    </div>
                                </div>
                                

                                
                            <md-button class="md-raised md-primary" v-if="form.Driver == 's7'" @click="TestConnection">Testar conexão</md-button>
                            <md-button class="md-raised md-primary" v-if="form.Driver == 'RabbitMq'" @click="TestConnection">Testar conexão</md-button>
                        </div>
                    </md-tab>

                    <md-tab md-label="General">
                        <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                            <div class="md-layout">

                                <div class="md-layout-item md-size-45 md-small-size-100 mf-1">
                                    <md-field :class="getValidationClass('Name')">
                                        <label>Name</label>
                                        <md-input name="Name" id="Name" autocomplete="Test card code para conexão" v-model="form.Name" :disabled="sending" />
                                        <span class="md-error" v-if="!$v.form.Name.required">Test card code é um campo obrigatório</span>
                                        <span class="md-error" v-else-if="!$v.form.Name.minlength">Tamanho Inválido</span>
                                    </md-field>
                                </div>

                                <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                                    <md-divider></md-divider>
                                    <md-subheader>Tags</md-subheader>
                                </div>

                                <div class="md-layout-item md-size-100 md-small-size-100 ">
                                    <div class="md-layout" v-for="(Dynamic_param,index) in $v.Dynamic_params.$each.$iter" :key="index">

                                        <div class="md-layout-item md-size-20 md-small-size-100">
                                            <label>Nome</label>
                                            <md-field :class="getValidationDynamicParamsClass('Name',index)">
                                                <md-input v-model.trim="Dynamic_param.Name.$model" autocomplete="Nome do parêmetro"/>
                                                <span class="md-error" v-if="!Dynamic_param.Name.required">Nome é um campo obrigatório</span>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-20 md-small-size-100">
                                            <label>Address</label>
                                            <md-field :class="getValidationDynamicParamsClass('Address',index)">
                                                <md-input v-model.trim="Dynamic_param.Address.$model" autocomplete="Nome do parêmetro"/>
                                                <span class="md-error" v-if="!Dynamic_param.Address.required">Address é um campo obrigatório</span>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-10 md-small-size-100">
                                            <label>Rate</label>
                                            <md-field :class="getValidationDynamicParamsClass('Rate',index)">
                                                <md-input v-model.trim="Dynamic_param.Rate.$model" autocomplete="Nome do parêmetro"/>
                                                <span class="md-error" v-if="!Dynamic_param.Rate.required">Rate é um campo obrigatório</span>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-20 md-small-size-100">
                                            <label>Modo</label>
                                            <md-field :class="getValidationDynamicParamsClass('Mode',index)">
                                                <select v-model="Dynamic_param.Mode.$model" id="Mode" class="md-input">
                                                    <option value="FromOTM" selected>FromOTM</option>
                                                    <option value="ToOTM">ToOTM</option>
                                                </select>
                                                <span class="md-error" v-if="!Dynamic_param.Mode.required">Modo é um campo obrigatório</span>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-20 md-small-size-100">
                                            <label>Formato</label>
                                            <md-field :class="getValidationDynamicParamsClass('TypeCode',index)">
                                                <select  v-model="Dynamic_param.TypeCode.$model" id="TypeCode" class="md-input">
                                                    <option v-for="typeCode in TypeCodes" :key="typeCode.code" :value="typeCode.code">{{typeCode.name}}</option>
                                                </select>
                                                <span class="md-error" v-if="!Dynamic_param.TypeCode.required">Formato é um campo obrigatório</span>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-10 md-small-size-100 mt-1">
                                            <md-button class="md-raised md-accent" id="remove_param" @click="RemoveParam(index)"><md-icon>remove</md-icon></md-button>                                              
                                        </div>                                   
                                    </div>
                                    <dynamicButton color="color-green" icon="add" format="md-raised" v-on:click.native="addParam"/>
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
    import { validationMixin } from 'vuelidate';
    import axios from 'axios';
    import {
        requiredIf,
        minLength
    } from 'vuelidate/lib/validators';
    import { dynamicButton } from "@/components";
    import "vue-select/dist/vue-select.css";

    export default {
        mixins: [validationMixin],
        components: {
            dynamicButton
        },
        props: ['edit'],       
        data: () => ({
            form: {
                Name: null,
                Driver: 's7',  
                HasReadGate: false,
                Ip:null,
                Port:null,
                MasterDevice:null,
                TestCardCode:null,
                Tags: null,
                Host:null,
                Slot:null,
                Rack:null,
                Exchange:null,   // ALTERACAO
                PortRabbit:null, // ALTERACAO
                HostRabbit:null, // ALTERACAO
                TipoPtl: 'Atop',
                inputPath: null,
                outputPath:null,
                resultPath:null,
                inputFileFilter:null
            },


            Dynamic_params:[
                {
                    Name:null,
                    Address:null,
                    Rate:0,
                    Mode:"FromOTM",
                    TypeCode:18
                }
            ],
            loader: false,
            sending: false,
            connection:true,
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
                        return this.connection == true || this.form.Driver != 's7' || this.form.Driver != 'RabbitMq'  //ALTERACAO
                    }),
                    minLength: minLength(3)
                },
                Ip: {
                    required:requiredIf(function(){
                        return this.form.Driver == 'ptl'
                    }),
                    minLength: minLength(8)
                },
                
                Port: {
                    required:requiredIf(function(){
                        return this.form.Driver == 'ptl'
                    }),
                    minLength: minLength(2)
                },

                PortRabbit: { // ALTERACAO
                    required:requiredIf(function(){
                        return this.form.Driver == 'RabbitMq'
                    }),
                    minLength: minLength(2)
                },

                Exchange: { // ALTERACAO
                    required:requiredIf(function(){
                        return this.form.Driver == 'RabbitMq'
                    }),
                    minLength: minLength(8)
                },

                MasterDevice: {
                    required:requiredIf(function(){
                        return this.form.Driver == 'ptl'
                    }),
                    minLength: minLength(2)
                },
                TestCardCode: {
                    required:requiredIf(function(){
                        return this.form.Driver == 'ptl'
                    }),
                    minLength: minLength(2)
                },
                Host: {
                    required:requiredIf(function(){
                        return this.form.Driver == 's7'
                    }),
                    minLength: minLength(3)
                },

                HostRabbit: { // ALTERACAO
                    required:requiredIf(function(){
                        return this.form.Driver == 'RabbitMq'
                    }),
                    minLength: minLength(3)
                },
                Slot: {
                    required:requiredIf(function(){
                        return this.form.Driver == 's7'
                    }),
                    minLength: minLength(1)
                },
                Rack: {
                    required:requiredIf(function(){
                        return this.form.Driver == 's7'
                    }),
                    minLength: minLength(1)
                },
                inputPath: {
                    required:requiredIf(function(){
                        return this.form.Driver == 'File'
                    }),
                    minLength: minLength(1)
                },
                outputPath:{
                    required:requiredIf(function(){
                        return this.form.Driver == 'File'
                    }),
                    minLength: minLength(1)
                },
                resultPath:{
                    required:requiredIf(function(){
                        return this.form.Driver == 'File'
                    }),
                    minLength: minLength(1)
                },
                inputFileFilter:{
                    required:requiredIf(function(){
                        return this.form.Driver == 'File'
                    }),
                    minLength: minLength(1)
                },
            },
            Dynamic_params:{
                $each: {
                    Name: {
                        required:requiredIf(function(){
                            return this.connection == true
                        }),
                        minLength: minLength(2)
                    },
                    Address:{
                        required:requiredIf(function(){
                            return this.connection == true
                        }),
                    },
                    Rate:{
                        required:requiredIf(function(){
                            return this.connection == true
                        }),
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
                    Address:null,
                    Rate:0,
                    Mode:"FromOTM",
                    TypeCode:18
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
                    this.form.Name =  this.edit.name;
                    this.form.Driver =  this.edit.driver;
                    if(this.form.Driver == 's7'){
                        this.form.Host = conection_string[0].split('=').pop();
                        this.form.Slot = conection_string[1].split('=').pop();
                        this.form.Rack = conection_string[2].split('=').pop();
                    }else if(this.form.Driver == 'ptl') if(this.form.Driver == 'ptl'){
                        this.form.Ip = conection_string[0].split('=').pop();
                        this.form.Port = conection_string[1].split('=').pop();
                        this.form.MasterDevice = conection_string[2].split('=').pop();
                        this.form.HasReadGate = conection_string[3].split('=').pop().toLowerCase();
                        this.form.TestCardCode = conection_string[4].split('=').pop();
                    }else if(this.form.Driver == 'RabbitMq'){
                        this.form.HostRabbit = conection_string[0].split('=').pop(); //ALTERAÇÕES ************
                        this.form.PortRabbit = conection_string[1].split('=').pop();
                        this.form.Exchange = conection_string[2].split('=').pop();
                    }else if(this.form.Driver == 'File'){
                        this.form.inputPath = conection_string[0].split('=').pop(); //ALTERAÇÕES ************
                        this.form.outputPath = conection_string[1].split('=').pop();
                        this.form.resultPath = conection_string[2].split('=').pop();
                        this.form.inputFileFilter = conection_string[3].split('=').pop();
                    }

                    this.Dynamic_params.pop();
                    this.edit.tags.forEach(element => {
                        let typeCode = this.TypeCodes.find(e => e.name == element.typeCode);
                        this.Dynamic_params.push({
                            Name:element.name,
                            Address:element.address,
                            Rate:element.rate,
                            Mode:element.mode,
                            TypeCode:typeCode.code
                        })
                    });

                }
            },
            TestConnection(){
                if(this.validateForm()){
                    this.Loader.showLoader = true;
                    let method = '';
                    if(this.form.Driver == 'ptl'){
                        method = "TestConnectionPtl"
                    }else if (this.form.Driver == 's7') { //ALTERACAO
                        method = "TestConnectionS7"
                    } else if(this.form.Driver == 'RabbitMq'){
                        method = "TestConnectionRabbit" 
                    }

                    axios
                        .post('/api/Device/'+ method ,JSON.stringify(this.form),{
                            headers:{
                                'Content-Type': 'application/json',
                            }
                        })
                        .then(response => {
                            this.Loader.showLoader = false;
                            if( response.data.result == true){
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
                                    text: 'Falha ao se conectar!',
                                })
                            }
                        });
                }
            },
            Submit(){
                if(this.connection && this.validateForm()){  //ALTERACAO
                    this.Loader.showLoader = true;
                    this.form.ContextName = this.$route.params.context;
                    this.form.Tags = this.Dynamic_params;
                    if(this.form.Driver == 'ptl'){
                        this.form.Config = "ip=" + this.form.Ip +"; Porta=" + this.form.Port +"; MasterDevice="+ this.form.MasterDevice +"; HasReadGate="+ this.form.HasReadGate +"; TestCardCode="+this.form.TestCardCode+";";
                    }else if(this.form.Driver == 's7'){
                        this.form.Config = "Host=" + this.form.Host+"; Rack= " + this.form.Rack +"; Slot="+ this.form.Slot+";"; 
                    }else if(this.form.Driver == 'RabbitMq'){
                        this.form.Config = "HostRabbit=" + this.form.HostRabbit+"; PortRabbit= " + this.form.PortRabbit +"; Exchange="+ this.form.Exchange+";"; 
                    }else if(this.form.Driver == 'File'){
                        this.form.Config = "inputPath=" + this.form.inputPath+"; outputPath= " + this.form.outputPath +"; resultPath="+ this.form.resultPath+";" + "; inputFileFilter="+ this.form.inputFileFilter+";"; 
                    }
                        axios
                            .post('/api/Device',JSON.stringify(this.form),{
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
                                        text: "Falha ao cadastrar device.",
                                    })
                                }                           
                            });
                    
                }else{
                    this.$swal({
                        position: 'top-end',
                        icon: 'error',
                        title: 'Erro...',
                        text: "Conexão com Device não foi estabelecida.",
                    })
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
