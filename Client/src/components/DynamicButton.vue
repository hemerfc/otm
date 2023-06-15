<template>
    <md-button :class="[format,color]">
        <md-icon v-if="icon">{{icon}}</md-icon>
        <span v-else>text</span>
    </md-button>
</template>

<script>
    export default {
        name: "dynamicButton",
        props: ['icon','color','format','text'],
        data: () => ({ 
            content:null
        }),
        methods: {
            contentButton(){
                if(this.icon){
                    this.content = `<md-icon>${this.icon}</md-icon>`
                }else{
                    this.content = this.text
                }
            }
        }
    };
</script>


<style scoped>
    .color-green{
        background-color: #00cc44 !important;
    }

    .color-green .md-icon{ 
        color:#ffffff
    }
</style>