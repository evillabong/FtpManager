using System;
using System.Collections.Generic;
using System.Text;

namespace FtpManager
{
    public class Authentication
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public Authentication()
        {

        }
        public Authentication(string user , string password)
        {
            this.Username = user;
            this.Password = password;
        }
    }
}
