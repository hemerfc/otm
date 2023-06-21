<template>
    <div>
        <form novalidate class="md-layout" @submit.prevent="validateForm">
            <div class="md-layout">
                <md-tabs md-dynamic-height>
                    <md-tab md-label="Transaction Settings">
                        <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                            <div class="md-layout">
                                
                                <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                    <md-field :class="getValidationClass('Name')">
                                        <label>Nome</label>
                                        <md-input name="Name" id="Name" autocomplete="Name para conexão" v-model="form.Name" :disabled="sending" />
                                        <span class="md-error" v-if="!$v.form.Name.required">Name é um campo obrigatório</span>
                                        <span class="md-error" v-else-if="!$v.form.Name.minlength">Tamanho Inválido</span>
                                    </md-field>
                                </div>

                                <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                    <md-field :class="getValidationClass('DataPointName')">
                                        <label v-if="form.DataPointName == null">Data Point Name</label>
                                        <select name="DataPointName" id="DataPointNamea" autocomplete="Data Point Name para conexão" class="md-input" v-model="form.DataPointName" :disabled="sending">
                                            <option v-if="dataPoints.length < 1" disabled>Sem data points cadastrado</option>
                                            <option v-for="dataPoint in dataPoints" :key="dataPoint.id" :value="dataPoint.name" >{{dataPoint.name}}</option>
                                        </select>
                                        <span class="md-error" v-if="!$v.form.DataPointName.required">Data Point Name é um campo obrigatório</span>
                                        <span class="md-error" v-else-if="!$v.form.DataPointName.minlength">Tamanho Inválido</span>
                                    </md-field>
                                </div>

                                <div class="md-layout-item md-size-30 md-small-size-100 mf-1"  v-if="form.TriggerType != 'OnScheduler'">
                                    <md-field :class="getValidationClass('TargetDeviceName')">
                                        <label v-if="form.TargetDeviceName == null">Device Name</label>
                                        <select name="TargetDeviceName" id="TargetDeviceName" autocomplete="Device Name para conexão" class="md-input" v-model="form.TargetDeviceName" :disabled="sending">
                                            <option v-if="devices.length < 1" disabled>Sem devices cadastrado</option>
                                            <option v-for="device in devices" :key="device.id" :value="device.name">{{device.name}}</option>
                                        </select>
                                        <span class="md-error" v-if="!$v.form.TargetDeviceName.required">Device Name é um campo obrigatório</span>
                                        <span class="md-error" v-else-if="!$v.form.TargetDeviceName.minlength">Tamanho Inválido</span>
                                    </md-field>
                                </div>

                                <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                    <md-field :class="getValidationClass('TriggerType')">
                                        <label v-if="form.TriggerType == null">Trigger Type</label>
                                        <select name="TriggerType" id="TriggerType" autocomplete="Trigger Type para conexão" class="md-input" v-model="form.TriggerType" :disabled="sending">
                                            <option value="OnCycle">On Cycle</option>
                                            <option value="OnTagChange">On Tag Change</option>
                                            <option value="OnScheduler">On Tag Scheduler</option>
                                        </select>
                                        <span class="md-error" v-if="!$v.form.TriggerType.required">Trigger Type é um campo obrigatório</span>
                                        <span class="md-error" v-else-if="!$v.form.TriggerType.minlength">Tamanho Inválido</span>
                                    </md-field>
                                </div>

                                <div class="md-layout-item md-size-30 md-small-size-100 mf-1">
                                    <md-field :class="getValidationClass('TriggerTagName')">
                                        <label>Trigger Tag Name</label>
                                        <md-input name="TriggerTagName" id="TriggerTagName" autocomplete="Trigger Tag Name para conexão" v-model="form.TriggerTagName" :disabled="sending" />
                                        <span class="md-error" v-if="!$v.form.TriggerTagName.required">Trigger Tag Name é um campo obrigatório</span>
                                        <span class="md-error" v-else-if="!$v.form.TriggerTagName.minlength">Tamanho Inválido</span>
                                    </md-field>
                                </div>
                            </div>
                        </div>
                    </md-tab>

                    <md-tab md-label="Source Binds" v-if="form.TriggerType != 'OnScheduler'">
                        <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                            <div class="md-layout">

                                <div class="md-layout-item md-size-100 md-small-size-100 ">
                                    <div class="md-layout" v-for="(Dynamic_param,index) in $v.Dynamic_params_SourceBinds.$each.$iter" :key="index">

                                        <div class="md-layout-item md-size-30 md-small-size-100">
                                            <md-field :class="getValidationDynamicParamsClassSourceBinds('DataPointParam',index)">
                                                <label>Data Point Param</label>
                                                <md-input v-model.trim="Dynamic_param.DataPointParam.$model" autocomplete="Data Point Param do parêmetro"/>
                                                <span class="md-error" v-if="!Dynamic_param.DataPointParam.required">Data Point Param é um campo obrigatório</span>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-30 md-small-size-100">
                                            <md-field>
                                                <label>Value</label>
                                                <md-input v-model.trim="Dynamic_param.Value.$model" autocomplete="Nome do parêmetro"/>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-30 md-small-size-100">
                                            <md-field>
                                                <label>Device Tag</label>
                                                <md-input v-model.trim="Dynamic_param.DeviceTag.$model" autocomplete="Device Tag do parêmetro"/>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-10 md-small-size-100 mt-1">
                                            <md-button class="md-raised md-accent" id="remove_param" @click="RemoveSourceBindsParam(index)"><md-icon>remove</md-icon></md-button>                                              
                                        </div>                                   
                                    </div>
                                    <dynamicButton color="color-green" icon="add" format="md-raised" v-on:click.native="addSourceBindsParam"/>
                                </div>
                            </div>
                        </div>
                    </md-tab>

                    <md-tab md-label="Target Binds" v-if="form.TriggerType != 'OnScheduler'">
                        <div class="md-layout-item md-size-100 md-small-size-100 mt-1">
                            <div class="md-layout">

                                <div class="md-layout-item md-size-100 md-small-size-100 ">
                                    <div class="md-layout" v-for="(Dynamic_param,index) in $v.Dynamic_params_TargetBinds.$each.$iter" :key="index">

                                        <div class="md-layout-item md-size-30 md-small-size-100">
                                            <md-field :class="getValidationDynamicParamsClassTargetBinds('DataPointParam',index)">
                                                <label>Data Point Param</label>
                                                <md-input v-model.trim="Dynamic_param.DataPointParam.$model" autocomplete="Data Point Param do parêmetro"/>
                                                <span class="md-error" v-if="!Dynamic_param.DataPointParam.required">Data Point Param é um campo obrigatório</span>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-30 md-small-size-100">
                                            <md-field>
                                                <label>Value</label>
                                                <md-input v-model.trim="Dynamic_param.Value.$model" autocomplete="Nome do parêmetro"/>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-30 md-small-size-100">
                                            <md-field>
                                                <label>Device Tag</label>
                                                <md-input v-model.trim="Dynamic_param.DeviceTag.$model" autocomplete="Device Tag do parêmetro"/>
                                            </md-field>
                                        </div>

                                        <div class="md-layout-item md-size-10 md-small-size-100 mt-1">
                                            <md-button class="md-raised md-accent" id="remove_param" @click="RemoveTargetBindsParam(index)"><md-icon>remove</md-icon></md-button>                                              
                                        </div>                                   
                                    </div>
                                    <dynamicButton color="color-green" icon="add" format="md-raised" v-on:click.native="addTargetBindsParam"/>
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
        required,
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
                DataPointName: null,
                TargetDeviceName: null,
                TriggerType:null,
                TriggerTagName:null,
                Binds:null,
            },
            Dynamic_params_SourceBinds:[
                {
                DataPointParam:null,
                Value:null,
                DeviceTag:null
                }
            ],
            Dynamic_params_TargetBinds:[
                {
                DataPointParam:null,
                Value:null,
                DeviceTag:null               
                }
            ],
            loader: false,
            sending: false,
            dataPoints:[],
            devices:[],
            ScheluderDataPoint:[]
        }),
        validations: {
            form: {
                Name: {
                    required,
                    minLength: minLength(3)
                },
                DataPointName: {
                    required,
                    minLength: minLength(5)
                },
                TargetDeviceName: {
                    required:requiredIf(function(){
                        return this.form.TriggerType != 'OnScheduler'
                    }),
                    minLength: minLength(2)
                },
                TriggerType: {
                required:requiredIf(function(){
                        return this.form.TriggerType != 'OnScheduler'
                    }),
                    minLength: minLength(1)
                },
                TriggerTagName: {
                required:requiredIf(function(){
                        return this.form.TriggerType != 'OnScheduler'
                    }),
                    minLength: minLength(2)
                }
            },
            Dynamic_params_SourceBinds:{               
                $each: {
                    DataPointParam: {
                    required:requiredIf(function(){
                        return this.form.TriggerType != 'OnScheduler'
                    }),
                        minLength: minLength(2)
                    },
                    Value: {
                        minLength: minLength(1)
                    },
                    DeviceTag: {
                        minLength: minLength(1)
                    }
                }
            },
            Dynamic_params_TargetBinds:{ 
                $each: {
                    DataPointParam: {
                    required:requiredIf(function(){
                        return this.form.TriggerType != 'OnScheduler'
                    }),
                        minLength: minLength(2)
                    },
                    Value: {
                        minLength: minLength(1)
                    },
                    DeviceTag: {
                        minLength: minLength(1)
                    }
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
            getValidationDynamicParamsClassSourceBinds(fieldName,index) {
                console.log( this.$v.Dynamic_params_SourceBinds.$each.$iter[index])
                const field = this.$v.Dynamic_params_SourceBinds.$each.$iter[index][fieldName]
                console.log(field)

                if (field) {
                    return {
                        'md-invalid': field.$invalid && field.$dirty
                    }
                }
            },
            getValidationDynamicParamsClassTargetBinds(fieldName,index) {
                const field = this.$v.Dynamic_params_TargetBinds.$each.$iter[index][fieldName]

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
            addSourceBindsParam () {
                this.Dynamic_params_SourceBinds.push({
                    DataPointParam:null,
                    Value:null,
                    DeviceTag:null
                })
            },
            addTargetBindsParam () {
                this.Dynamic_params_TargetBinds.push({
                    DataPointParam:null,
                    Value:null,
                    DeviceTag:null
                })
            },
            RemoveSourceBindsParam (index) {
                if(index != 0){
                    this.Dynamic_params_SourceBinds.splice(index,1)
                }
            },
            RemoveTargetBindsParam (index) {
                if(index != 0){
                    this.Dynamic_params_TargetBinds.splice(index,1)
                }
            },
            EditProps(){
                if(this.edit){
                    this.form.Id = this.edit.id;
                    this.form.Name =  this.edit.name;
                    this.form.DataPointName =  this.edit.dataPointName;
                    this.form.TargetDeviceName =  this.edit.targetDeviceName;
                    this.form.TriggerType =  this.edit.triggerType;
                    this.form.TriggerTagName =  this.edit.triggerTagName;

                    this.Dynamic_params_SourceBinds.pop();
                    this.edit.sourceBinds.forEach(element => {
                        this.Dynamic_params_SourceBinds.push({
                            DataPointParam:element.dataPointParam,
                            Value:element.value,
                            DeviceTag:element.deviceTag
                        })
                    });

                    this.Dynamic_params_TargetBinds.pop();
                    this.edit.targetBinds.forEach(element => {
                        this.Dynamic_params_TargetBinds.push({
                            DataPointParam:element.dataPointParam,
                            Value:element.value,
                            DeviceTag:element.deviceTag
                        })
                    });
                }
            },
            getData(){
                axios
                    .post('/api/Transaction/GetData',{ContextName:this.$route.params.context},{
                        headers:{
                            'Content-Type': 'application/json',
                        }
                    })
                    .then(response => {
                        this.dataPoints = response.data.dataPoints;
                        this.devices = response.data.devices
                        this.ScheluderDataPoint = response.data.schedulerDataPoint           
                    });
            },
            Submit(){
                if(this.validateForm()){
                    this.Loader.showLoader = true;
                    this.form.SourceBinds = this.Dynamic_params_SourceBinds;
                    this.form.TargetBinds = this.Dynamic_params_TargetBinds;
                    this.form.ContextName = this.$route.params.context

                    axios
                        .post('/api/Transaction',JSON.stringify(this.form),{
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
                }
            },
        },
        created: function(){
            this.getData();
            this.EditProps();
        },
        watch: {
            '$route.params.context': function (){
                this.getData();
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
</style>
