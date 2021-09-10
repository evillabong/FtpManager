using FtpManager;
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
    public class FileInfo
    {
        public string Name { get; private set; }
        public string NameWithoutExtension { get; }
        public string Description { get; private set; }
        public long Length { get; private set; }
        public DateTime LastWriteTime { get; private set; }
        public string FullName { get; private set; }
        public string RelativePath { get; private set; }
        /// <summary>
        /// Obtiene el directorio relativo donde se encuentra ubicado el archivo sin contar con el servidor ftp y puerto.
        /// </summary>
        public string DirectoryPath { get; private set; }

        /// <summary>
        /// Obtiene la ruta hasta el directorio del archivo actual incluyendo el nombre del servidor <b>ftp </b> y <b>puerto</b>.
        /// </summary>
        public string FullDirectoryPath { get; private set; }
        public bool Exist { get; private set; } = false;
        public bool IsDirectory { get; private set; } = true;
        public bool IsPdf { get; private set; } = false;
        public bool IsXml { get; private set; } = false;

        public string Extension { get; private set; }
        public bool Fail { get; private set; }
        public string FailMessage { get; private set; }
        private Configuration _configuration { get; set; }
        public int Buffer { get; private set; } = 10240;



        public FileInfo(Configuration ftpConfiguration)
        {
            var port = ftpConfiguration.Port == 0 ? "" : $":{ ftpConfiguration.Port}";
            this.Buffer = ftpConfiguration.BufferReaderBytes > 0 ? ftpConfiguration.BufferReaderBytes : 10240;

            var uri = $@"ftp://{ftpConfiguration.Server}{port}";
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Credentials = new NetworkCredential(ftpConfiguration.UserReader.Username, ftpConfiguration.UserReader.Password);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.UsePassive = true;
            try
            {
                FtpWebResponse response = (FtpWebResponse)(request.GetResponse());
                if (response.StatusCode == FtpStatusCode.CommandOK || response.StatusCode == FtpStatusCode.FileActionOK || response.StatusCode == FtpStatusCode.FileStatus)
                {
                    this.Fail = false;
                    this.FailMessage = "";
                    return;
                }
                else
                {
                    this.FailMessage = response.StatusDescription;
                }
            }
            catch (System.Exception ex)
            {
                this.FailMessage = ex.Message;
            }
            this.Fail = true;
        }

        /// <summary>
        /// Crea una intancia de un documento dentro del servidor <b>ftp</b> especificado en la <b>configuración</b>.
        /// </summary>
        /// <param name="ftpConfiguration">Parámetro de configuración para inicializar el servidor ftp.</param>
        /// <param name="filename">Ruta relativa del documento en el servidor ftp. "/DOCUMENTOS/PERSONAL/ETC/documento.pdf"</param>

        public FileInfo(Configuration ftpConfiguration, string filename)
        {
            var port = ftpConfiguration.Port == 0 ? "" : $":{ ftpConfiguration.Port}";
            this.Buffer = ftpConfiguration.BufferReaderBytes > 0 ? ftpConfiguration.BufferReaderBytes : 10240;
            filename = filename.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!filename.StartsWith($"{Path.AltDirectorySeparatorChar}"))
            {
                filename = $"{Path.AltDirectorySeparatorChar}{filename}";
            }

            var uri = $@"ftp://{ftpConfiguration.Server}{port}{filename.GetHtmlText()}";
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Credentials = new NetworkCredential(ftpConfiguration.UserReader.Username, ftpConfiguration.UserReader.Password);
            request.Method = WebRequestMethods.Ftp.GetFileSize;
            request.UsePassive = true;

            this.Name = filename.Split('/')[filename.Split('/').Count() - 1];
            this.NameWithoutExtension = Name.Split('.')[0];
            this.FullName = uri;
            this.RelativePath = $@"{filename.GetHtmlText()}";
            this.Extension = filename.Split('.')[filename.Split('.').Count() - 1];
            this.IsPdf = this.Extension == "pdf";
            this.IsXml = this.Extension == "xml";
            this._configuration = ftpConfiguration;
            this.DirectoryPath = filename.Substring(0, filename.Length - (this.Name.Length + 1));
            this.FullDirectoryPath = $"ftp://{ftpConfiguration.Server}{port}{this.DirectoryPath}";
            this.IsDirectory = false;
            try
            {
                FtpWebResponse response = (FtpWebResponse)(request.GetResponse());

                if ((response.StatusCode == FtpStatusCode.CommandOK || response.StatusCode == FtpStatusCode.FileActionOK || response.StatusCode == FtpStatusCode.FileStatus))
                {
                    var request2 = (FtpWebRequest)WebRequest.Create(uri);
                    request2.Credentials = request.Credentials;
                    request2.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                    request2.UsePassive = true;
                    FtpWebResponse response2 = (FtpWebResponse)(request2.GetResponse());

                    if (response2.StatusCode == FtpStatusCode.CommandOK || response2.StatusCode == FtpStatusCode.FileActionOK || response2.StatusCode == FtpStatusCode.FileStatus)
                    {

                        this.Length = response.ContentLength;
                        this.Description = response.BannerMessage;
                        this.LastWriteTime = response2.LastModified;
                        this.Exist = true;
                        this.Fail = false;
                        this.FailMessage = "";
                        response.Close();
                    }
                    else
                    {
                        this.Fail = true;
                        this.FailMessage = response2.StatusDescription;
                    }
                }
            }
            catch (System.Exception ex)
            {

                Debug.Print(ex.Message);
                this.Fail = true;
            }
        }

        /// <summary>
        /// Obtiene un codigo <b>hash</b> del contenido del documento especificado por la clase <b>FileInfo</b>.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetHash()
        {
            return (await ReadAllText()).GetHashCode().ToString();
        }

        /// <summary>
        /// Permite eliminar el documento especificado por la instancia <b>FileInfo</b>.
        /// </summary>
        /// <returns></returns>
        public async Task Delete()
        {
            if (Exist)
            {
                try
                {
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(this.FullName);
                    request.Method = WebRequestMethods.Ftp.DeleteFile;
                    request.Proxy = null;
                    request.UseBinary = false;
                    request.UsePassive = true;
                    request.KeepAlive = false;
                    request.Credentials = new NetworkCredential(this._configuration.UserWriter.Username, this._configuration.UserWriter.Password);

                    FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync();
                    this.Description = response.StatusDescription;
                    response.Close();
                    this.Exist = false;
                    Fail = false;
                    FailMessage = "";
                }
                catch (System.Exception ex)
                {
                    Fail = true;
                    FailMessage = ex.Message;
                }
            }
        }

        /// <summary>
        /// Obtiene el documento FTP en bytes desde la instancia especificada por la clase <b>FileInfo</b>.
        /// </summary>
        /// <returns></returns>
        public async Task<byte[]> ReadAllBytes()
        {
            if (Exist)
            {
                try
                {
                    var document = default(byte[]);
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(this.FullName);
                    request.Method = WebRequestMethods.Ftp.DownloadFile;
                    request.UsePassive = true;
                    request.Credentials = new NetworkCredential(this._configuration.UserReader.Username, this._configuration.UserReader.Password);

                    FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync();
                    Console.WriteLine($"Download Complete, status {response.StatusDescription}");
                    //if (response.StatusCode == FtpStatusCode.CommandOK || response.StatusCode == FtpStatusCode.FileActionOK || response.StatusCode == FtpStatusCode.DataAlreadyOpen)
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(responseStream);
                            var bytes = default(byte[]);
                            using (var memstream = new MemoryStream())
                            {
                                var buffer = new byte[this.Buffer];
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
                    this.Length = document.Length;
                    Fail = false;
                    FailMessage = "";
                    return document;
                }
                catch (System.Exception ex)
                {
                    Fail = true;
                    FailMessage = ex.Message;
                }
            }
            return null;
        }

        /// <summary>
        /// Obtiene el contenido FTP en texto plano desde la instancia especificada por la clase <b>FileInfo</b>.
        /// </summary>
        /// <returns></returns>
        public async Task<string> ReadAllText()
        {
            var text = Encoding.ASCII.GetString(await ReadAllBytes());
            return text;
        }

        /// <summary>
        /// Permite crear un documento a partir de la instancia de la clase <b>FileInfo</b> a un servidor <p stlye="color:red"> Ftp. </p>.
        /// </summary>
        /// <param name="data">Contenido en bytes del documento a crear.</param>
        /// <param name="replace">Especifica si reemplaza un documento existente en el mismo directorio.</param>
        /// <returns></returns>
        public async Task Create(byte[] data, bool replace = false)
        {
            if (data != null)
            {
                if (!await DirectoryExist())
                {
                    await CreateDirectory();
                }
                if (this.Exist && !replace)
                {
                    if (this.Length == data.Length)
                    {
                        throw new FtpFileException("Un archivo con el mismo tamaño ya existe en el directorio de destino.\nNo se copiará al servidor ftp.");
                    }
                }
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(this.FullName);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.UseBinary = true;
                request.UsePassive = true;
                request.Timeout = 60000 * 2;
                request.Credentials = new NetworkCredential(_configuration.UserWriter.Username, _configuration.UserWriter.Password);
                request.ContentLength = data.Length;

                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    requestStream.Write(data, 0, data.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine($"Upload File Complete, status {response.StatusDescription}");
                }
            }
        }
        /// <summary>
        /// Permite crear un documento a partir de la instancia de la clase <b>FileInfo</b> a un servidor <p stlye="color:red"> Ftp. </p>.
        /// </summary>
        /// <param name="data">Contenido en stream del documento a crear.</param>
        /// <param name="replace">Especifica si reemplaza un documento existente en el mismo directorio.</param>
        /// <returns></returns>
        public async Task Create(Stream data, bool replace = false)
        {
            var document = data.ToBytes(Buffer);
            await Create(document, replace);
        }
        /// <summary>
        /// Permite crear un documento a partir de la instancia de la clase <b>FileInfo</b> a un servidor <p stlye="color:red"> Ftp. </p>.
        /// </summary>
        /// <param name="content">Contenido en teto plano del documento a crear.</param>
        /// <param name="replace">Especifica si reemplaza un documento existente en el mismo directorio.</param>
        /// <returns></returns>
        public async Task Create(string content, bool replace = false)
        {
            await Create(Encoding.ASCII.GetBytes(content), replace);
        }

        /// <summary>
        /// Permite crear el directorio de la ruta relativa especificada al instanciar el documento ftp.
        /// </summary>
        /// <returns></returns>
        public async Task CreateDirectory()
        {
            var folders = this.DirectoryPath.Split(Path.AltDirectorySeparatorChar).Where(p => !string.IsNullOrEmpty(p));
            var dir = $@"ftp://{this._configuration.Server}";
            foreach (var folder in folders)
            {
                dir = $"{dir}{Path.AltDirectorySeparatorChar}{folder}".Replace(" ", "%20");
                if (!await DirectoryExist(dir))
                {
                    WebRequest request = WebRequest.Create(dir);
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    request.Credentials = new NetworkCredential(this._configuration.UserWriter.Username, this._configuration.UserWriter.Password);
                    using (var resp = (FtpWebResponse)await request.GetResponseAsync())
                    {
                        Console.WriteLine($"MKD OK: {resp.StatusCode} {dir}");
                    }
                }
            }
        }
        /// <summary>
        /// Especifica si el directorio de la ruta relativa existe en el servidor ftp.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DirectoryExist()
        {
            return await DirectoryExist(this.FullDirectoryPath);
        }
        /// <summary>
        /// Especifica si el directorio por parámetro existe en el servidor ftp.
        /// </summary>
        /// <param name="directory">Ruta relativa del directorio (parámetro).</param>
        /// <returns></returns>
        public async Task<bool> DirectoryExist(string directory)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(directory);
                request.Credentials = new NetworkCredential(this._configuration.UserWriter.Username, this._configuration.UserWriter.Password);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync();
                return true;
            }
            catch (WebException ex)
            {
                Debug.Print(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Esta funcionalidad permite copiar un documento instanciado por la clase <b>FileInfo</b> via Ftp.
        /// </summary>
        /// <param name="relativePath">Se especifica la ruta relativa del documento de destino "/DOCUMENTOS/PERSONAL/ETC/documento.pdf"</param>
        /// <param name="replace">Se especifica si el documento que se instancia se reemplazará.</param>
        /// <returns></returns>
        public async Task CopyTo(string relativePath, bool replace = false)
        {
            if (Exist)
            {
                var outputFile = new FileInfo(_configuration, relativePath);
                await outputFile.Create(await ReadAllBytes(), replace);
            }
            else
            {
                throw new System.Exception($"Documento de origen no existe. \n {this.RelativePath}");
            }

        }

    }
}
