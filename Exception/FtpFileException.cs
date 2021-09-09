using System;
using System.Collections.Generic;
using System.Text;

namespace FtpManager.Exception
{
    public class FtpFileException : System.Exception
    {
        public FtpFileException() : base("Se ha generado un inconveniente al procesar el archivo FTP.")
        {

        }
        public FtpFileException (string message) : base (message)
        {
            
        }
    }
}
