using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FtpManager.Extension
{
    public static class StreamExtensions
    {
        public static string ToString(this Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
        public static Stream ToStream(this String data)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(data);
            return new MemoryStream(byteArray);
        }
        public static Stream ToStream(this byte[] data)
        {
            return new MemoryStream(data);
        }
        public static byte[] ToBytes(this Stream data,int _buffer = 1024)
        {
            byte[] buffer = new byte[16 * _buffer];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = data.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
