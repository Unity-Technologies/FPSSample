using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace TusClient
{
    public class TusClient
    {
        // ***********************************************************************************************
        // Properties



        // ***********************************************************************************************
        // Events
        public delegate void UploadingEvent(long bytesTransferred, long bytesTotal);
        public event UploadingEvent Uploading;

        public delegate void DownloadingEvent(long bytesTransferred, long bytesTotal);
        public event DownloadingEvent Downloading;

        private string autorization = null;
        public String Autorization
        {
            set { autorization = value; }
        }

        // ***********************************************************************************************
        // Private
        //------------------------------------------------------------------------------------------------

        private CancellationTokenSource cancelSource = new CancellationTokenSource();
        
        // ***********************************************************************************************
        // Public
        //------------------------------------------------------------------------------------------------

        public IWebProxy Proxy { get; set; }

        public TusClient()
        {
            
        }

        public void Cancel()
        {
            this.cancelSource.Cancel();
        }

        public string Create(string URL, System.IO.FileInfo file, Dictionary<string, string> metadata = null)
        {
            if (metadata == null)
            {
                metadata = new Dictionary<string,string>();
            }
            if (!metadata.ContainsKey("filename"))
            {
                metadata["filename"] = file.Name;
            }
            return Create(URL, file.Length, metadata);
        }
        public string Create(string URL, long UploadLength, Dictionary<string, string> metadata = null)
        {
            var requestUri = new Uri(URL);
            var client = new TusHTTPClient();
            client.Proxy = this.Proxy;

            var request = new TusHTTPRequest(URL);

            request.Method = "POST";
            request.AddHeader("Tus-Resumable", "1.0.0");
            request.AddHeader("Upload-Length", UploadLength.ToString());
            request.AddHeader("Content-Length", "0");
            if(autorization != null)
                request.AddHeader("Authorization", autorization);

            if (metadata == null)
            {
                metadata = new Dictionary<string,string>();
            }

            var metadatastrings = new List<string>();
            foreach (var meta in metadata)
            {
                string k = meta.Key.Replace(" ", "").Replace(",","");
                string v = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(meta.Value));
                metadatastrings.Add(string.Format("{0} {1}", k, v ));
            }
            request.AddHeader("Upload-Metadata", string.Join(",",metadatastrings.ToArray()));

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                if (response.Headers.ContainsKey("Location"))
                {
                    Uri locationUri;
                    if (Uri.TryCreate(response.Headers["Location"],UriKind.RelativeOrAbsolute,out locationUri ))
                    {
                        if (!locationUri.IsAbsoluteUri)
                        {
                            locationUri = new Uri(requestUri, locationUri);
                        }
                        return locationUri.ToString();
                    }
                    else
                    {
                        throw new Exception("Invalid Location Header");
                    }

                }
                else
                {
                    throw new Exception("Location Header Missing");
                }
                
            }
            else
            {
                throw new Exception("CreateFileInServer failed. " + response.ResponseString );
            }
        }
        //------------------------------------------------------------------------------------------------
        public void Upload(string URL, System.IO.FileInfo file)
        {
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                Upload(URL, fs);
            }

        }
        public void Upload(string URL, System.IO.Stream fs)
        {

            var Offset = this.getFileOffset(URL);
            var client = new TusHTTPClient();
            System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1Managed();
            int ChunkSize = (int) Math.Ceiling(5 * 1024.0 * 1024.0); //3 mb

            if (Offset == fs.Length)
            {
                if (Uploading != null)
                    Uploading((long)fs.Length, (long)fs.Length);
            }


            while (Offset < fs.Length)
                {
                    fs.Seek(Offset, SeekOrigin.Begin);
                    byte[] buffer = new byte[ChunkSize];
                    var BytesRead = fs.Read(buffer, 0, ChunkSize);

                    Array.Resize(ref buffer, BytesRead);
                    var sha1hash = sha.ComputeHash(buffer);

                    var request = new TusHTTPRequest(URL);
                    request.cancelToken = this.cancelSource.Token;
                    request.Method = "PATCH";
                    if (autorization != null)
                    {
                        request.AddHeader("Authorization", autorization);
                    }
                    request.AddHeader("Tus-Resumable", "1.0.0");
                    request.AddHeader("Upload-Offset", string.Format("{0}", Offset));
                    request.AddHeader("Upload-Checksum", "sha1 " + Convert.ToBase64String(sha1hash));
                    request.AddHeader("Content-Type", "application/offset+octet-stream");
                    
                    request.BodyBytes = buffer;

                    request.Uploading += delegate(long bytesTransferred, long bytesTotal)
                    {
                        if (Uploading != null)
                            Uploading((long)Offset + bytesTransferred, (long)fs.Length);
                    };

                    try
                    {
                        var response = client.PerformRequest(request);

                        if (response.StatusCode == HttpStatusCode.NoContent)
                        {
                            Offset += BytesRead;
                        }
                        else
                        {
                            throw new Exception("WriteFileInServer failed. " + response.ResponseString);
                        }
                    }
                    catch (IOException ex)
                    {
                        if (ex.InnerException.GetType() == typeof(System.Net.Sockets.SocketException))
                        {
                            var socketex = (System.Net.Sockets.SocketException) ex.InnerException;
                            if (socketex.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset)
                            {
                                // retry by continuing the while loop but get new offset from server to prevent Conflict error
                                Offset = this.getFileOffset(URL);
                            }
                            else
                            {
                                throw socketex;
                            }                            
                        }
                        else
                        {
                            throw;
                        }                        
                    }



                }
            
        }
        //------------------------------------------------------------------------------------------------
        public TusHTTPResponse Download(string URL)
        {
            var client = new TusHTTPClient();

            var request = new TusHTTPRequest(URL);
            request.cancelToken = this.cancelSource.Token;
            request.Method = "GET";

            request.Downloading += delegate(long bytesTransferred, long bytesTotal)
            {
                if (Downloading != null)
                    Downloading((long)bytesTransferred, (long)bytesTotal);
            };

            var response = client.PerformRequest(request);

            return response;
        }
        //------------------------------------------------------------------------------------------------
        public TusHTTPResponse Head(string URL)
        {
            var client = new TusHTTPClient();
            var request = new TusHTTPRequest(URL);
            request.Method = "HEAD";
            request.AddHeader("Tus-Resumable", "1.0.0");
            if (autorization != null)
            {
                request.AddHeader("Authorization", autorization);
            }

            try
            {
                var response = client.PerformRequest(request);
                return response;
            }
            catch (TusException ex)
            {
                var response = new TusHTTPResponse();
                response.StatusCode = ex.statuscode;
                return response;
            }
        }
        //------------------------------------------------------------------------------------------------
        public class TusServerInfo
        {
            public string Version = "";
            public string SupportedVersions = "";
            public string Extensions = "";
            public long MaxSize = 0;
            
            public bool SupportsDelete
            {
                get { return this.Extensions.Contains("termination"); }
            }
            
        }

        public TusServerInfo getServerInfo(string URL)
        {
            var client = new TusHTTPClient();
            var request = new TusHTTPRequest(URL);
            request.Method = "OPTIONS";

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
            {
                // Spec says NoContent but tusd gives OK because of browser bugs
                var info = new TusServerInfo();
                response.Headers.TryGetValue("Tus-Resumable", out info.Version);
                response.Headers.TryGetValue("Tus-Version", out info.SupportedVersions);
                response.Headers.TryGetValue("Tus-Extension", out info.Extensions);

                string MaxSize;
                if (response.Headers.TryGetValue("Tus-Max-Size", out MaxSize))
                {
                    info.MaxSize = long.Parse(MaxSize);
                }
                else
                {
                    info.MaxSize = 0;
                }

                return info;
            }
            else
            {
                throw new Exception("getServerInfo failed. " + response.ResponseString);
            }
        }
        //------------------------------------------------------------------------------------------------
        public bool Delete(string URL)
        {
            var client = new TusHTTPClient();
            var request = new TusHTTPRequest(URL);
            request.Method = "DELETE";
            request.AddHeader("Tus-Resumable", "1.0.0");
            if (autorization != null)
            {
                request.AddHeader("Authorization", autorization);
            }

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Gone)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        // ***********************************************************************************************
        // Internal
        //------------------------------------------------------------------------------------------------
        private long getFileOffset(string URL)
        {
            var client = new TusHTTPClient();
            var request = new TusHTTPRequest(URL);
            request.Method = "HEAD";
            request.AddHeader("Tus-Resumable", "1.0.0");
            if (autorization != null)
                request.AddHeader("Authorization", autorization);

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Headers.ContainsKey("Upload-Offset"))
                {
                    return long.Parse(response.Headers["Upload-Offset"]);
                }
                else
                {
                    throw new Exception("Offset Header Missing");
                }
            }
            else
            {
                throw new Exception("getFileOffset failed. " + response.ResponseString);
            }
        }
        // ***********************************************************************************************
    } // End of Class
} // End of Namespace
