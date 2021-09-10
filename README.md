# FtpManager
Este proyecto permite operar con un servidor ftp.

            Configuration _configuration = new Configuration
            {
                Server = "192.168.1.101",
                Port = 21,
                UserReader = new FtpManager.Authentication("user_reader_name", "user_reader_password"),
                UserWriter = new FtpManager.Authentication("user_writer_password", "user_writer_password"),
                Ssl = false
            };

            // Read file from ftp server in the specified filename
            var ftpinfo = new FtpManager.FileInfo(_configuration, "/EXAMPLE DOCUMENTS/document.pdf");
            if (ftpinfo.Exist)
            {
                //Read bytes from ftp server file
                byte[] document = await ftpinfo.ReadAllBytes();
            }
            else
            {
                /// Create file with content data in to referenced file on instance
                await ftpinfo.Create(System.IO.File.ReadAllBytes(@"C:\local_document.pdf"));
            }

            if((await ftpinfo.ReadAllBytes()).Length == 0)
            {
                //delete referenced file on instance
                await ftpinfo.Delete();
            }
            else
            {
                //Create a copy from referenced file on instance
                await ftpinfo.CopyTo("/backup/document_BACKUP.pdf");
            }
