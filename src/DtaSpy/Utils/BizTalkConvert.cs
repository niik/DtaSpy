/*
 * Copyright (c) 2012 Markus Olsson
 * var mail = string.Join(".", new string[] {"j", "markus", "olsson"}) + string.Concat('@', "gmail.com");
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this 
 * software and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish, 
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace DtaSpy
{
    /// <summary>
    /// Utility method for converting from BizTalk block streams.
    /// </summary>
    public static class BizTalkConvert
    {
        /// <summary>
        /// Deserializes the message part data and returns the decompressed content as a byte array
        /// </summary>
        public static byte[] DeserializeMessage(byte[] buffer)
        {
            using (var source = new MemoryStream(buffer))
            using (var destination = new MemoryStream())
            {
                DeserializeMessageTo(source, destination);

                return destination.ToArray();
            }
        }

        /// <summary>
        /// Deserializes the message part data within the given stream and returns the 
        /// decompressed content as a byte array
        /// </summary>
        public static byte[] DeserializeMessage(Stream source)
        {
            using (var ms = new MemoryStream())
            {
                DeserializeMessageTo(source, ms);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Deserializes the message part data within the given source stream and writes the
        /// content into the given destination stream.
        /// </summary>
        public static int DeserializeMessageTo(Stream source, Stream destination)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (destination == null)
                throw new ArgumentNullException("destination");

            using (var decoder = new BizTalkMessagePartStream(source, StreamMode.Read))
            {
                byte[] buffer = new byte[8192];

                int total = 0;
                int read;

                do
                {
                    read = decoder.Read(buffer, 0, buffer.Length);
                    destination.Write(buffer, 0, read);
                    total += read;
                }
                while (read > 0);

                return total;
            }
        }

        public static BizTalkPropertyBag DeserializeContext(byte[] buffer)
        {
            using (var ms = new MemoryStream(buffer))
                return DeserializeContext(ms);
        }

        public static BizTalkPropertyBag DeserializeContext(Stream input)
        {
            using (var contextReader = new BizTalkContextReader(input))
                return contextReader.ReadContext();
        }
    }
}
