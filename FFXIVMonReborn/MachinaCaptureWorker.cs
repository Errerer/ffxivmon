﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Machina;
using Machina.FFXIV;

namespace FFXIVMonReborn
{
    class MachinaCaptureWorker
    {
        [Flags]
        public enum ConfigFlags
        {
            None = 0,
            StripHeaderActors = 1,
            DontUsePacketTimestamp = 2
        }

        private readonly XivMonTab _myTab;
        private readonly TCPNetworkMonitor.NetworkMonitorType _monitorType;

        private volatile bool _shouldStop;

        public MachinaCaptureWorker(XivMonTab window, TCPNetworkMonitor.NetworkMonitorType monitorType)
        {
            this._myTab = window;
            this._monitorType = monitorType;
        }

        private void MessageReceived(long epoch, byte[] message, int set)
        {
            var res = Parse(message);

            PacketListItem item = new PacketListItem() { IsVisible = true, ActorControl = -1, Data = message, MessageCol = res.header.MessageType.ToString("X4"), DirectionCol = "S",
                CategoryCol = set.ToString(), TimeStampCol = Util.UnixTimeStampToDateTime(res.header.Seconds).ToString(@"MM\/dd\/yyyy HH:mm:ss"), SizeCol = res.header.MessageLength.ToString(), 
                Set = set, RouteIdCol = res.header.RouteID.ToString() };

            _myTab.Dispatcher.Invoke(new Action(() => { _myTab.AddPacketToListView(item); }));
        }

        private void MessageSent(long epoch, byte[] message, int set)
        {
            var res = Parse(message);

            PacketListItem item = new PacketListItem() { IsVisible = true, ActorControl = -1, Data = message, MessageCol = res.header.MessageType.ToString("X4"), DirectionCol = "C",
                CategoryCol = set.ToString(), TimeStampCol = Util.UnixTimeStampToDateTime(res.header.Seconds).ToString(@"MM\/dd\/yyyy HH:mm:ss"), SizeCol = res.header.MessageLength.ToString(),
                Set = set, RouteIdCol = res.header.RouteID.ToString() };

            _myTab.Dispatcher.Invoke(new Action(() => { _myTab.AddPacketToListView(item); }));
        }

        private static ParseResult Parse(byte[] data)
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            FFXIVMessageHeader head = (FFXIVMessageHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(FFXIVMessageHeader));
            handle.Free();

            ParseResult result = new ParseResult();
            result.header = head;
            result.data = data;

            return result;
        }

        public void Run()
        {
            FFXIVNetworkMonitor monitor = new FFXIVNetworkMonitor();
            monitor.MonitorType = _monitorType;
            monitor.MessageReceived = (long epoch, byte[] message, int set) => MessageReceived(epoch, message, set);
            monitor.MessageSent = (long epoch, byte[] message, int set) => MessageSent(epoch, message, set);
            monitor.Start();

            while (!_shouldStop);

            Console.WriteLine("MachinaCaptureWorker: Terminating");
            monitor.Stop();
        }

        public void Stop()
        {
            _shouldStop = true;
        }

        internal class ParseResult
        {
            public FFXIVMessageHeader header;
            public byte[] data;
        }
    }

    public class PacketListItem
    {
        public byte[] Data;
        public bool IsVisible { get; set; } = true;
        public int ActorControl { get; set; }
        public int Set { get; set; }

        public string DirectionCol { get; set; }
        public string MessageCol { get; set; }
        public string NameCol { get; set; }
        public string RouteIdCol { get; set; }
        public string CommentCol { get; set; }
        public string SizeCol { get; set; }
        public string CategoryCol { get; set; }
        public string TimeStampCol { get; set; }

    }
}
