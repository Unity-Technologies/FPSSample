public class ServerListConfig
{
    // The Url of the serverlist service endpoint
    public string Url { get; set; }

    // The time in seconds between serverlist GET calls
    public int Period { get; set; }

    public static ServerListConfig BasicConfig(string projectId)
    {
        return new ServerListConfig() 
        {
            Url = string.Format("http://104.154.156.161:8080/api/projects/{0}/servers?multiplay=true", projectId),
            Period = 5
        };
    }
}