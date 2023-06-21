using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using Otm.Server.ContextConfig;
using System.Collections.Concurrent;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.IO;
using NLog;

namespace Otm.Server.Device.S7
{
    public class FileDevice : IDevice
    {

        public FileDevice()
        {
            tagValues = new ConcurrentDictionary<string, object>();
            tagVersion = new ConcurrentDictionary<string, int>();
            tagsAction = new ConcurrentDictionary<string, Action<string, object>>();
        }

        public string Name { get { return Config.Name; } }

        public BackgroundWorker Worker { get; private set; }

        private DeviceConfig Config;


        public Stopwatch Stopwatch { get; }

        private ILogger Logger;

        public bool Ready { get; private set; }


        public bool Enabled { get { return true; } }
        bool IDeviceStatus.Connected => Connected; //throw new NotImplementedException();


        public bool Connected = false;
        public bool Connecting = false;
        public DateTime lastConnectionTry = DateTime.Now;
        public int RECONNECT_DELAY = 3000;

        private readonly ConcurrentDictionary<string, object> tagValues;
        private readonly ConcurrentDictionary<string, int> tagVersion;
        private readonly ConcurrentDictionary<string, Action<string, object>> tagsAction;
        public EventingBasicConsumer consumer { get; set; }

        private string inputPath;
        private string outputPath;
        private string resultPath;
        private string inputFileFilter;

        private bool inputReady;
        private bool outputReady;

        private bool configured;

        public DateTime LastErrorTime { get { return DateTime.Now; } }

        public IReadOnlyDictionary<string, object> TagValues { get { return null; } }

        public IConnection RabbitConnection { get; private set; }
        public IModel RabbitChannel { get; private set; }
        public string UniqueDeviceId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime? LastUpdateDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int LicenseRemainingHours { get; set; }

        public object tagsActionLock = new object();

        public void Init(DeviceConfig dvConfig, ILogger logger)
        {
            this.Logger = logger;
            this.Config = dvConfig;
            GetConfig(dvConfig);
        }

        private void GetConfig(DeviceConfig dvConfig)
        {
            GetDeviceParameter(dvConfig);
            GetDeviceTags(dvConfig);
        }

        private void GetDeviceParameter(DeviceConfig dvConfig)
        {
            try
            {
                var cparts = dvConfig.Config.Split(';');

                this.inputPath = (cparts.FirstOrDefault(x => x.Contains("inputPath=")) ?? "").Replace("inputPath=", "").Trim();
                this.outputPath = (cparts.FirstOrDefault(x => x.Contains("outputPath=")) ?? "").Replace("outputPath=", "").Trim();
                this.resultPath = (cparts.FirstOrDefault(x => x.Contains("resultPath=")) ?? "").Replace("resultPath=", "").Trim();
                this.inputFileFilter = (cparts.FirstOrDefault(x => x.Contains("inputFileFilter=")) ?? "").Replace("inputFileFilter=", "").Trim();
            }
            catch (Exception ex)
            {
                Logger.Error($"FileDevice ({Config.Name})|GetDeviceParameter|Error: {ex}");
                throw;
            }
        }
        private void GetDeviceTags(DeviceConfig dvConfig)
        {
            try
            {
                //routingKey = dvConfig.Tags.FirstOrDefault(x => x.Name == nameof(routingKey)).Name ?? "*";
            }
            catch (Exception ex)
            {
                Logger.Error($"FileDevice ({Config.Name})|GetDeviceTags|Error: {ex}");
                throw;
            }
        }

        public void Start(BackgroundWorker worker)
        {
            // backgroud worker
            Worker = worker;

            while (true)
            {
                try
                {
                    if (CheckFiles())
                        ConfigureConnection();
                    else
                    {
                        Logger.Error($"FileDevice ({Config.Name})|Start|Erro nos diret�rios: inputReady:{(inputReady ? "Ok" : "Nok")}.  outputReady:{(outputReady ? "Ok" : "Nok")}");
                        configured = false;
                    }

                    var waitEvent = new ManualResetEvent(false);
                    waitEvent.WaitOne(500);

                    if (Worker.CancellationPending)
                    {
                        Ready = false;
                        Stop();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Ready = false;
                    //RabbitConnection.Dispose();
                    Logger.Error($"FileDevice ({Config.Name})|Start|Update Loop Error {ex}");
                    //client.Disconnect();
                }
            }
        }


        private bool CheckFiles()
        {
            inputReady = Directory.Exists(this.inputPath);
            outputReady = Directory.Exists(this.outputPath);


            return inputReady && outputReady;
        }

        private void ConfigureConnection()
        {
            if (!configured)
            {
                Logger.Info($"FileDevice ({Config.Name})|ConfigureConnection|Configurando conex�o...");
                Logger.Info($"FileDevice ({Config.Name})|ConfigureConnection|Instaurando o Watcher na pasta de input: '{inputPath}'");
                //using var watcher = new FileSystemWatcher(@"C:\temp\files\input");
                var watcher = new FileSystemWatcher(inputPath);

                watcher.NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.Security
                                     | NotifyFilters.Size;

                watcher.Changed += OnChanged;
                watcher.Created += OnCreated;
                watcher.Deleted += OnDeleted;
                watcher.Renamed += OnRenamed;
                watcher.Error += OnError;

                Logger.Info($"FileDevice ({Config.Name})|ConfigureConnection|Utilizando o filtro: '{inputFileFilter}'");
                //watcher.Filter = "*.txt";
                watcher.Filter = inputFileFilter;
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;


                configured = true;
            }
        }


        public void Stop()
        {
            //
        }

        private void ProcessFile(string fileName, string fileContent)
        {
            Logger.Info($"FileDevice ({Config.Name})|ProcessFile|Inicio. fileName: '{fileName}'. fileContent: '{fileContent}'");

            var tagTriggers = new List<(Action<string, object> func, string tagName, object tagValue)>();

            SetTagValue("input_name", fileName);
            SetTagValue("input_content", fileContent.Replace("\t", "__"));

            //SaveFile(fileName);

            //Se as tags possuem action
            if (tagsAction.ContainsKey("input_name"))
            {
                // guarda o trigger para executar apos atualizar todos os valores
                tagTriggers.Add(new(tagsAction["input_name"], "input_name", tagValues["input_name"]));
            }

            if (tagsAction.ContainsKey("input_content"))
            {
                // guarda o trigger para executar apos atualizar todos os valores
                tagTriggers.Add(new(tagsAction["input_content"], "input_content", tagValues["input_content"]));
            }

            foreach (var tt in tagTriggers)
            {
                lock (tagsActionLock)
                {
                    tt.func(tt.tagName, tt.tagValue);
                }
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            Logger.Info($"FileDevice ({Config.Name})|OnChanged|Changed: {e.FullPath}");
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Logger.Info($"FileDevice ({Config.Name})|OnCreated|Created: {e.FullPath}");
            
            var filename = Path.GetFileName(e.FullPath);
            var fileContent = GetContent(e.FullPath);
            Logger.Info($"FileDevice ({Config.Name})|OnCreated|Content: {fileContent}");
            ProcessFile(filename, fileContent);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            string fileContent = GetContent(e.FullPath);
            Logger.Info($"FileDevice ({Config.Name})|OnDeleted|Deleted: {e.FullPath}");
            Logger.Info($"FileDevice ({Config.Name})|OnDeleted|Deleted Content: {fileContent}");
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Logger.Info($"FileDevice ({Config.Name})|OnRenamed|Renamed:");
            Logger.Info($"FileDevice ({Config.Name})|OnRenamed|    Old: {e.OldFullPath}");
            Logger.Info($"FileDevice ({Config.Name})|OnRenamed|    New: {e.FullPath}");

            string fileContent = GetContent(e.FullPath);
            Logger.Info($"FileDevice ({Config.Name})|OnRenamed|Content: {fileContent}");
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Logger.Info($"FileDevice ({Config.Name})|OnError|{e.GetException()}");
            PrintException(e.GetException());
        }

        private void PrintException(Exception ex)
        {
            if (ex != null)
            {
                Logger.Error($"FileDevice ({Config.Name})|PrintException|Message: {ex.Message}");
                Logger.Error($"FileDevice ({Config.Name})|PrintException|Stacktrace:");
                Logger.Error($"FileDevice ({Config.Name})|PrintException|{(ex.StackTrace)}");

                if (ex.InnerException != null)
                    PrintException(ex.InnerException);
            }
        }

        private string GetContent(string filePath)
        {
            var result = string.Empty;

            string[] lines = null;

            if (File.Exists(filePath))
            {
                try
                {
                    lines = File.ReadAllLines(filePath);
                }
                catch (Exception ex)
                {
                    Logger.Error($"FileDevice ({Config.Name})|Arquivo '{filePath}' em uso:({ex.StackTrace}, tentando novamente...");
                    GetContent(filePath);
                }
            }

            if (lines != null)
            {
                foreach (string line in lines)
                {
                    result += "\t" + line;
                }
            }

            return result;
        }

        #region Legacy

        public void OnTagChangeAdd(string tagName, Action<string, object> triggerAction)
        {
            var tagConfig = GetTagConfig(tagName);

            // can't use a output tag as trigger, output put tags are writed to PLC
            if (tagConfig.Mode == Modes.FromOTM) // from OTM to device
            {
                throw new Exception($"FileDevice ({Config.Name})|OnTagChangeAdd|Error can't put a trigger on a input tag");
            }
            if (!tagsAction.ContainsKey(tagName))
                tagsAction[tagName] = triggerAction;
            else
                tagsAction[tagName] += triggerAction;
        }

        public void OnTagChangeRemove(string tagName, Action<string, object> triggerAction)
        {
            tagsAction[tagName] -= triggerAction;
        }

        public bool ContainsTag(string tagName)
        {
            return Config.Tags.Any(x => x.Name == tagName);
        }

        public DeviceTagConfig GetTagConfig(string name)
        {
            return Config.Tags.FirstOrDefault(x => x.Name == name);
        }

        public object GetTagValue(string tagName)
        {
            return tagValues[tagName];
        }

        public void SetTagValue(string tagName, object value)
        {
            tagValues[tagName] = value;

            if(!tagVersion.ContainsKey(tagName))
                tagVersion[tagName] = 0;
            else
                tagVersion[tagName] += 1;

            Logger.Debug($"FileDevice ({Config.Name})|SetTagValue|TagName: '{tagName}'|TagValues: '{value}'");

            // output_content
            // output_name

            if (tagName == "output_content" || tagName == "output_name")
            {
                if (tagVersion["output_content"] == tagVersion["output_name"])
                {
                    try
                    {
                        if (!Directory.Exists(resultPath))
                        {
                            Directory.CreateDirectory(resultPath);
                        }

                        File.Create(resultPath + "saida.txt " + tagValues["output_content"]);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error create result Path|Exception|Message: {ex.Message}");
                    }

                }
            }
        }

        private void SaveFile(string filename) {
            try
            {
                if (!File.Exists(outputPath))
                {
                    using (FileStream fs = File.Create(outputPath)) { }
                }

                File.Move(inputPath + filename, outputPath);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error move file|FileName ({filename})|Exception|Message: {ex.Message}");
            }
        }

        public void GetLicenseRemainingHours()
        {
            LicenseRemainingHours = int.MaxValue;
        }

        #endregion Legacy
    }
}

