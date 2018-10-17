using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace TusClient
{
    public class TusHTTPRequest
    {
        public delegate void UploadingEvent(long bytesTransferred, long bytesTotal);
        public event UploadingEvent Uploading;

        public delegate void DownloadingEvent(long bytesTransferred, long bytesTotal);
        public event DownloadingEvent Downloading;

        public Uri URL { get; set; }
        public string Method { get; set; }
        public Dictionary<string,string> Headers { get; set; }
        public byte[] BodyBytes { get; set; }

        public CancellationToken cancelToken;

        public string BodyText
        {
            get { return System.Text.Encoding.UTF8.GetString(this.BodyBytes); }
            set { BodyBytes = System.Text.Encoding.UTF8.GetBytes(value); }
        }
        

        public TusHTTPRequest(string u)
        {
            this.URL = new Uri(u);
            this.Method = "GET";
            this.Headers = new Dictionary<string, string>();
            this.BodyBytes = new byte[0];
        }

        public void AddHeader(string k,string v)
        {
            this.Headers[k] = v;
        }

        public void FireUploading(long bytesTransferred, long bytesTotal)
        {
            if (Uploading != null)
                Uploading(bytesTransferred, bytesTotal);
        }

        public void FireDownloading(long bytesTransferred, long bytesTotal)
        {
            if (Downloading != null)
                Downloading(bytesTransferred, bytesTotal);
        }

    }
    public class TusHTTPResponse
    {
        public byte[] ResponseBytes { get; set; }
        public string ResponseString { get { return System.Text.Encoding.UTF8.GetString(this.ResponseBytes); } }
        public HttpStatusCode StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        public TusHTTPResponse()
        {
            this.Headers = new Dictionary<string, string>();
        }

    }

    public class TusHTTPClient
    {

        public IWebProxy Proxy { get; set; }
        

        public TusHTTPResponse PerformRequest(TusHTTPRequest req)
        {

            try
            {
                var instream = new MemoryStream(req.BodyBytes);

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(req.URL);
                request.AutomaticDecompression = DecompressionMethods.GZip;
                
                request.Timeout = System.Threading.Timeout.Infinite;
                request.ReadWriteTimeout = System.Threading.Timeout.Infinite;
                request.Method = req.Method;
                request.KeepAlive = false;

                request.Proxy = this.Proxy;

                try
                {
                    ServicePoint currentServicePoint = request.ServicePoint;
                    currentServicePoint.Expect100Continue = false;
                }
                catch (PlatformNotSupportedException)
                {
                    //expected on .net core 2.0 with systemproxy
                    //fixed by https://github.com/dotnet/corefx/commit/a9e01da6f1b3a0dfbc36d17823a2264e2ec47050
                    //should work in .net core 2.2
                }


                //SEND
                req.FireUploading(0, 0);
                byte[] buffer = new byte[4096];

                long contentlength = 0;

                int byteswritten = 0;
                long totalbyteswritten = 0;

                contentlength = (long)instream.Length;
                request.AllowWriteStreamBuffering = false;
                request.ContentLength = instream.Length;

                foreach (var header in req.Headers)
                {
                    switch (header.Key)
                    {
                        case "Content-Length":
                            request.ContentLength = long.Parse(header.Value);
                            break;
                        case "Content-Type":
                            request.ContentType = header.Value;
                            break;
                        default:
                            request.Headers.Add(header.Key, header.Value);
                            break;
                    }
                }

                if (req.BodyBytes.Length > 0)
                {
                    using (System.IO.Stream requestStream = request.GetRequestStream())
                    {
                        instream.Seek(0, SeekOrigin.Begin);
                        byteswritten = instream.Read(buffer, 0, buffer.Length);

                        while (byteswritten > 0)
                        {
                            totalbyteswritten += byteswritten;

                            req.FireUploading(totalbyteswritten, contentlength);

                            requestStream.Write(buffer, 0, byteswritten);

                            byteswritten = instream.Read(buffer, 0, buffer.Length);

                            req.cancelToken.ThrowIfCancellationRequested();
                        }


                    }
                }

                req.FireDownloading(0, 0);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();


                contentlength = 0;
                contentlength = (long)response.ContentLength;
                //contentlength=0 for gzipped responses due to .net bug

                buffer = new byte[16 * 1024];
                var outstream = new MemoryStream();

                using (Stream responseStream = response.GetResponseStream())
                {
                    int bytesread = 0;
                    long totalbytesread = 0;

                    bytesread = responseStream.Read(buffer, 0, buffer.Length);

                    while (bytesread > 0)
                    {
                        totalbytesread += bytesread;

                        req.FireDownloading(totalbytesread, contentlength);

                        outstream.Write(buffer, 0, bytesread);

                        bytesread = responseStream.Read(buffer, 0, buffer.Length);

                        req.cancelToken.ThrowIfCancellationRequested();
                    }
                }

                TusHTTPResponse resp = new TusHTTPResponse();
                resp.ResponseBytes = outstream.ToArray();
                resp.StatusCode = response.StatusCode;
                foreach (string headerName in response.Headers.Keys)
                {
                    resp.Headers[headerName] = response.Headers[headerName];
                }

                return resp;

            }
            catch (OperationCanceledException cancelEx)
            {
                TusException rex = new TusException(cancelEx);
                throw rex;
            }
            catch (WebException ex)
            {
                TusException rex = new TusException(ex);
                throw rex;
            }
        }
    }


    public class TusException : WebException
    {

        public string ResponseContent { get; set; }
        public HttpStatusCode statuscode { get; set; }
        public string statusdescription { get; set; }


        public WebException OriginalException;
        public TusException(TusException ex, string msg)
            : base(string.Format("{0}{1}", msg, ex.Message), ex, ex.Status, ex.Response)
        {
            this.OriginalException = ex;


            this.statuscode = ex.statuscode;
            this.statusdescription = ex.statusdescription;
            this.ResponseContent = ex.ResponseContent;


        }

        public TusException(OperationCanceledException ex)
            : base(ex.Message, ex, WebExceptionStatus.RequestCanceled, null)
        {
            this.OriginalException = null;           
        }

        public TusException(WebException ex, string msg = "")
            : base(string.Format("{0}{1}", msg, ex.Message), ex, ex.Status, ex.Response)
        {

            this.OriginalException = ex;

            HttpWebResponse webresp = (HttpWebResponse)ex.Response;


            if (webresp != null)
            {
                this.statuscode = webresp.StatusCode;
                this.statusdescription = webresp.StatusDescription;

                StreamReader readerS = new StreamReader(webresp.GetResponseStream());

                var resp = readerS.ReadToEnd();

                readerS.Close();

                this.ResponseContent = resp;
            }
           

        }

        public string FullMessage
        {
            get
            {
                var bits = new List<string>();
                if (this.Response != null)
                {
                    bits.Add(string.Format("URL:{0}", this.Response.ResponseUri));
                }
                bits.Add(this.Message);
                if (this.statuscode != HttpStatusCode.OK)
                {
                    bits.Add(string.Format("{0}:{1}", this.statuscode, this.statusdescription));
                }
                if (!string.IsNullOrEmpty(this.ResponseContent))
                {
                    bits.Add(this.ResponseContent);
                }

                return string.Join(Environment.NewLine, bits.ToArray());
            }
        }

    }
}


