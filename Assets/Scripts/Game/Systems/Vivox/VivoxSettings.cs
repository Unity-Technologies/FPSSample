using System;
using System.ComponentModel;
using UnityEngine;
using VivoxUnity;

public static class VivoxSettings
{
    [ConfigVar(Name = "chat.enabled", DefaultValue = "0", Description = "Enable Voice Chat")]
    public static ConfigVar chatEnabled;

    [ConfigVar(Name = "chat.autojoin", DefaultValue = "0", Description = "Auto-join when not in a channel")]
    public static ConfigVar chatAutoJoin;

    [ConfigVar(Name = "chat.showspeaker", DefaultValue = "0", Description = "Show chat bubble speaking icon")]
    public static ConfigVar chatShowBubble;

    [ConfigVar(Name = "chat.pushtotalkkey", DefaultValue = "<unbound>", Description = "Push-to-talk Key")]
    public static ConfigVar pushToTalkKey;

    [ConfigVar(Name = "chat.inputdevice", DefaultValue = "1", Description = "Input Device")]
    public static ConfigVar inputDevice;

    [ConfigVar(Name = "chat.outputdevice", DefaultValue = "1", Description = "OutputDevice")]
    public static ConfigVar outputDevice;

    [ConfigVar(Name = "chat.voicechatvol", DefaultValue = "1", Description = "Voice Chat Volume")]
    public static ConfigVar voiceChatVol;

    [ConfigVar(Name = "chat.micvol", DefaultValue = "1", Description = "Microphone volume")]
    public static ConfigVar micVol;

    // TODO: Revise these into intefaces
    public static ChatSystemClient chatSystemClient = null;
    public static string targetServer = String.Empty;
    public static int teamIndex = -1;

    private static Uri _serverUri = new Uri("https://vdx5.www.vivox.com/api2");
    private static string _tokenDomain = "vdx5.vivox.com";
    private static string _tokenIssuer = "testjyuill-testtalk-dev";
    private static string _tokenKey = "exam707";
    private static TimeSpan _tokenExpiration = TimeSpan.FromSeconds(90);

    private static string _Username;
    private static VivoxUnity.Client _client;
    private static ILoginSession _loginSession;
    private static IChannelSession _positionalChannelSession;
    private static IChannelSession _textChannelSession;
    private static IChannelSession _teamChannelSession;
    private static int _lastTeamIndex = -1;
    private static float _nextPosUpdate = 0;

    // Initialize Vivox client
    public static void Init()
    {
        if (_client != null)
            return;

        _client = new VivoxUnity.Client();
        _client.Initialize(new VivoxUnity.VivoxConfig { InitialLogLevel = vx_log_level.log_debug });
    }

    // Unitialize Vivox client
    public static void Uninit()
    {
        _client.Uninitialize();

        if (_client != null)
        {
            _client.Uninitialize();
            _client = null;
        }
    }

    public static void Update()
    {
        // Pump Vivox
        VivoxUnity.Client.RunOnce();

        // Update positional audio
        if (Time.time > _nextPosUpdate && VivoxSettings._positionalChannelSession != null && VivoxSettings._positionalChannelSession.AudioState == ConnectionState.Connected)
        {
            var cam = Game.game.TopCamera();
            Transform speaker = cam.transform;
            Transform listener = cam.transform;
            VivoxSettings._positionalChannelSession.Set3DPosition(speaker.position, listener.position, listener.forward, listener.up);
            _nextPosUpdate += 0.3f; // Only update after 0.3 or more seconds
        }

        // Update team channels
        if (teamIndex != _lastTeamIndex)
        {
            if (_loginSession != null && _loginSession.State == LoginState.LoggedIn)
            {
                if (_lastTeamIndex >= 0 && _teamChannelSession != null && _teamChannelSession.TextState == ConnectionState.Connected)
                {
                    ChannelId channelId = new ChannelId(_tokenIssuer, targetServer + "_team" + _lastTeamIndex.ToString(), _tokenDomain, ChannelType.NonPositional);
                    LeaveChannel(channelId);
                }

                if (teamIndex >= 0 && (_teamChannelSession == null || _teamChannelSession.TextState == ConnectionState.Disconnected))
                {
                    Debug.Log(String.Format("Switching to team channel {0}", teamIndex));
                    ChannelId channelId = new ChannelId(_tokenIssuer, targetServer + "_team" + teamIndex.ToString(), _tokenDomain, ChannelType.NonPositional);
                    _teamChannelSession = JoinChannel(channelId, false, OnChannelMessageReceived, OnChannelPropertyChanged);
                    _lastTeamIndex = teamIndex;
                }
            }
        }
    }

    public static void Login(String username)
    {
        _loginSession = _client.GetLoginSession(new AccountId(_tokenIssuer, username, _tokenDomain));
        if (_loginSession != null)
        {
            _Username = username;
            _loginSession.PropertyChanged += onLoginSessionPropertyChanged;

            _loginSession.BeginLogin(_serverUri, _loginSession.GetLoginToken(_tokenKey, _tokenExpiration), ar =>
            {
                try
                {
                    _loginSession.EndLogin(ar);
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                    return;
                }

                // At this point, login is successful and other operations can be performed.
                ChannelId positionalChannelId = new ChannelId(_tokenIssuer, targetServer + "_positional", _tokenDomain, ChannelType.Positional);
                _positionalChannelSession = JoinChannel(positionalChannelId, true, null, OnChannelPropertyChanged);

                ChannelId textChannelId = new ChannelId(_tokenIssuer, targetServer + "_text", _tokenDomain, ChannelType.NonPositional);
                _textChannelSession = JoinChannel(textChannelId, false, OnChannelMessageReceived, OnChannelPropertyChanged);
            });
        }
    }

    static private void onLoginSessionPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        if ("State" == propertyChangedEventArgs.PropertyName)
        {
            ILoginSession session = sender as ILoginSession;
        }
    }

    static public void Logout()
    {
        if (_loginSession != null)
        {
            _loginSession.Logout();
            _loginSession.PropertyChanged -= onLoginSessionPropertyChanged;
            _loginSession = null;
        }
    }

    static public IChannelSession JoinChannel(ChannelId channelId, bool enableAudio, EventHandler<QueueItemAddedEventArgs<IChannelTextMessage>> onChannelMessageReceived, PropertyChangedEventHandler onChannelPropertyChanged)
    {
        IChannelSession channelSession = _loginSession.GetChannelSession(channelId);

        if (onChannelPropertyChanged != null)
        {
            channelSession.PropertyChanged += onChannelPropertyChanged;
        }

        channelSession.BeginConnect(enableAudio, (onChannelMessageReceived != null), TransmitPolicy.Yes, channelSession.GetConnectToken(_tokenKey, _tokenExpiration), ar =>
        {
            try
            {
                channelSession.EndConnect(ar);

                if (onChannelMessageReceived != null)
                {
                    channelSession.MessageLog.AfterItemAdded += onChannelMessageReceived;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());

                channelSession = null;
            }

        });

        return channelSession;
    }

    private static void OnChannelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        var channelSession = (IChannelSession)sender;
        
        if ("AudioState" == propertyChangedEventArgs.PropertyName)
        {
            Debug.Log(String.Format("channel {0}.{1} changed to {2}", channelSession.Channel.Name, propertyChangedEventArgs.PropertyName, channelSession.AudioState.ToString()));
        }

        if ("TextState" == propertyChangedEventArgs.PropertyName)
        {
            Debug.Log(String.Format("channel {0}.{1} changed to {2}", channelSession.Channel.Name, propertyChangedEventArgs.PropertyName, channelSession.TextState.ToString()));
        }
    }

    private static void OnChannelMessageReceived(object sender, QueueItemAddedEventArgs<IChannelTextMessage> queueItemAddedEventArgs)
    {
        if (VivoxSettings.chatSystemClient != null)
        {
            var message = queueItemAddedEventArgs.Value.Message;
            VivoxSettings.chatSystemClient.ReceiveMessage(message);
        }
    }

    static public void LeaveChannel(ChannelId channelId)
    {
        if (_loginSession != null)
        {
            var channelSession = _loginSession.GetChannelSession(channelId);
            if (channelSession != null)
            {
                // Disconnect from channel 
                channelSession.Disconnect();

                // (Optionally) deleting the channel entry from the channel list
                _loginSession.DeleteChannelSession(channelId);
            }
        }
    }

    static private void SendMessage(IChannelSession channel, string message)
    {
        if (channel != null && channel.TextState == ConnectionState.Connected)
        {
            channel.BeginSendText(message, ar =>
            {
                try
                {
                    channel.EndSendText(ar);
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            });
        }
    }

    static public void SendGlobalMessage(string message)
    {
        SendMessage(_textChannelSession, message);
    }

    static public void SendTeamMessage(string message)
    {
        SendMessage(_teamChannelSession, message);
    }
}