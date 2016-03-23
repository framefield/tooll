// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

//#define USE_COUCHDB
//#define USE_SOCKETS

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Framefield.Core
{
    public interface ICommand
    {
        string Name { get; }
        bool IsUndoable { get; }
        void Undo();
        void Do();
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class PersistentCommand
    {
        public PersistentCommand() { }
        public ICommand Command;
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class MacroCommand : ICommand
    {
        public MacroCommand() { }

        public MacroCommand(string name, IEnumerable<ICommand> commands)
        {
            _name = name;
            _commands = commands.ToList();
        }

        public string Name { get { return _name; } }

        public bool IsUndoable
        {
            get
            {
                return _commands.Aggregate(true, (result, current) => result && current.IsUndoable);
            }
        }

        public void Do()
        {
            _commands.ForEach(c => c.Do());
        }

        public void Undo()
        {
            var tmpCmds = new List<ICommand>(_commands);
            tmpCmds.Reverse();
            tmpCmds.ForEach(c => c.Undo());
        }

        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        internal List<ICommand> Commands { get { return _commands; } set { _commands = value; } }

        protected string _name = String.Empty;

        protected List<ICommand> _commands = new List<ICommand>();
    }

    public class UndoRedoStack : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region properties
#if USE_COUCHDB
        public bool CanUndo { get { return (_undoStackDB["Entries"] as JArray).Count > 0; } }
        public bool CanRedo { get { return (_redoStackDB["Entries"] as JArray).Count > 0; } }
        public IEnumerable<JToken> UndoList { get { return _undoStackDB["Entries"] as JArray; } }
        public IEnumerable<JToken> RedoList { get { return _redoStackDB["Entries"] as JArray; } }
#else
        public bool CanUndo { get { return _undoStack.Count > 0; } }
        public bool CanRedo { get { return _redoStack.Count > 0; } }
        public IEnumerable<string> UndoList { get { return from command in _undoStack select command.Name; } }
        public IEnumerable<string> RedoList { get { return from command in _redoStack select command.Name; } }
#endif
        #endregion

        public UndoRedoStack(bool sender = true)
        {
#if USE_COUCHDB
            if (!_couchDB.IsDBExisting("commands"))
            {
                _couchDB.CreateDatabase("commands");
            }

            try
            {
                _undoStackDB = JObject.Parse(_couchDB.GetDocument(_dbName, _undoStackDocId, string.Empty));
            }
            catch (Exception)
            {
                var rev = _couchDB.StoreDocument(_dbName, _undoStackDocId, "{ \"Entries\" : [] }").Item2;
                _undoStackDB = JObject.Parse(_couchDB.GetDocument(_dbName, _undoStackDocId, string.Empty));
            }

            try
            {
                _redoStackDB = JObject.Parse(_couchDB.GetDocument(_dbName, _redoStackDocId, string.Empty));
            }
            catch (Exception)
            {
                var rev = _couchDB.StoreDocument(_dbName, _redoStackDocId, "{ \"Entries\" : [] }").Item2;
                _redoStackDB = JObject.Parse(_couchDB.GetDocument(_dbName, _redoStackDocId, string.Empty));
            }
#endif
#if USE_SOCKETS
            if (!sender)
            {
                _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, PORT);
                _listenerSocket.Bind(ipLocal);
                _listenerSocket.Listen(4);
                _listenerSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
            }
#endif
        }

        public void Dispose()
        {
#if USE_SOCKETS
            if (_listenerSocket != null)
                _listenerSocket.Close();
            if (_receiverSocket != null)
                _receiverSocket.Close();
            _senderSockets.ForEach(socket => socket.Close());
#endif
        }

#if USE_SOCKETS
        private const int PORT = 8221;

        public void AddSocketToRemoteReceiver(IPAddress ipAddressOfRemoteReceiver)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IAsyncResult result = socket.BeginConnect(ipAddressOfRemoteReceiver, PORT, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(1000, true);
            if (success)
            {
                _senderSockets.Add(socket);
            }
            else
            {
                Logger.Error("Failed to connect to client {0}.", ipAddressOfRemoteReceiver.ToString());
            }
        }

        public void OnClientConnect(IAsyncResult asyn)
        {
            try
            {
                _receiverSocket = _listenerSocket.EndAccept(asyn);
                WaitForData(_receiverSocket);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\n OnClientConnection: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                System.Diagnostics.Debugger.Log(0, "1", se.Message);
            }

        }

        Byte[] _buffer = new Byte[1024 * 100]; // 100k

        public void WaitForData(System.Net.Sockets.Socket soc)
        {
            try
            {
                // now start to listen for any data...
                soc.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(OnDataReceived), this);
            }
            catch (SocketException se)
            {
                System.Diagnostics.Debugger.Log(0, "1", se.Message);
            }

        }

        public void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                int iRx = 0;
                iRx = _receiverSocket.EndReceive(asyn);
                char[] chars = new char[iRx];
                var decoder = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = decoder.GetChars(_buffer, 0, iRx, chars, 0);
                var szData = new System.String(chars);
                var splittedCommands = szData.Split(new string[] { _messageTerminator }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var cmd in splittedCommands)
                {
                    EnqueReceivedCommand(cmd);
                }
                WaitForData(_receiverSocket);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                System.Diagnostics.Debugger.Log(0, "1", se.Message);
            }
        }
#endif




        private Assembly ResolveEventHandler(object sender, ResolveEventArgs args)
        {
            var namespaceGuid = GetNamespaceGuidFromFunctionAssemblyName(args.Name);
            var namespaceToLookFor = "Framefield.Core.ID" + namespaceGuid.Replace('-', '_');
            var asm = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                       from type in assembly.GetTypes()
                       where type.Namespace == namespaceToLookFor
                       select assembly).FirstOrDefault();

            Logger.Debug("'ResolveEventHandler' called for assembly '{0}' which was resolved with assembly '{1}' from current domain.", args.Name, (asm != null) ? asm.FullName : string.Empty);

            return asm;
        }


        internal static string GetNamespaceGuidFromFunctionAssemblyName(string input)
        {
            Regex idSearchPattern = new Regex(@"ID([0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12})");
            var match = idSearchPattern.Match(input);
            return match.Groups[1].Value;
        }


        #region public methods
        public void AddAndExecute(ICommand command)
        {
            Add(command);

            command.Do();
        }

#if USE_SOCKETS
        private Queue<string> _receivedCommandsQueue = new Queue<string>();

        private void EnqueReceivedCommand(string jsonCommand)
        {
            lock (_receivedCommandsQueue)
            {
                _receivedCommandsQueue.Enqueue(jsonCommand);
            }
        }

        public bool ProcessReceivedCommands()
        {
            lock (_receivedCommandsQueue)
            {
                bool wereElementsToProcessed = _receivedCommandsQueue.Count > 0;
                AppDomain.CurrentDomain.AssemblyResolve += ResolveEventHandler;

                while (_receivedCommandsQueue.Count > 0)
                {
                    string jsonCommand = _receivedCommandsQueue.Dequeue();
                    var command = JsonConvert.DeserializeObject<PersistentCommand>(jsonCommand, _serializerSettings);
                    //                     StoreCmdInDB(jsonCommand, "Undo");
                    command.Command.Do();
                    Logger.Info("Executed command received from remote: {0}", command.Command.Name);
                }

                AppDomain.CurrentDomain.AssemblyResolve -= ResolveEventHandler;
                return wereElementsToProcessed;
            }
        }

        string _messageTerminator = "_\"\"\"_";
#endif

        public void Add(ICommand command)
        {
            if (command.IsUndoable)
            {
                var jsonCommand = SerializeCommand(command);
#if USE_COUCHDB
                StoreCmdInDB(jsonCommand, "Undo");
#else
                _undoStack.Push(command);
                _redoStack.Clear();
#endif
#if USE_SOCKETS
                if (_senderSockets.Count > 0)
                {
                    var closedSockets = new List<Socket>();
                    var bytes = System.Text.Encoding.ASCII.GetBytes(jsonCommand + _messageTerminator);
                    foreach (var socket in _senderSockets)
                    {
                        try
                        {
                            socket.Send(bytes);
                        }
                        catch (System.Net.Sockets.SocketException e)
                        {
                            Logger.Warn("Socket connection lost: {0}", e.Message);
                            closedSockets.Add(socket);
                        }
                    }

                    foreach (var socket in closedSockets)
                    {
                        _senderSockets.Remove(socket);
                        socket.Dispose();
                    }
                }
#endif
            }
            else
            {
                Clear();
            }
            NotifyAll();
        }

        public void Undo()
        {
#if USE_COUCHDB
            var undoEntries = _undoStackDB["Entries"] as JArray;
            if (undoEntries.Count > 0)
            {
                var cmdEntry = undoEntries[0];
                var cmd = GetCmdFromDB(cmdEntry.Value<string>("CmdID"), cmdEntry.Value<string>("CmdRev"));
                cmd.Undo();
                undoEntries.RemoveAt(0);
                var redoEntries = _redoStackDB["Entries"] as JArray;
                redoEntries.AddFirst(cmdEntry);
                UpdateUndoStackDB();
                UpdateRedoStackDB();
            }
#else
            if (_undoStack.Count > 0) {
                var command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);
            }
#endif
            NotifyAll();
        }

        public void Redo()
        {
#if USE_COUCHDB
            var redoEntries = _redoStackDB["Entries"] as JArray;
            if (redoEntries.Count > 0)
            {
                var cmdEntry = redoEntries[0];
                var cmd = GetCmdFromDB(cmdEntry.Value<string>("CmdID"), cmdEntry.Value<string>("CmdRev"));
                cmd.Do();
                redoEntries.RemoveAt(0);
                var undoEntries = _undoStackDB["Entries"] as JArray;
                undoEntries.AddFirst(cmdEntry);
                UpdateUndoStackDB();
                UpdateRedoStackDB();
            }
#else
            if (_redoStack.Count > 0) {
                var command = _redoStack.Pop();
                command.Do();
                _undoStack.Push(command);
            }
#endif
            NotifyAll();
        }

        public void Clear()
        {
#if USE_COUCHDB
#else
            _undoStack.Clear();
            _redoStack.Clear();
#endif
            NotifyAll();
        }
        #endregion

        #region private stuff
        List<string> _properties = new List<string>() { "CanUndo", "CanRedo", "UndoList", "RedoList" };
        private void NotifyAll()
        {
            foreach (var property in _properties)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }


        private string SerializeCommand(ICommand cmd)
        {
            var persistentCmd = new PersistentCommand() { Command = cmd };
            var jsonCommand = JsonConvert.SerializeObject(persistentCmd, Formatting.Indented, _serializerSettings);
            return jsonCommand;
        }

#if USE_COUCHDB

        private void StoreCmdInDB(string jsonCommand, string queueType)
        {
            string cmdID = Guid.NewGuid().ToString();
            var result = _couchDB.StoreDocument(_dbName, cmdID, jsonCommand);

            var undoStackEntry = new JObject(
                                     new JProperty("CmdID", result.Item1),
                                     new JProperty("CmdRev", result.Item2));

            var entries = _undoStackDB["Entries"] as JArray;
            entries.AddFirst(undoStackEntry);
            UpdateUndoStackDB();
        }

        private void UpdateUndoStackDB()
        {
            _undoStackDB["_rev"] = _couchDB.StoreDocument(_dbName, _undoStackDocId, _undoStackDB.ToString()).Item2;
        }

        private void UpdateRedoStackDB()
        {
            _redoStackDB["_rev"] = _couchDB.StoreDocument(_dbName, _redoStackDocId, _redoStackDB.ToString()).Item2;
        }

        private ICommand GetCmdFromDB(string cmdID, string cmdRev)
        {
            // enable resolve handler in order to get notified when deserializing types defined in operators
            // This is the case for specific implementations of IOperatorState (e.g. CurveState). These are 
            // matched in the event handler with the existing compiled assembly in current domain. The matching
            // is done via namespace
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEventHandler;

            // read from db
            var response = _couchDB.GetDocument(_dbName, cmdID, cmdRev);
            var command = JsonConvert.DeserializeObject<PersistentCommand>(response, _serializerSettings);

            AppDomain.CurrentDomain.AssemblyResolve -= ResolveEventHandler;

            return command.Command;
        }


        CouchDB _couchDB = new CouchDB() { ServerUrl = "http://localhost:5984" };
        string _dbName = "commands";
        string _undoStackDocId = "UndoStack";
        JObject _undoStackDB = null;
        string _redoStackDocId = "RedoStack";
        JObject _redoStackDB = null;
#else
        private Stack<ICommand> _undoStack = new Stack<ICommand>();
        private Stack<ICommand> _redoStack = new Stack<ICommand>();
#endif
        #endregion

        JsonSerializerSettings _serializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto };
#if USE_SOCKETS
        List<Socket> _senderSockets = new List<Socket>();
        private Socket _listenerSocket;
        private Socket _receiverSocket;
#endif

    }
}
