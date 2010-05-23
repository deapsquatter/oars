﻿using System;
using System.Runtime.InteropServices;

namespace Oars.Core
{
    public enum BufferEventFlushMode
    {
        Normal = 0,
        Flush = 1,
        Finished = 2
    }

    public class BufferEventEventArgs : EventArgs
    {
        public BufferEventEvents Events { get; private set; }

        internal BufferEventEventArgs(BufferEventEvents events)
        {
            Events = events;
        }
    }

    public class BufferEvent : IDisposable
    {
        public EventBase EventBase { get; private set; }
        IntPtr bev;

        EVBuffer input;
        EVBuffer output;

        public event EventHandler Read;
        public event EventHandler Write;
        public event EventHandler<BufferEventEventArgs> Event;

        public EVBuffer Input { get { return input ?? (input = new EVBuffer(bufferevent_get_input(bev))); } }
        public EVBuffer Output { get { return output ?? (output = new EVBuffer(bufferevent_get_output(bev))); } }

        int readLow, readHigh = -1;

        public int ReadLowWatermark
        {
            get { return readLow; }
            set { readLow = value; SetReadWatermark(); }
        }

        public int ReadHighWatermark
        {
            get { return readHigh; }
            set { readHigh = value; SetReadWatermark(); }
        }

        void SetReadWatermark()
        {
            bufferevent_setwatermark(bev, Events.EV_READ, new IntPtr(readLow), new IntPtr(readHigh));
        }

        public BufferEvent(EventBase eventBase, IntPtr socket)
        {
            var options = (int)(BufferEventOptions.CloseOnFree | BufferEventOptions.DeferCallbacks);
            bev = bufferevent_socket_new(eventBase.Handle, socket, options);
            //Console.WriteLine("bufferevent_socket_new returned " + bev.ToInt32());

            var readCb = Marshal.GetFunctionPointerForDelegate(new bufferevent_data_cb(ReadCallbackInternal));
            var writeCb = Marshal.GetFunctionPointerForDelegate(new bufferevent_data_cb(WriteCallbackInternal));
            var eventCb = Marshal.GetFunctionPointerForDelegate(new bufferevent_event_cb(EventCallbackInternal));

            bufferevent_setcb(bev, readCb, writeCb, eventCb, IntPtr.Zero);
        }

        public void Dispose()
        {
            bufferevent_free(bev);
        }

        public void Enable()
        {
            bufferevent_enable(bev, (short)(Events.EV_READ | Events.EV_WRITE));
        }

        void ReadCallbackInternal(IntPtr bev, IntPtr ctx)
        {
            if (Read != null)
                Read(this, EventArgs.Empty);
        }

        void WriteCallbackInternal(IntPtr bev, IntPtr ctx)
        {
            if (Write != null)
                Write(this, EventArgs.Empty);
        }

        void EventCallbackInternal(IntPtr bev, short what, IntPtr ctx)
        {
            if (Event != null)
                Event(this, new BufferEventEventArgs((BufferEventEvents)what));
        }

        #region interop

        private delegate void bufferevent_data_cb(IntPtr bev, IntPtr ctx);
        private delegate void bufferevent_event_cb(IntPtr bev, short what, IntPtr ctx);

        enum BufferEventOptions
        {
            CloseOnFree = 1 << 0,
            ThreadSafe = 1 << 1,
            DeferCallbacks = 1 << 2
        }

        [DllImport("event_core")]
        static extern IntPtr bufferevent_socket_new(IntPtr event_base, IntPtr socket, int options);

        [DllImport("event_core")]
        static extern void bufferevent_free(IntPtr bev);

        [DllImport("event_core")]
        static extern void bufferevent_setcb(IntPtr bev, IntPtr readcb, IntPtr writecb, IntPtr eventcb, IntPtr ctx);

        [DllImport("event_core")]
        static extern void bufferevent_enable(IntPtr bev, short events);

        [DllImport("event_core")]
        static extern IntPtr bufferevent_get_input(IntPtr bev);

        [DllImport("event_core")]
        static extern IntPtr bufferevent_get_output(IntPtr bev);

        [DllImport("event_core")]
        static extern int bufferevent_flush(IntPtr bev, short iotype, int mode);

        [DllImport("event_core")]
        static extern void bufferevent_setwatermark(IntPtr bev, Events events, IntPtr lowmark, IntPtr highmark);

        #endregion
    }

    [Flags]
    public enum BufferEventEvents
    {
        ReadError = 0x01,
        WriteError = 0x02,
        EOF = 0x10,
        Error = 0x20,
        Timeout = 0x40,
        Connected = 0x80
    }
}