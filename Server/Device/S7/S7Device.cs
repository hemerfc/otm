using System;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using Otm.Server.ContextConfig;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Otm.Shared.ContextConfig;
using System.Collections.Concurrent;
using Otm.Shared.Status;

namespace Otm.Server.Device.S7
{
    public class S7Device : IDevice
    {
        public string Name { get { return Config.Name; } }

        public BackgroundWorker Worker { get; private set; }

        private DeviceConfig Config;

        //public DeviceColector Colector { get; }

        private ConcurrentDictionary<int, DB> dbDict;
        private IS7Client client;
        private readonly ConcurrentDictionary<string, Action<string, object>> tagsAction;
        // private readonly Dictionary<string, object> tagValues;
        private readonly ConcurrentDictionary<string, int> tagDbIndex;

        public Stopwatch Stopwatch { get; }

        private string host;
        private int rack;
        private int slot;
        private DateTime? connError = null;
        private ILogger Logger;
        private bool firstLoadRead;
        private bool firstLoadWrite;

        public bool Ready { get; private set; }


        private object tagsActionLock;

        public bool Enabled { get { return true; } }
        public bool Connected { get { return client?.Connected ?? false; } }
        public DateTime LastErrorTime { get { return DateTime.Now; } }

        public IReadOnlyDictionary<string, object> TagValues { get { return null; } }

        public string UniqueDeviceId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int LicenseRemainingHours { get; set; }
        public DateTime? LastUpdateDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public S7Device()
        {
            this.tagsAction = new ConcurrentDictionary<string, Action<string, object>>();
            this.tagDbIndex = new ConcurrentDictionary<string, int>();
            this.Stopwatch = new Stopwatch();
            firstLoadRead = true;
            firstLoadWrite = true;
            Ready = false;
            tagsActionLock = new object();
        }

        public void Init(DeviceConfig dvConfig, IS7Client client, ILogger logger)
        {
            this.client = client;
            Init(dvConfig, logger);
        }

        public void Init(DeviceConfig dvConfig, ILogger logger)
        {
            this.Logger = logger;
            this.Config = dvConfig;
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

        private ConcurrentDictionary<int, DB> GetDBFromConfig(DeviceConfig dvConfig)
        {
            var dict = new ConcurrentDictionary<int, DB>();

            if (dvConfig.Tags != null)
                GetDeviceTags(dvConfig, dict);

            return dict;
        }

        private void GetDeviceTags(DeviceConfig dvConfig, ConcurrentDictionary<int, DB> dict)
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
                    Logger.LogError($"Dev {dvConfig.Name}: Tag parse error Tag:{t}");
                }
                else
                {
                    var it = new DBItem
                    {
                        Offset = byteOffset,
                        BitOffset = bitOffset,
                        TypeCode = t.TypeCode,
                        Name = t.Name
                    };

                    if (it.TypeCode == TypeCode.String)
                        it.Value = null;
                    else
                    {
                        var type = Type.GetType("System." + Enum.GetName(typeof(TypeCode), it.TypeCode));
                        it.Value = Activator.CreateInstance(type);
                    }

                    switch (g["g3"].Value)
                    {
                        case "dw":
                            it.Lenght = 4;
                            break;
                        case "w":
                            it.Lenght = 2;
                            break;
                        case "x":
                            it.Lenght = 1;
                            break;
                        default:
                            Logger.LogError($"Dev {dvConfig.Name}: Tag parse error Tag {t.Name} {t.Address}");
                            break;
                    }

                    if (!dict.ContainsKey(dbValue))
                    {
                        dict[dbValue] = new DB
                        {
                            Number = dbValue,
                            Mode = t.Mode,
                            Itens = new ConcurrentDictionary<string, DBItem>()
                        };
                    }
                    else
                    {
                        if (dict[dbValue].Mode != t.Mode)
                        {
                            var err = $"Dev {dvConfig.Name}: Tag parse error Tag {t.Name} {t.Address}, can't have in and out tag in same DB!";
                            throw new Exception(err);
                        }
                    }

                    if (!dict[dbValue].Itens.Keys.Contains(t.Name))
                    {
                        dict[dbValue].Itens[t.Name] = it;
                        tagDbIndex[t.Name] = dbValue;
                    }
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

        public void Start(BackgroundWorker worker)
        {
            // backgroud worker
            Worker = worker;
            long count = 0;
            long readTime = 0;
            long writeTime = 0;

            while (true)
            {
                try
                {
                    if (this.Connected)
                    {
                        Stopwatch.Restart();

                        // read device tag's
                        ReadDeviceTags();

                        readTime += Stopwatch.ElapsedMilliseconds;

                        // write tags
                        WriteDeviceTags();

                        writeTime += Stopwatch.ElapsedMilliseconds;
                        count += 1;

                        if (count >= 10)
                        {
                            //Colector.Write(readTime / count, writeTime / count);
                            count = 0;
                            readTime = 0;
                            writeTime = 0;
                        }

                        Ready = true;
                    }
                    else
                    {
                        Ready = false;
                        count = 0;
                        readTime = 0;
                        writeTime = 0;

                        this.Reconnect();
                        ReadDeviceTags();
                    }
                    // wait 100ms
                    /// TODO: wait time must be equals the minimum update rate of tags
                    var waitEvent = new ManualResetEvent(false);
                    waitEvent.WaitOne(50);

                    if (Worker.CancellationPending)
                    {
                        Ready = false;
                        Stop();
                        return;
                    }

                    //ErrorInUpdateLoop = false;
                }
                catch (Exception ex)
                {
                    Ready = false;
                    Logger.LogError($"Dev {Config.Name}: Update Loop Error {ex}");
                    client.Disconnect();
                }
            }
        }

        public void Stop()
        {
            //
        }

        public void ReadDeviceTags()
        {
            // get dbs
            foreach (var db in this.dbDict.Values)
            {
                var err = client.DBRead(db.Number, 0, db.Lenght, db.Buffer);

                if (err == 0)
                {
                    if (db.Mode == Modes.ToOTM) // from device to OTM
                    {
                        foreach (var dbItem in db.Itens.Values)
                        {
                            dbItem.OldValue = dbItem.Value;
                            switch (dbItem.TypeCode)
                            {
                                case TypeCode.Byte:
                                    dbItem.Value = S7.GetByteAt(db.Buffer, dbItem.Offset);
                                    //tagValues[dbItem.Name] = dbItem.Value;
                                    break;
                                case TypeCode.Int32:
                                    dbItem.Value = S7.GetIntAt(db.Buffer, dbItem.Offset);
                                    //tagValues[dbItem.Name] = dbItem.Value;
                                    break;
                                case TypeCode.Decimal:
                                    dbItem.Value = S7.GetRealAt(db.Buffer, dbItem.Offset);
                                    //tagValues[dbItem.Name] = dbItem.Value;
                                    break;
                                case TypeCode.Boolean:
                                    dbItem.Value = S7.GetBitAt(db.Buffer, dbItem.Offset, dbItem.BitOffset);
                                    //tagValues[dbItem.Name] = dbItem.Value;
                                    break;
                                case TypeCode.String:
                                    dbItem.Value = S7.GetStringAt(db.Buffer, dbItem.Offset);
                                    //tagValues[dbItem.Name] = dbItem.Value;
                                    break;
                                default:
                                    //tagValues[dbItem.Name] = null;
                                    var msg = $"Dev {Config.Name}: Get value error. Tag {dbItem.Name}";
                                    throw new Exception(msg);
                            }
                        }

                        // this is the first execution of ReadDeviceTags?
                        if (!firstLoadRead)
                        {
                            // executa as acoes apos o loop de update
                            foreach (var dbItem in db.Itens.Values)
                            {
                                if (!dbItem.Value.Equals(dbItem.OldValue))
                                {
                                    if (tagsAction.ContainsKey(dbItem.Name))
                                    {
                                        lock (tagsActionLock)
                                        {
                                            tagsAction[dbItem.Name](dbItem.Name, dbItem.Value);
                                        }
                                    }
                                }
                            }
                        }
                        else
                            firstLoadRead = false;
                    }
                }
                else
                {
                    var msg = $"Error on read db {db.Number}. Error {client.ErrorText(err)}";
                    throw new Exception(msg);
                }
            }
        }
        private void WriteDeviceTags()
        {
            // get dbs
            foreach (var db in this.dbDict.Values)
            {
                // this is the first execution of WriteDeviceTags?
                if (firstLoadWrite)
                {
                    var errRead = client.DBRead(db.Number, 0, db.Lenght, db.Buffer);

                    if (errRead != 0)
                    {
                        var msg = $"Error on read db {db.Number}. Error {client.ErrorText(errRead)}";
                        throw new Exception(msg);
                    }
                    else
                        firstLoadWrite = false;
                }

                if (db.Mode == Modes.FromOTM) // from OTM to device
                {
                    foreach (var dbItem in db.Itens.Values)
                    {
                        if (dbItem.Value != null/* && !dbItem.Value.Equals(dbItem.OldValue)*/)
                        {
                            dbItem.OldValue = dbItem.Value;

                            switch (dbItem.TypeCode)
                            {
                                case TypeCode.Byte:
                                    S7.SetByteAt(db.Buffer, dbItem.Offset, Convert.ToByte(dbItem.Value));
                                    break;
                                case TypeCode.Int32:
                                    S7.SetIntAt(db.Buffer, dbItem.Offset, (short)((int)dbItem.Value));
                                    break;
                                case TypeCode.Decimal:
                                    S7.SetRealAt(db.Buffer, dbItem.Offset, (float)dbItem.Value);
                                    break;
                                case TypeCode.Boolean:
                                    S7.SetBitAt(ref db.Buffer, dbItem.Offset, dbItem.BitOffset, (bool)dbItem.Value);
                                    break;
                                case TypeCode.String:
                                    S7.SetStringAt(db.Buffer, dbItem.Offset, ((string)dbItem.Value).Length, (string)dbItem.Value);
                                    /// TODO: Create a property to limit lenght of a string
                                    break;
                                default:
                                    //tagValues[dbItem.Name] = null;
                                    var msg = $"Dev {Config.Name}: Set value error. Tag {dbItem.Name}";
                                    throw new Exception(msg);
                            }
                        }
                    }

                    var errWrite = client.DBWrite(db.Number, 0, db.Lenght, db.Buffer);

                    if (errWrite != 0)
                    {
                        var msg = $"Error on write db {db.Number}. Error {client.ErrorText(errWrite)}";
                        throw new Exception(msg);
                    }
                }
            }
        }

        public DeviceTagConfig GetTagConfig(string name)
        {
            return Config.Tags.FirstOrDefault(x => x.Name == name);
        }

        public object GetTagValue(string tagName)
        {
            var dbIdx = tagDbIndex[tagName];
            return dbDict[dbIdx].Itens[tagName].Value;
        }

        public void SetTagValue(string tagName, object value)
        {
            var dbIdx = tagDbIndex[tagName];
            dbDict[dbIdx].Itens[tagName].Value = value;
        }

        private void Reconnect()
        {
            firstLoadWrite = true;
            firstLoadRead = true;

            int res = client.ConnectTo(this.host, this.rack, this.slot);

            if (res != 0)
            {
                if (connError == null)
                {
                    var err = client.ErrorText(res);
                    connError = DateTime.Now;

                    Logger.LogError($"Dev {Config.Name}: Connection error. {err}");
                }
            }
            else
            {
                connError = null;

                Logger.LogError($"Dev {Config.Name}: Connected.");
            }
        }

        public void OnTagChangeAdd(string tagName, Action<string, object> triggerAction)
        {
            var tagConfig = GetTagConfig(tagName);

            // can't use a output tag as trigger, output put tags are writed to PLC
            if (tagConfig.Mode == Modes.FromOTM) // from OTM to device
            {
                throw new Exception("Error can't put a trigger on a input tag");
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

        public void GetLicenseRemainingHours()
        {
            LicenseRemainingHours = int.MaxValue;
        }

        private class DB
        {
            public int Number;
            public int Lenght;
            public Modes Mode;
            public byte[] Buffer;
            public ConcurrentDictionary<string, DBItem> Itens;
        }

        private class DBItem
        {
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public TypeCode TypeCode { get; set; }
            public int Offset { get; set; }
            public int Lenght { get; set; }
            public int BitOffset { get; set; }
            public object Value { get; set; }
            public object OldValue { get; set; }
            public string Name { get; set; }
        }
    }
}

