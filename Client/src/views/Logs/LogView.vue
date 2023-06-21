<template>
    <div class="container">
        <md-card class="md-layout-item md-size-100 md-small-size-100">
            <md-card-header>
            <div class="md-title">Filtros</div>
            </md-card-header>

            <md-card-content>
                <div class="md-layout md-gutter">
                    <div class="md-layout-item md-small-size-100">
                        <md-field>
                            <div class="block">
                                <h3>Date</h3>
                                <md-datepicker v-model="date" />
                            </div>
                        </md-field>

                        <md-field>
                            <label>Inicio</label>
                            <md-input type="time" name="inicio" id="inicio" v-model="Inicio" />
                        </md-field>

                         <md-field>
                            <label>Fim</label>
                            <md-input type="time" name="fim" id="fim" v-model="Fim" />
                        </md-field>
                    </div>

                   
                </div>           
            </md-card-content>

            <md-card-actions>
                <md-button type="submit" class="md-primary">Filtrar</md-button>
            </md-card-actions>
        </md-card>

    </div>
</template>

<script>
    import axios from 'axios';
    export default {
        data: () => ({
            Repository:null,
            files:null,
            date: new Date(),
            Inicio: null,
            Fim:null
        }),
        methods: {
            getFiles() {
               this.Loader.showLoader = true;
               axios
                    .get('api/Logs')
                    .then(response => {
                        this.Loader.showLoader = false;
                        this.files = response.data
                    })
            }
        },
        mounted(){
            this.getFiles();           
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