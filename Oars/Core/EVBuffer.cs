﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace Oars.Core
{
    public class EVBuffer : IDisposable
    {
        IntPtr handle;
        bool ownsBuffer;

        public int Length { get { return evbuffer_get_length(handle); } }

        public EVBuffer()
        {
            handle = evbuffer_new();
            ownsBuffer = true;
        }

        public EVBuffer(IntPtr handle)
        {
            this.handle = handle;
        }

        public void Dispose()
        {
            if (ownsBuffer)
                evbuffer_free(handle);
        }

        public bool Add(byte[] data, int offset, int count)
        {
            unsafe {
                fixed (byte *ptr = &data[offset])
                    return evbuffer_add(handle, ptr, count) > 0;
            }
        }

        public int Remove(byte[] data, int offset, int count)
        {
            var c = new IntPtr(count);

            unsafe {
                fixed (byte *ptr = &data[0])
                    return evbuffer_remove(handle, ptr, count);
            }
        }

        public int Remove(EVBuffer buffer, int len)
        {
            return evbuffer_remove_buffer(handle, buffer.handle, len);
        }

        #region interop

        [DllImport("event_core")]
        private static extern IntPtr evbuffer_new();

        [DllImport("event_core")]
        private static extern void evbuffer_free(IntPtr buf);

        [DllImport("event_core")]
        private static unsafe extern int evbuffer_add(IntPtr buf, byte* data, int len);

        [DllImport("event_core")]
        private static unsafe extern int evbuffer_remove(IntPtr buf, byte* data, int len);

        [DllImport("event_core")]
        private static extern int evbuffer_remove_buffer(IntPtr src, IntPtr dest, int len);

        [DllImport("event_core")]
        private static extern int evbuffer_add_buffer(IntPtr outbuf, IntPtr inbuf);

        [DllImport("event_core")]
        private static extern int evbuffer_add_file(IntPtr output, IntPtr fd, int offset, int length);

        [DllImport("event_core")]
        private static extern int evbuffer_get_length(IntPtr buf);

        #endregion
    }
}