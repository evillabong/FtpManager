using FtpManager.Exception;
using FtpManager.Extension;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FtpManager
{
    public class Methods
    {
        public static async Task<byte[]> GetFile(Configuration ftpConfiguration, string filename)
        {
            var document = default(byte[]);
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(filename.GetHtmlText());
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(ftpConfiguration.UserReader.Username, ftpConfiguration.UserReader.Password);

            FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync();
            Console.WriteLine($"Download Complete, status {response.StatusDescription}");
            if (response.StatusCode == FtpStatusCode.CommandOK || response.StatusCode == FtpStatusCode.FileActionOK || response.StatusCode == FtpStatusCode.DataAlreadyOpen)
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream);
                    var bytes = default(byte[]);
                    using (var memstream = new MemoryStream())
                    {
                        var buffer = new byte[ftpConfiguration.BufferReaderBytes];
                        var bytesRead = default(int);
                        while ((bytesRead = reader.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                            memstream.Write(buffer, 0, bytesRead);
                        bytes = memstream.ToArray();
                        document = bytes;
                    }
                    reader.Close();
                    response.Close();
                }
                Console.WriteLine($"Download Complete, status {response.StatusDescription}");
            }

            return document;
        }

        public static async Task<byte[]> GetFileRelative(Configuration ftpConfiguration, string filename)
        {
            return await GetFile(ftpConfiguration, $@"ftp://{ftpConfiguration.Server}:{ftpConfiguration.Port}{filename.GetHtmlText()}");
        }

        public static async Task<bool> FileRelativeExist(Configuration ftpConfiguration, string filename)
        {
            return await FileExist(ftpConfiguration, $@"ftp://{ftpConfiguration.Server}:{ftpConfiguration.Port}{filename.GetHtmlText()}");
        }

        public static async Task<bool> FileExist(Configuration ftpConfiguration, string filename)
        {
            var request = (FtpWebRequest)WebRequest.Create(filename.GetHtmlText());
            request.Credentials = new NetworkCredential(ftpConfiguration.UserReader.Username, ftpConfiguration.UserReader.Password);
            request.Method = WebRequestMethods.Ftp.GetFileSize;

            try
            {
                FtpWebResponse response = (FtpWebResponse)(await request.GetResponseAsync());

                return true;
            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    return false;
            }
            return false;
        }

        public static bool IsDirectory(string directory)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(); // or however you want to handle null values
            }

            // GetExtension(string) returns string.Empty when no extension found
            return System.IO.Path.GetExtension(directory) == string.Empty;
        }

        public static async Task<bool> Upload(Configuration ftpConfiguration, byte[] data, string dirDestination, bool replace = false, bool makeDirectory = true)
        {
            if (data != null)
            {
                dirDestination = dirDestination.Replace(@"\\", "/").Replace(@"\", "/").Replace(" ", "%20");
                var ftpFile = new FileInfo(ftpConfiguration, dirDestination);

                if (makeDirectory)
                {
                    if (!DirectoryExist(ftpFile.FullDirectoryPath))
                    {
                        MakeDirectory(ftpConfiguration, dirDestination);
                    }
                }

                if (ftpFile.Exist && !replace)
                {

                    if (ftpFile.Length == data.Length)
                    {
                        throw new FtpFileException("Un archivo con el mismo tamaño ya existe en el directorio de destino.\nNo se copiará al servidor ftp.");
                    }
                }
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpFile.FullName);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.UseBinary = true;
                request.UsePassive = true;
                request.Timeout = 60000 * 2;
                request.Credentials = new NetworkCredential(ftpConfiguration.UserWriter.Username, ftpConfiguration.UserWriter.Password);

                //byte[] fileContents;
                //using (StreamReader sourceStream = new StreamReader(filename))
                //{
                //    fileContents = Encoding.UTF8.GetBytes(await sourceStream.ReadToEndAsync());
                //}

                request.ContentLength = data.Length; //fileContents.Length;

                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    requestStream.Write(data, 0, data.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine($"Upload File Complete, status {response.StatusDescription}");
                }

                return true;
            }
            return false;
        }
        public static async Task<bool> Upload(Configuration ftpConfiguration, string filename, string dirDestination, bool replace = false, bool makeDirectory = true)
        {
            var file = new System.IO.FileInfo(filename);
            if (file.Exists)
            {

                dirDestination = dirDestination.Replace(@"\\", "/").Replace(@"\", "/").Replace(" ", "%20");
                var ftpFile = new FileInfo(ftpConfiguration, dirDestination);

                if (DirectoryExist(ftpFile.RelativePath))
                {
                    MakeDirectory(ftpConfiguration, dirDestination);
                }

                if (ftpFile.Exist && !replace)
                {

                    if (ftpFile.Length == file.Length)
                    {
                        throw new FtpFileException("Un archivo con el mismo tamaño ya existe en el directorio de destino.\nNo se copiará al servidor ftp.");
                    }
                }
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(file.FullName);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.UseBinary = true;
                request.UsePassive = true;
                request.Timeout = 60000 * 2;
                request.Credentials = new NetworkCredential(ftpConfiguration.UserWriter.Username, ftpConfiguration.UserWriter.Password);

                byte[] fileContents;
                using (StreamReader sourceStream = new StreamReader(filename))
                {
                    fileContents = Encoding.UTF8.GetBytes(await sourceStream.ReadToEndAsync());
                }

                request.ContentLength = fileContents.Length;

                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine($"Upload File Complete, status {response.StatusDescription}");
                }

                return true;
            }
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ftpConfiguration"></param>
        /// <param name="data"></param>
        /// <param name="dirDestination"></param>
        /// <param name="replace"></param>
        /// <param name="makeDirectory"></param>
        /// <param name="buffer">Set [buffer] = 10240 is null</param>
        /// <returns></returns>
        public static async Task<bool> Upload(Configuration ftpConfiguration, byte[] data, string dirDestination, int? buffer, bool replace = false, bool makeDirectory = true)
        {
            dirDestination = dirDestination.Replace(@"\\", "/").Replace(@"\", "/").Replace(" ", "%20");

            var file = new FileInfo(ftpConfiguration, dirDestination);

            if (DirectoryExist(file.FullName))
            {
                MakeDirectory(ftpConfiguration, dirDestination);
            }

            if (file.Exist)
            {
                if (file.Length == data.Length)
                {
                    throw new FtpFileException("Un archivo con el mismo tamaño ya existe en el directorio de destino.\nNo se copiará al servidor ftp.");
                }
            }

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(file.FullName);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.UseBinary = true;
            request.UsePassive = true;
            request.Timeout = 60000 * 2;
            request.Credentials = new NetworkCredential(ftpConfiguration.UserWriter.Username, ftpConfiguration.UserWriter.Password);

            try
            {
                using (Stream fileStream = data.ToStream())
                {
                    using (Stream ftpStream = await request.GetRequestStreamAsync())
                    {
                        byte[] buffer1 = new byte[buffer ?? 10240];
                        int read;
                        while ((read = fileStream.Read(buffer1, 0, buffer1.Length)) > 0)
                        {
                            await ftpStream.WriteAsync(buffer1, 0, read);
                            Console.WriteLine("Uploaded {0} bytes", fileStream.Position);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.Print(ex.Message);
                throw new FtpFileException("Un archivo con el mismo tamaño ya existe en el directorio de destino.\nNo se copiará al servidor ftp.");
            }
            using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
            {
                Console.WriteLine($"Upload File Complete, status {response.StatusDescription}");
            }

            return true;
        }

        public static List<System.IO.FileInfo> GetFilesRecursive(string directory)
        {

            return Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).ToList().Select(p => new System.IO.FileInfo(p)).ToList();
        }
        public static bool MakeDirectory(Configuration ftpConfiguration, string directory)
        {
            try
            {
                directory = directory.Replace(@"\\", "/").Replace(@"\", "/");
                var dir = $@"ftp://{ftpConfiguration.Server}/";
                var folders = directory.Split('/').Where(p => !string.IsNullOrEmpty(p));
                foreach (var folder in folders)
                {

                    dir = $"{dir}{folder}{Path.AltDirectorySeparatorChar}".Replace(" ", "%20");
                    WebRequest request = WebRequest.Create(dir);
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    request.Credentials = new NetworkCredential(ftpConfiguration.UserWriter.Username, ftpConfiguration.UserWriter.Password);
                    using (var resp = (FtpWebResponse)request.GetResponse())
                    {
                        Console.WriteLine($"MKD OK: {resp.StatusCode} {dir}");
                    }

                }
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.Print(ex.Message);
                throw new FtpFileException($"MKD GENERAL: {ex.Message}");
            }
        }
        public static bool MakeDirectory(Configuration ftpConfiguration, string[] directories)
        {
            var dir = directories.ToList().Where(p => !string.IsNullOrEmpty(p)).Distinct().Select(p => p).ToList();
            int count = 0;
            foreach (var directory in dir)
            {
                try
                {
                    var make = MakeDirectory(ftpConfiguration, directory);
                }
                catch
                {
                    count++;
                }
            }
            if (count > 0)
            {
                throw new FtpFileException($"No todos los directorios fueron creados ({count}).");
            }
            return true;
        }
        public static bool DirectoryExist(string dirPath)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(dirPath);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                return true;
            }
            catch (WebException ex)
            {
                Debug.Print(ex.Message);
                return false;
            }
        }
        public static string GetDirectoryDestination(string dirBase, string filename)
        {

            var dir = filename.Length > dirBase.Length ? filename.Substring((dirBase.Length + 1),
                filename.Length - ((dirBase.Length + 1))) : "";
            return dir;
        }
    }
}
