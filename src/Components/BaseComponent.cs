using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using NLog;

namespace Otm.Components
{
    public abstract class BaseComponent : IBaseComponent
    {
        protected string CompenentConfig { get; set; }
        protected string ComponentName { get; private set; }
        protected ILogger Logger { get; set; }
        protected IMessager Messager { get; set; }
        protected BlockingCollection<ComponentMessage> MessagesQueue;
        protected int RefreshTime { get; set; }
        protected BackgroundWorker Worker;
        protected object AddMessageLock = new object();
        
        public virtual void Init(String componentConfig, ILogger logger, IMessager messager)
        {
            CompenentConfig = componentConfig;
            ComponentName = "";

            Logger = logger;
            Messager = messager;
            MessagesQueue = new BlockingCollection<ComponentMessage>(256);
            RefreshTime = 100; // a cada 100ms
        }

        public BackgroundWorker GetWorker()
        {
            return Worker;
        }
        
        public virtual void Start(BackgroundWorker worker)
        {
            Logger.Info($"{ComponentName}: Iniciando o componente.");

            Worker = worker;

            while(true)
            {
                Update();

                ComponentMessage msg;
                // aguarda uma nova mensagem por 100ms
                if (MessagesQueue.TryTake(out msg, RefreshTime))
                    ProcessMessage(msg);

                if (Worker.CancellationPending)
                {
                    Stop();
                    // e.Cancel = true;
                    return;
                }                    
            }
        }

        public virtual void Update()
        {

        }

        public virtual void Stop()
        {
            Logger.Info($"{ComponentName}: Encerrando o componente.");
        }

        protected virtual void ProcessMessage(ComponentMessage msg)
        {
            switch (msg.Name)
            {
                case "STOP_CTRL":
                    
                    break;
                default:
                    Logger.Error($"{ComponentName}: Mensagem não esperado ({msg.Name}).");
                    break;                    
            }
        }

        public void AddMessage(ComponentMessage msg)
        {
            try 
            {
                lock (AddMessageLock)
                {
                    MessagesQueue.Add(msg);
                }            
            }
            catch(Exception ex)
            {
                Logger.Error(ex, $"Falha ao incluir a mensagem! (Msg {msg.Name} Source {msg.Source} Target {msg.Target})");
            }
        }        
    }
}