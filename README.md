[![Open in Dev Containers](https://img.shields.io/static/v1?label=Dev%20Containers&message=Open&color=blue&logo=visualstudiocode)](https://vscode.dev/redirect?url=vscode://ms-vscode-remote.remote-containers/cloneInVolume?url=https://github.com/hemerfc/otm)


# OTM - Open Transaction Manager
 
    O OTM tem como objectivo simplificar a integração de sistema de Automação (Siemens, Rockwell, etc).

    Para isso possui os componentes "Devices", que são usados para receber e enviar informaçoes dos PLCs, estas informação disparam "Triggers" nas Transactions, que são responsaveis por mapear e direcionar os dados coletados para os DataPoints (funcoes ou storeprocedures).

    Tambem são utilizados os componentes "Brokers", responsaveis por receber mensagens (protocolos baseados em Sockets TCP) dos dispositivos de Automação e encaminhar para serviços de mensageria (ak Rabbit MQ), tambem fazem o processo contrario recebendo do serviço de mensageria e encaminhando para os dispositivos de Automação. A construção do protocolo de mensagens em si, esta fora do escopo deste projeto e faz parte de outro projeto de codigo fechado.

 ## Project setup
 Depois de baixar o projeto, rodar comando
```
git submodule init
```
```
git submodule update
```
acessar a pasta Server/Client e rodar 
```
npm install
```
