/*
Copyright (c) 2014-2018 by Mercer Road Corp

Permission to use, copy, modify or distribute this software in binary or source form
for any purpose is allowed only under explicit prior consent in writing from Mercer Road Corp

THE SOFTWARE IS PROVIDED "AS IS" AND MERCER ROAD CORP DISCLAIMS
ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL MERCER ROAD CORP
BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL
DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR
PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS
ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS
SOFTWARE.
*/

using System.ComponentModel;

namespace VivoxUnity
{
    /// <summary>
    /// The state of the login session.
    /// </summary>
    [DefaultValue(LoggedOut)]
    public enum LoginState
    {
        /// <summary>
        /// Login Session is logged out.
        /// </summary>
        LoggedOut = vx_login_state_change_state.login_state_logged_out,
        /// <summary>
        /// Login Session is logged in.
        /// </summary>
        LoggedIn = vx_login_state_change_state.login_state_logged_in,
        /// <summary>
        /// Login Session is in the process of logging in.
        /// </summary>
        LoggingIn = vx_login_state_change_state.login_state_logging_in,
        /// <summary>
        /// Login Session is in the process of logging out.
        /// </summary>
        LoggingOut = vx_login_state_change_state.login_state_logging_out
    }

    /// <summary>
    /// How to handle incoming subscriptions.
    /// </summary>
    public enum SubscriptionMode
    {
        /// <summary>
        /// Automatically accept all incoming subscription requests.
        /// </summary>
        Accept = vx_buddy_management_mode.mode_auto_accept,
        /// <summary>
        /// Automatically block all incoming subscription requests.
        /// </summary>
        Block = vx_buddy_management_mode.mode_block,
        /// <summary>
        /// Defer incoming subscription request handling to the application. 
        /// The IncomingSubscriptionRequests collection will raise the AfterItemAdded event in this case.
        /// </summary>
        Defer = vx_buddy_management_mode.mode_application,
    }

    /// <summary>
    /// The online status of the user
    /// </summary>
    public enum PresenceStatus
    {
        /// <summary>
        /// Generally available
        /// </summary>
        Available = vx_buddy_presence_state.buddy_presence_online,
        /// <summary>
        /// Do Not Disturb
        /// </summary>
        DoNotDisturb = vx_buddy_presence_state.buddy_presence_busy,
        /// <summary>
        /// Away
        /// </summary>
        Away = vx_buddy_presence_state.buddy_presence_away,
        /// <summary>
        /// Currently in a call.
        /// </summary>
        InACall = vx_buddy_presence_state.buddy_presence_onthephone,
        /// <summary>
        /// Not available (offline)
        /// </summary>
        Unavailable = vx_buddy_presence_state.buddy_presence_offline,
        /// <summary>
        /// Available to chat
        /// </summary>
        Chat = vx_buddy_presence_state.buddy_presence_chat,
        /// <summary>
        /// Away for an extended period of time.
        /// </summary>
        ExtendedAway = vx_buddy_presence_state.buddy_presence_extended_away
    }

    /// <summary>
    /// How often the SDK will send participant property events while in a channel.
    /// </summary>
    /// <remarks>
    /// <para>Participant property events by default are only sent on participant state change (starts talking, stops talking, is muted, is unmuted). If set to a per second rate, messages will be sent at that rate if there has been a change since the last update message. This is always true unless the participant is muted through the SDK, causing no audio energy and no state changes.</para>
    /// <para>WARNING: Setting this value a non-default value will increase user and server traffic. It should only be done if a real-time visual representation of audio values are needed (e.g., graphic VAD indicator). For a static VAD indicator, the default setting is correct.</para>
    /// </remarks>
    [DefaultValue(StateChange)]
    public enum ParticipantPropertyUpdateFrequency
    {
        /// <summary>
        /// On participant state change (DEFAULT).
        /// </summary>
        StateChange = 100,
        /// <summary>
        /// Never.
        /// </summary>
        Update00Hz = 0,
        /// <summary>
        /// 1 times per second.
        /// </summary>
        Update01Hz = 50,
        /// <summary>
        /// 5 times per second.
        /// </summary>
        Update05Hz = 10,
        /// <summary>
        /// 10 times per second.
        /// </summary>
        Update10Hz = 5
    }

    /// <summary>
    /// The type of channel
    /// </summary>
    public enum ChannelType
    {
        /// <summary>
        /// A typical conferencing channel
        /// </summary>
        NonPositional,
        /// <summary>
        /// A conferencing channel where users' voices are rendered with 3D positional effects.
        /// </summary>
        /// <remarks>Not currently supported.</remarks>
        Positional,
        /// <summary>
        /// A conferencing channel where the user's text and audio is echoed back to that user.
        /// </summary>
        Echo
    }

    /// <summary>
    /// Whether or not to transmit audio into a channel.
    /// </summary>
    public enum TransmitPolicy
    {
        /// <summary>
        /// Do not transmit.
        /// </summary>
        No,
        /// <summary>
        /// Transmit.
        /// </summary>
        Yes
    };

    /// <summary>
    /// Represents the state of any resource with connection semantics (Media and text state).
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// The resource is disconnected
        /// </summary>
        Disconnected,
        /// <summary>
        /// The resource is in the process of connecting
        /// </summary>
        Connecting,
        /// <summary>
        /// The resource is connected
        /// </summary>
        Connected,
        /// <summary>
        /// The resource is in the process of disconnecting
        /// </summary>
        Disconnecting
    }

    /// <summary>
    /// The distance model for a Positional channel, which determines the algorithm to use when computing attenuation.
    /// </summary>
    public enum AudioFadeModel
    {
        /// <summary>
        /// No distance based attenuation is applied. All speakers are rendered as if they were in the same position as the listener.
        /// </summary>
        None = 0,
        /// <summary>
        /// Fades voice quickly at first, buts slows down as you get further from conversational distance.
        /// </summary>
        InverseByDistance = 1,
        /// <summary>
        /// Fades voice slowly at first, but speeds up as you get further from conversational distance.
        /// </summary>
        LinearByDistance = 2,
        /// <summary>
        /// Makes voice within the conversational distance louder, but fade quickly beyond it.
        /// </summary>
        ExponentialByDistance = 3
    }
}