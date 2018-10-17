using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ConnectedGames.Build
{
    public class DeployTools
    {
        const string k_ServerName = "Server";
        const string k_ClientName = "Client";

        [SerializeField] public string m_ClientApi;
        [SerializeField] string m_ProjectId;
        [SerializeField] string m_OrgId;
        [SerializeField] string m_ProjectName;
        [SerializeField] string m_AccessToken;
        
        [SerializeField] List<BuildUpload> m_BuildUploads = new List<BuildUpload>();
        [SerializeField] List<BuildZip> m_BuildZips = new List<BuildZip>();
        [SerializeField] bool m_UploadAfterZip;
        [SerializeField] ProgressUpdate m_OnProgressUpdate;
        
        public delegate void ProgressUpdate(string fileName, double progress);

        public bool Done { get; private set; }
        
        public DeployTools(ProgressUpdate progressUpdate, string clientApi)
        {
            m_OnProgressUpdate = progressUpdate;
            m_ClientApi = clientApi;
        }
        
        public DeployTools(ProgressUpdate progressUpdate, string clientApi, string projectId, string orgId, string projectName, string accessToken) : this(progressUpdate, clientApi)
        {
            m_ProjectId = projectId;
            m_OrgId = orgId;
            m_ProjectName = projectName;
            m_AccessToken = accessToken;
        }
        
        
        // TODO: BuildTools should be zipping, uploading and deploying
        public void CompressAndUpload(string srcPath, string dstPath, string platform, string label)
        {
            Compress(srcPath, dstPath, platform, label);
            // TODO: Proper way is to probably launch a thread which waits for the compress to finish then triggeres upload
            //UploadTarget(dstPath, platform);
            m_UploadAfterZip = true;
        }

        public void Compress(string srcPath, string dstPath, string platform, string label)
        {
            Debug.Log("Compressing " + srcPath + " to " + dstPath);
            var zip = new BuildZip(srcPath, dstPath, platform, label);
            zip.StartZip();
            m_BuildZips.Add(zip);
            m_OnProgressUpdate("Compressing", 0.01);
        }
        
        public void Upload(string label, string fileName, string clientApi, string platform)
        {
            m_BuildUploads.Add(new BuildUpload(label, fileName, clientApi, platform, null, null, null, null));                
        }

        public void Upload(string label, string fileName, string clientApi, string platform, string projectId, string orgId, string projectName, string accessToken)
        {
            m_BuildUploads.Add(new BuildUpload(label, fileName, clientApi, platform, projectId, orgId, projectName, accessToken));                
        }
        
        // TODO: This triggers startAction event on current task, find better method name
        bool StartUploading()
        {
            if (m_BuildUploads.Count == 0)
                return false;
            try
            {
                m_BuildUploads[0].StartUpload();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to start upload: " + e.Message);
                return false;
            }
            return true;
        }
        
        // Format the target (client or server) name according to platform
        // TODO: This could also check that the build is still valid
        public string GetTargetName(bool isClient, string platform)
        {
            if (platform == BuildTarget.NoTarget.ToString())
            {
                return "";
            }
        
            string result = k_ServerName;
            if (isClient)
            {
                result = k_ClientName;
            }
            if (platform == BuildTarget.StandaloneOSX.ToString())
            {
                result += ".app";
            }
            return result;
        }

        public string GenerateLabel(string platform, bool isClient)
        {
            string type = k_ServerName;
            if (isClient)
            {
                type = k_ClientName;
            }

            string shortPlatform = "win";
            if (platform.Equals(BuildTarget.StandaloneLinux64.ToString()))
            {
                shortPlatform = "linux";
            }
            else if (platform.Equals(BuildTarget.StandaloneOSX.ToString()))
            {
                shortPlatform = "osx";
            }
            return Application.productName + "_" + type + "_" + shortPlatform;
        }

        public void UpdateLoop()
        {
            if (m_BuildUploads.Count != 0)
            {
                var currentBuild = m_BuildUploads[0];
                
                double progress = 0.0;
                if (!currentBuild.IsDone())
                {
                    progress += currentBuild.Progress / 100.0;
                    m_OnProgressUpdate("Uploading " + currentBuild.UploadedFile.Name, progress);
                }
                else
                {
                    m_OnProgressUpdate(currentBuild.UploadedFile.Name, 0.0);
                    if (currentBuild.IsError)
                    {
                        Debug.LogError("Upload failed.");
                        Debug.LogError(currentBuild.ErrorMessage);
                        m_BuildUploads.Clear();
                        Done = true;
                    }
                    else
                    {
                        Debug.Log("Upload successful for " + currentBuild.UploadedFile.Name + " in " + currentBuild.ElapsedTime + " seconds.");
                        // There could be more uploads queued, remove this one
                        m_BuildUploads.Remove(currentBuild);
                        if (!StartUploading())
                        {
                            m_BuildUploads.Clear();
                            Done = true;
                        }
                    }
                }
            }
            
            if(m_BuildZips.Count != 0)
            {
                double progress = 0.0;
                int doneCount = 0;
                for (int i = 0; i < m_BuildZips.Count; i++)
                {
                    if (m_BuildZips[i].IsDone)
                        doneCount++;
                }
                if (doneCount != m_BuildZips.Count)
                {
                    for (int i = 0; i < m_BuildZips.Count; i++)
                    {
                        if (!m_BuildZips[i].IsDone)
                        {                        
                            progress += m_BuildZips[i].Progress / (100.0 * m_BuildZips.Count);
                        }
                        else
                        {
                            progress += 100.0 / (100.0 * m_BuildZips.Count);
                        }
                    }

                    m_OnProgressUpdate("Compressing", progress);
                }
                else
                {
                    m_OnProgressUpdate("Compressing finished", 0.0);

                    for (int i = 0; i < m_BuildZips.Count; i++)
                    {
                        Debug.Log("Compressed " + m_BuildZips[i].FileName + " in " + m_BuildZips[i].ElapsedTime + " seconds");
                        if (m_UploadAfterZip)
                        {
                            Upload(m_BuildZips[i].Label, m_BuildZips[i].FileName, m_ClientApi, m_BuildZips[i].Platform, m_ProjectId, m_OrgId, m_ProjectName, m_AccessToken);
                        }
                    }
                    m_BuildZips.Clear();
                    if (StartUploading())
                    {
                        m_OnProgressUpdate("Uploading  " + m_BuildUploads[0].UploadedFile.Name, 0.01);
                    }
                }
            }
        }
    }
}
