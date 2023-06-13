<template>
    <div>
        <div class="md-layout md-gutter">
            <div class="md-layout-item md-size-50">
                <md-field>
                    
                    <select v-model="dataPoint" class="md-input" name="dataPoint" id="dataPoint" @change="onChangeDataPoint()">
                        <option v-for="dataPoint in dataPoints" :key="dataPoint.id" :value="dataPoint.id">{{dataPoint.name}}</option>
                    </select>
                </md-field>
            </div>

            <div class="md-layout-item md-size-100">
                <div class="md-layout" v-for="Params in dataPointParams" :key="Params.name">
                    <div class="md-layout-item md-size-30 mf-1">
                        <md-field>
                            <label>Nome</label>
                            <md-input v-model="Params.name" disabled></md-input>
                        </md-field>
                    </div>

                    <div class="md-layout-item md-size-30 mf-1">
                        <md-field>
                            <label>Valor</label>
                            <md-input v-model="Params.value"></md-input>
                        </md-field>
                    </div>
                </div>
            </div>
            
            <div class="md-layout-item md-size-100">
                <md-button class="md-raised md-primary" @click="execute()">Executar</md-button>
            </div>
        </div>
    </div>
</template>

<script>
import axios from 'axios';
import { validationMixin } from 'vuelidate';
export default {
    mixins: [validationMixin],
    data: () => ({
        dataPoints: [],
        dataPoint: null,
        dataPointParams:[],
    }),
    methods: {
        getAllDataPoint() {
            axios
                .get('/api/DataPoint')
                .then(response => {
                    this.dataPoints = response.data
                })
        },
        onChangeDataPoint(){
            let params = this.dataPoints.filter(i => i.id == this.dataPoint);
            params.forEach(element => {               
                this.dataPointParams = element.params;
            });
        },
        execute(){
            let dataPoint = this.dataPoints.filter(i => i.id == this.dataPoint);

            axios
                .post('/api/DataPoint/ExecuteProcedure',JSON.stringify(dataPoint),{
                    headers:{
                        'Content-Type': 'application/json',
                    }
                })
                .then(response => {
                    console.log(response)
                });
        }
    },
    mounted() {
        this.getAllDataPoint();         
    }
}
</script>

<style  scoped>
    .mt-1 {
        margin-top: 15px;
    }

    .mt-2 {
        margin-top: 30px;
    }

    .mf-1 {
        margin-left: 15px;
    }
</style>