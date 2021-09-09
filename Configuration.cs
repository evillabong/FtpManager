using System;
using System.Collections.Generic;
using System.Text;

namespace FtpManager
{
    public class Configuration
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public int BufferReaderBytes { get; set; }
        public bool Ssl { get; set; }

        public Authentication UserReader { get; set; }
        public Authentication UserWriter { get; set; }

        public Configuration()
        {

        }
        public Configuration (string server , int port , bool ssl = false , int buffer = 2048)
        {
            this.Server = server;
            this.Port = port;
            this.Ssl = ssl;
            this.BufferReaderBytes = buffer;
        }
    }
}
