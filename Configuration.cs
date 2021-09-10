using System;
using System.Collections.Generic;
using System.Text;

namespace FtpManager
{
    public class Configuration
    {
        public string Server { get; set; }

        /// <summary>
        /// Puerto por defecto <b>21</b>
        /// </summary>
        public int Port { get; set; } = 21;
        /// <summary>
        /// Buffer por defecto <b>2048 bytes</b>
        /// </summary>
        public int BufferReaderBytes { get; set; } = 2048;

        /// <summary>
        /// Ssl por defecto <b>false</b> = DESHABILITADO
        /// </summary>
        public bool Ssl { get; set; } = false;

        public Authentication UserReader { get; set; }
        public Authentication UserWriter { get; set; }

        public Configuration()
        {

        }

        public Configuration (string server , int port = 21, bool ssl = false , int buffer = 2048)
        {
            this.Server = server;
            this.Port = port;
            this.Ssl = ssl;
            this.BufferReaderBytes = buffer;
        }
    }
}
