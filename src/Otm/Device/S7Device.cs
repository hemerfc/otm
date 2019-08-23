using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using Otm.Config;
using Otm.Logger;
using Snap7;

namespace Otm.Device
{
    public class S7Device : IDevice
    {

        public ILoggerFactory LoggerFactory { get; }
        public ILogger Logger { get; private set; }
        public bool Connected { get { return client?.Connected ?? false; } }

        private DeviceConfig Config;
        private Dictionary<int, DB> dbDict;
        private readonly IS7Client client;
        private readonly Dictionary<string, Action<string, object>> tagsAction;
        private readonly Dictionary<string, object> tagValues;
        private string host;
        private int rack;
        private int slot;
        private DateTime? connError = null;

        public S7Device(DeviceConfig dvConfig, IS7ClientFactory clientFactory, ILoggerFactory loggerFactory)
        {
            this.LoggerFactory = loggerFactory;
            this.Logger = LoggerFactory.GetCurrentClassLogger();
            this.Config = dvConfig;
            this.client = clientFactory.CreateClient();
            this.tagsAction = new Dictionary<string, Action<string, object>>();
            this.tagValues = new Dictionary<string, object>();
            GetConfig(dvConfig);
        }

        private void GetConfig(DeviceConfig dvConfig)
        {
            // dvConfig.Config = host=192.168.1.1;rack=0;slot=0
            var cparts = dvConfig.Config.Split(';');

            this.host = (cparts.FirstOrDefault(x => x.Contains("host=")) ?? "").Replace("host=", "").Trim();
            var strRack = (cparts.FirstOrDefault(x => x.Contains("rack=")) ?? "").Replace("rack=", "").Trim();
            var strSlot = (cparts.FirstOrDefault(x => x.Contains("slot=")) ?? "").Replace("slot=", "").Trim();

            this.rack = 0;
            int.TryParse(strRack, out this.rack);
            this.slot = 0;
            int.TryParse(strSlot, out this.slot);

            this.dbDict = GetDBFromConfig(dvConfig);
        }

        private Dictionary<int, DB> GetDBFromConfig(DeviceConfig dvConfig)
        {
            var dict = new Dictionary<int, DB>();

            if (dvConfig.Tags != null)
                GetDeviceTags(dvConfig, dict);

            return dict;
        }

        private void GetDeviceTags(DeviceConfig dvConfig, Dictionary<int, DB> dict)
        {
            foreach (var t in dvConfig.Tags)
            {
                // regex 
                var strRegex = "^(?<g1>[a-z]+)(?<g2>[0-9]+)\\.(?<g3>[a-z]+)(?<g4>[0-9]+)\\.?(?<g5>[0-9]+)?";
                Regex regex = new Regex(strRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var validG1 = new String[] { "db" };
                var validG3 = new String[] { "dw", "w", "x", "b" };

                var match = regex.Match(t.Address);
                var g = match.Groups;
                var vg1 = validG1.Contains(g["g1"].Value);
                var vg2 = int.TryParse(g["g2"].Value, out var dbValue);
                var vg3 = validG3.Contains(g["g3"].Value);
                var vg4 = int.TryParse(g["g4"].Value, out var byteOffset);

                var vg5 = true;
                int bitOffset = 0;
                if (g["g5"].Success)
                {
                    if (g["g3"].Value == "x")
                        vg5 = int.TryParse(g["g5"].Value, out bitOffset);
                    else
                    {
                        vg5 = false;
                        vg3 = false;
                    }
                }

                if (!vg1 || !vg2 || !vg3 || !vg4 || !vg5)
                {
                    Logger.Error($"Dev {dvConfig.Name}: Tag parse error Tag:{t}");
                }
                else
                {
                    var it = new DBItem();
                    it.Offset = byteOffset;
                    it.BitOffset = bitOffset;
                    it.Type = t.Type;

                    switch (g["g3"].Value)
                    {
                        case "dw":
                            it.Lenght = 2;
                            break;
                        case "w":
                        case "x":
                            it.Lenght = 1;
                            break;
                        default:
                            Logger.Error($"Dev {dvConfig.Name}: Tag parse error Tag {t.Name} {t.Address}");
                            break;
                    }

                    if (!dict.ContainsKey(dbValue))
                    {
                        dict[dbValue] = new DB();
                        dict[dbValue].Mode = t.Mode;
                        dict[dbValue].Itens = new Dictionary<string, DBItem>();
                    } else 
                    {
                        if (dict[dbValue].Mode != t.Mode)
                        {
                            var err = $"Dev {dvConfig.Name}: Tag parse error Tag {t.Name} {t.Address}, can't have in and out tag in same DB!";
                            throw new Exception(err);
                        }
                    }

                    if (!dict[dbValue].Itens.Keys.Contains(t.Name))
                        dict[dbValue].Itens[t.Name] = it;
                    else                
                        throw new Exception($"Dev {dvConfig.Name}: Duplicated tag {t.Name}");

                    if (dict[dbValue].Lenght < it.Offset + it.Lenght)
                        dict[dbValue].Lenght = it.Offset + it.Lenght;
                }

                foreach (var db in dict)
                {
                    db.Value.Buffer = new byte[db.Value.Lenght];
                }
            }
        }

        public void UpdateTags()
        {
            if (this.Connected)
            {
                // get dbs
                foreach (var db in this.dbDict)
                {
                    var err = client.DBRead(db.Key, 0, db.Value.Lenght, db.Value.Buffer);

                    if (err == 0)
                    {
                        if (db.Value.Mode == "out") 
                        {
                            foreach(var tag in db.Value.Itens)
                            {
                                tag.Value.OldValue = tag.Value.Value;
                                switch (tag.Value.Type)
                                {
                                    case "int":
                                        tag.Value.Value = S7.GetIntAt(db.Value.Buffer, tag.Value.Offset);                                    
                                        tagValues[tag.Key] = tag.Value.Value;
                                        break;
                                    case "real":
                                        tag.Value.Value = S7.GetRealAt(db.Value.Buffer, tag.Value.Offset);                                    
                                        tagValues[tag.Key] = tag.Value.Value;
                                        break;
                                    case "bool":
                                        tag.Value.Value = S7.GetBitAt(db.Value.Buffer, tag.Value.Offset, tag.Value.BitOffset);                                    
                                        tagValues[tag.Key] = tag.Value.Value;
                                        break;
                                    default:
                                        tagValues[tag.Key] = null;
                                        Logger.Error($"Dev {Config.Name}: Get value error. Tag {tag.Key}");
                                        break;
                                }

                                if (tag.Value.Value != tag.Value.OldValue)
                                {
                                    if (tagsAction.ContainsKey(tag.Key))
                                    {
                                        tagsAction[tag.Key](tag.Key, tag.Value.Value);
                                    }
                                } 
                            }
                        }
                    }
                    else
                    {
                        Logger.Error($"Dev {Config.Name}: Error on read db {db.Value.Number}. Error {client.ErrorText(err)}");
                    }
                }
            }
            else
            {
                this.Reconnect();
            }
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
        }

        private void Reconnect()
        {
            int res = client.ConnectTo(this.host, this.rack, this.slot);

            if (res != 0)
            {
                if (connError == null)
                { 
                    var err = client.ErrorText(res);
                    connError = DateTime.Now;

                    Logger.Error($"Dev {Config.Name}: Connection error. {err}");
                }
            }
            else
            {
                connError = null;

                Logger.Error($"Dev {Config.Name}: Connected.");
            }
        }

        public void OnTagChangeAdd(string tagName, Action<string, object> triggerAction)
        {
            var tagConfig = GetTagConfig(tagName);

            // can't use a input tag as trigger, input put tags are writed to PLCn
            if (tagConfig.Mode == "in") 
            {
                throw new Exception("Error can't put a trigger on a input tag");
            }
            tagsAction[tagName] += triggerAction;
        }

        public void OnTagChangeRemove(string tagName, Action<string, object> triggerAction)
        {
            tagsAction[tagName] -= triggerAction;
        }

        public bool ContainsTag(string tagName)
        {
            return tagsAction.ContainsKey(tagName);
        }

        public class DB
        {
            public int Number;
            public int Lenght;
            public string Mode;
            public byte[] Buffer;
            public Dictionary<string, DBItem> Itens;
        }

        public class DBItem
        {
            public string Type { get; set; }
            public int Offset { get; set; }
            public int Lenght { get; set; }
            public int BitOffset { get; set; }
            public object Value  { get; set; }
            public object OldValue  { get; set; }
        }
    }   
}

