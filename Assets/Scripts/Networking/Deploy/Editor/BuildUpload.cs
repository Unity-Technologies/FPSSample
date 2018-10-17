using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using SimpleJSON;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net;

using Debug = UnityEngine.Debug;

namespace ConnectedGames.Build
{

    public class BuildUpload
    {
        const string k_CloudArtifactsUrl = "https://build-artifact-api.cloud.unity3d.com/api/v1/projects/";
        const string k_CloudBuildUrl = "https://build-api.cloud.unity3d.com/api/v1/orgs/";
        
        double m_Perc;
        double m_PercentTotal;
        Task m_UploadingTask;
        UnityWebRequest m_Request;
        Stopwatch m_Timer = new Stopwatch();
        
        [SerializeField] int m_BuildId;
        [SerializeField] string m_Label;
        [SerializeField] string m_Platform = "";
        [SerializeField] FileInfo m_UploadedFile;
        TusClient.TusClient m_TusClient;
        [SerializeField] string m_ClientApi;
        [SerializeField] string m_ProjectId;
        [SerializeField] string m_OrgId;
        [SerializeField] string m_ProjectName;
        [SerializeField] string m_AccessToken;
        
        List<UploadTask> m_TaskList = new List<UploadTask>();
        int m_CurrentTask;
        bool m_IsError;
        string m_ErrorMessage = "";

        public double Progress
        {
            get { return m_PercentTotal + m_Perc; }
        }
        
        public bool IsError
        {
            get { return m_IsError; }
        }
        
        public string ErrorMessage
        {
            get { return m_ErrorMessage; }
        }
        
        public string Label
        {
            get { return m_Label; }
            set { m_Label = value; }
        }
        
        public FileInfo UploadedFile
        {
            get { return m_UploadedFile; }
            set { m_UploadedFile = value; }
        }
        
        public string ClientAPI
        {
            get { return m_ClientApi; }
            set { m_ClientApi = value; }
        }

        public string Platform
        {
            get { return m_Platform; }
            set { m_Platform = value; }
        }

        public long ElapsedTime
        {
            get { return m_Timer.ElapsedMilliseconds / 1000; }
        }
                
        public void UploadingDelegate(long bytesTransferred, long bytesTotal)
        {
            m_Perc = bytesTransferred / (double)bytesTotal * (100.0 - m_PercentTotal);
            //Debug.Log(string.Format("Up {0:0.00}% {1} of {2}", m_Perc, bytesTransferred, bytesTotal));
        }

        class UploadTask
        {
            public UploadTask(Action start, Action end)
            {
                startAction = start;
                endAction = end;
            }
            public Action startAction;
            public Action endAction;
        }

        public BuildUpload(string label, string path, string clientApi, string platform) : this()
        {
            m_Label = label;
            m_UploadedFile = new FileInfo(path);
            m_ClientApi = clientApi;
            m_Platform = platform.ToLower();
        }
        
        public BuildUpload(string label, string path, string clientApi, string platform, string projectId, string orgId, string projectName, string accessToken) : this(label, path, clientApi, platform)
        {
            m_ProjectId = projectId;
            m_OrgId = orgId;
            m_ProjectName = projectName;
            m_AccessToken = accessToken;
        }

        public BuildUpload()
        {
            Action start = StartBuildGet;
            Action end = EndBuildGet;
            UploadTask task = new UploadTask(start, end);
            m_TaskList.Add(task);
            start = new Action(this.StartBuildCreate);
            end = new Action(this.EndBuildCreate);
            task = new UploadTask(start, end);
            m_TaskList.Add(task);
            start = new Action(this.StartArtifactCreate);
            end = new Action(this.EndArtifactCreate);
            task = new UploadTask(start, end);
            m_TaskList.Add(task);
            start = new Action(this.StartFileUpload);
            end = new Action(this.EndFileUpload);
            task = new UploadTask(start, end);
            m_TaskList.Add(task);
            
            m_Timer.Start();
        }

        void CheckConnection()
        {
            if (!string.IsNullOrEmpty(m_ProjectId))
            {
                return;
            }
            
            if (string.IsNullOrEmpty(CloudProjectSettings.accessToken) || string.IsNullOrEmpty(CloudProjectSettings.projectId))
            {
                throw new UnityException("Unity Cloud Services need to be configured for this project in order to use the deployment options.");
            }

            m_ProjectId = CloudProjectSettings.projectId;
            m_OrgId = CloudProjectSettings.organizationId;
            m_ProjectName = CloudProjectSettings.projectName;
            m_AccessToken = CloudProjectSettings.accessToken;
        }

        void CheckParameters()
        {
            if (m_Label == null || m_Label.Length == 0)
            {
                throw new UnityException("Build label needs to be set");
            }
            if (m_UploadedFile == null)
            {
                throw new UnityException("No file name provided for upload.");
            }
            if (m_ClientApi == null || m_ClientApi.Length == 0)
            {
                throw new UnityException("Client API needs to be set, see https://developer.cloud.unity3d.com/login/me");
            }
        }

        public void StartUpload()
        {
            CheckConnection();
            CheckParameters();
            m_TusClient = null; //TODO: should be checked before
            m_TaskList[m_CurrentTask].startAction();
        }

        public bool IsDone()
        {
            if (m_UploadingTask != null)
            {
                if (m_UploadingTask.IsCompleted)
                    m_TaskList[m_CurrentTask].endAction();
                else
                    return false;
            }
            else if (m_Request != null)
            {
                if (!m_Request.isDone)
                    return false;
                m_TaskList[m_CurrentTask].endAction();
                m_Request = null;
                if (m_IsError)
                {
                    m_CurrentTask = 0;
                    return true;
                }
            }
            else
            {
                return true;
            }
            m_CurrentTask++;
            if (m_CurrentTask < m_TaskList.Count)
            {
                m_TaskList[m_CurrentTask].startAction();
                return false;
            }
            return true;
        }

        void StartBuildGet()
        {
            string targetUrl = k_CloudBuildUrl + m_OrgId + "/projects/" + m_ProjectId + "/buildtargets/_local/builds";
            m_Request = UnityWebRequest.Get(targetUrl);
            m_Request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            m_Request.SetRequestHeader("Authorization", "Bearer " + m_AccessToken);
            m_Request.SendWebRequest();
        }
        
        void EndBuildGet()
        {
            HandleEnd("Cannot get build list", 200);

            JSONNode N = JSON.Parse(m_Request.downloadHandler.text);
            JSONArray arr = N.AsArray;
            Assert.IsNotNull(arr);
            for(int i =0; i < arr.Count; ++i)
            {
                int id = arr[i]["build"].AsInt;
                m_BuildId = (id > m_BuildId) ? id : m_BuildId;
            }
            ++m_BuildId;
        }

        void StartBuildCreate()
        {
            JSONNode node = new JSONObject();
            node["platform"] = m_Platform;
            node["label"] = m_Label;

            string payload = node.ToString();
            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);

            string targetUrl = k_CloudBuildUrl + m_OrgId + "/projects/" + m_ProjectId + "/buildtargets/_local/builds";
            m_Request = new UnityWebRequest(targetUrl, "POST");
            m_Request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            m_Request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            m_Request.SetRequestHeader("Authorization", "Bearer " + m_AccessToken);
            m_Request.SetRequestHeader("Content-Type", "application/json");
            m_Request.SendWebRequest();
        }

        void EndBuildCreate()
        {
            HandleEnd("Cannot create build", 202);
        }

        void HandleEnd(string errorMessage, int desiredCode)
        {
            Debug.Log("HandleEnd: "+m_Request.downloadHandler.text + " err?:" + m_Request.isNetworkError + " code: " + m_Request.responseCode +  " desired: " + desiredCode);
            if (m_Request.isNetworkError || m_Request.responseCode != desiredCode)
            {
                m_IsError = true;
                m_ErrorMessage = errorMessage + ": '" + m_Request.error + "' " + " using " + m_Request.url
                    + "\nTarget: " + m_UploadedFile.Name
                    + "\n" + m_Request.downloadHandler.text;
            }
            Assert.IsNotNull(m_Request);
            m_PercentTotal += 1;
        }

        void StartArtifactCreate()
        {
            JSONNode node = new JSONObject();
            node["name"] = ".ZIP file";
            node["primary"] = true;
            node["public"] = false;
            node["files"] = new JSONArray();
            node["files"][0]["filename"] = m_UploadedFile.Name;
            node["files"][0]["size"] = m_UploadedFile.Length;
            string payload = node.ToString();
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);

            string targetUrl = k_CloudArtifactsUrl + m_ProjectId + "/buildtargets/_local/builds/" + m_BuildId.ToString() + "/artifacts";

            m_Request = new UnityWebRequest(targetUrl, "POST");
            m_Request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            m_Request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            m_Request.SetRequestHeader("Authorization", "Bearer " + m_AccessToken);
            m_Request.SetRequestHeader("Content-Type", "application/json");
            m_Request.SendWebRequest();
        }

        void EndArtifactCreate()
        {
            HandleEnd("Cannot create artifact", 201);
        }

        void StartFileUpload()
        {
            string targetUrl = k_CloudArtifactsUrl + m_ProjectName + "/buildtargets/_local/builds/" + m_BuildId.ToString() + "/artifacts/.ZIP file/upload/" + m_UploadedFile.Name;
            m_UploadingTask = new Task(() => this.FileUploadTask(targetUrl));
            m_UploadingTask.Start();
        }
        
        void FileUploadTask(string targetUrl)  //TODO: blocking call
        {
            Assert.IsNull(m_TusClient);
            m_TusClient = new TusClient.TusClient();
            m_TusClient.Uploading += UploadingDelegate;
            
            m_TusClient.Autorization = "Basic " + m_ClientApi;
            ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
            //string fileURL = m_TusClient.Create(targetUrl, m_UploadedFile);          
            try
            {
                m_TusClient.Upload(targetUrl, m_UploadedFile);
            }
            catch (TusClient.TusException e)
            {
                if (e.Status == System.Net.WebExceptionStatus.RequestCanceled)
                {
                    Debug.Log("Upload Cancelled");
                    m_IsError = true;
                }
                else
                {
                    Debug.Log("Another error");
                    m_IsError = true;
                }
                    
            }
            m_TusClient = null;
        }
        
        void EndFileUpload()
        {
            if(m_UploadingTask.IsFaulted)
            {
                var te = m_UploadingTask.Exception.InnerExceptions[0] as TusClient.TusException;
                Debug.Log(te.FullMessage);
                Debug.Log("Error uploading " + m_UploadingTask.Exception.ToString() + m_UploadingTask.ToString());
            }
            m_UploadingTask = null;
            m_Timer.Stop();
            Debug.Log("Upload Finished");
        }

        public bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool isOk = true;
            // If there are errors in the certificate chain, look at each error to determine the cause.
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
                    {
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                        bool chainIsValid = chain.Build((X509Certificate2)certificate);
                        if (!chainIsValid)
                        {
                            isOk = false;
                        }
                    }
                }
            }
            return isOk;
        }
    }
}
