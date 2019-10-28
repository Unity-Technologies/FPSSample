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

namespace VivoxUnity
{
    /// <summary>
    /// Common properties representing a player in a channel
    /// </summary>
    public interface IParticipantProperties
    {
        /// <summary>
        /// True if this participant corresponds to the currently connected user.
        /// </summary>
        bool IsSelf { get; }
        /// <summary>
        /// If true, the user is in audio
        /// </summary>
        bool InAudio { get; }
        /// <summary>
        /// If true, the user is in text
        /// </summary>
        bool InText { get; }
        /// <summary>
        /// If true, the user is speaking
        /// </summary>
        bool SpeechDetected { get; }
        /// <summary>
        /// This is the energy or intensity of the participants audio.
        /// </summary>
        /// <remarks>
        /// <para>This is used to determine how loud the user is speaking.  This is a value between 0 and 1.</para>
        /// <para>Participant property events by default are only sent on participant state change (starts talking, stops talking, is muted, is unmuted). If set to a per second rate, messages will be sent at that rate if there has been a change since the last update message. This is always true unless the participant is muted through the SDK, causing no audio energy and no state changes.</para>
        /// <para>Warning: Audio energy may not reach 0 when if the user is no longer speaking or even if the capture device is physically muted.The SDK may still pick up background noise or hum/interference from the physically muted device. To ensure audio energy reaches 0, the capture device must be muted through the SDK.</para>
        /// </remarks>
        double AudioEnergy { get; }
        /// <summary>
        /// Set the gain for this user, only for the currently logged in user.
        /// </summary>
        /// <remarks>
        /// The valid range is between -50 and 50.
        /// Positive values increase volume, negative values decrease volume. 
        /// Zero (default) leaves volume unchanged.
        /// </remarks>
        int LocalVolumeAdjustment { get; set; }
        /// <summary>
        /// Use this to silence or un-silence this participant for the currently connected user only.
        /// </summary>
        bool LocalMute { get; set; }
        /// <summary>
        /// Indicates if the user is currently typing.
        /// </summary>
        [System.Obsolete("This feature is currently not implemented")]
        bool IsTyping { get; }
        /// <summary>
        /// Indicates if the user has been muted for all users.
        /// </summary>
        bool IsMutedForAll { get; }
    }
}
