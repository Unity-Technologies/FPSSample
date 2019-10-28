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

using System;
using System.Text.RegularExpressions;

namespace VivoxUnity
{
    /// <summary>
    /// Properties to control the 3D effects applied to audio in Positional Channels
    /// </summary>
    public sealed class Channel3DProperties
    {
        private readonly int _audibleDistance;
        private readonly int _conversationalDistance;
        private readonly float _audioFadeIntensityByDistance;
        private readonly AudioFadeModel _audioFadeModel;

        /// <summary>
        /// The maximum distance from the listener that a speaker can be heard.
        /// </summary>
        /// <remarks>
        /// Any players within this distance from you in any direction will appear in the same positional
        /// voice channel as you and may be heard.When a player crosses this threshold distance from your perspective,
        /// an IChannelSession event will fire, either EventParticipantAdded when a player comes within this distance,
        /// or EventParticipantLeft when a player moves beyond it.You will stop receiving audio from participants
        /// beyond this range, even before the participant left event is called, but are guaranteed to receive the
        /// added event before receiving audio.The value of this property is measured in arbitrary “distance units,”
        /// so can be set to any scale and does not need to conform to any real units.The default value is 2700.
        /// </remarks>
        public int AudibleDistance => _audibleDistance;
        /// <summary>
        /// The distance from the listener within which a speaker’s voice is heard at its original volume, and beyond which the speaker's voice begins to fade.
        /// </summary>
        /// <remarks>
        /// This property is measured in arbitrary “distance units,” but should use the same scale as
        /// audibleDistance.Your 3D audio experience will sound the most realistic when the value of this property
        /// is set to half the height of a typical player avatar in your game. For near-human-sized entities, this
        /// means about 1 meter, 90 centimeters, or 3 feet.The default value is 90.
        /// </remarks>
        public int ConversationalDistance => _conversationalDistance;
        /// <summary>
        /// How strong the effect of the audio fade is as the speaker moves away from the listener, past the conversational distance. 1=normal strength, .5=half strength, 2=double strength, etc.
        /// </summary>
        /// <remarks>
        /// This parameter is a scalar used in the audio fade calculations as either a constant multiplier, or
        /// an exponent, depending on the audioFadeModel used.In this way, it scales up or down the result of the audio
        /// attenuation at different distances, as determined by the model's formula. Values greater than 1.0 mean audio
        /// will fade quicker as you move away from the conversational distance, and less than 1.0 mean that is will
        /// fade slower.The default value is 1.0.
        /// </remarks>
        public float AudioFadeIntensityByDistance => _audioFadeIntensityByDistance;

        /// <summary>
        /// The model used to determine how loud a voice is at different distances.
        /// </summary>
        /// <remarks>
        /// Voice heard within the conversationalDistance is at the original speaking volume, and voice from speakers
        /// past the audibleDistance will no longer be transmitted.The loudness of the audio at every other distance within
        /// this range is controlled by one of three possible audio fade models.The default value is InverseByDistance, which
        /// is the most realistic.
        /// - InverseByDistance
        ///         - Fades voice quickly at first, buts slows down as you get further from conversational distance. 
        ///         - Formally, the attenuation increases in inverse proportion to the distance.This option models real life acoustics,
        ///           and will sound the most natural.
        /// - LinearByDistance
        ///         - Fades voice slowly at first, but speeds up as you get further from conversational distance. Formally,
        ///           the attenuation increases in linear proportion to the distance.The audioFadeIntensityByDistance factor is
        ///           the negative slope of the attenuation curve.This option can be thought of as a compromise between
        ///           realistic acoustics and a radio channel with no distance attenuation.
        /// - ExponentialByDistance
        ///         - Fades voice extremely quickly beyond conversational distance + 1. Formally, the attenuation increases in
        ///           inverse proportion to the distance raised to the power of the audioFadeIntensityByDistance factor.It
        ///           shares a curve shape similar to realistic attenuation, but allows for much steeper rolloff. This option
        ///           can be used to apply a 'cocktail party effect' to the audio space; by tuning the
        ///           audioFadeIntensityByDistance, this model allows nearby participants to be understandable while mixing
        ///           farther participants’ conversation into a bearable and non-intrusive chatter.
        /// </remarks>    
        public AudioFadeModel AudioFadeModel => _audioFadeModel;

        /// <summary>
        /// Default constructor that sets fields to their suggested values.
        /// </summary>
        public Channel3DProperties()
        {
            _audibleDistance = 32;
            _conversationalDistance = 1;
            _audioFadeIntensityByDistance = 1.0f;
            _audioFadeModel = AudioFadeModel.InverseByDistance;
        }

        internal Channel3DProperties(string properties)
        {
            Regex regex = new Regex(@"([^-]+)-([^-]+)-([^-]+)-([^-]+)");
            var matches = regex.Matches(properties);
            _audibleDistance = int.Parse(matches[0].Groups[1].Value);
            _conversationalDistance = int.Parse(matches[0].Groups[2].Value);
            _audioFadeIntensityByDistance = float.Parse(matches[0].Groups[3].Value);
            _audioFadeModel = (AudioFadeModel)int.Parse(matches[0].Groups[4].Value);
        }

        /// <summary>
        /// Constructor used to set all 3D channel properties. Please refer to the Vivox Core Unreal Developer Guide for recommendations on picking values for different 3D scenarios.
        /// </summary>
        /// <param name="audibleDistance">The maximum distance from the listener that a speaker can be heard. Must be &gt; 0</param>
        /// <param name="conversationalDistance">The distance from the listener within which a speaker’s voice is heard at its original volume. Must be &gt;= 0 and &lt;= audibleDistance.</param>
        /// <param name="audioFadeIntensityByDistanceaudio">How strong the effect of the audio fade is as the speaker moves away from the listener. Must be &gt;= 0. This value will be rounded to three decimal places</param>
        /// <param name="audioFadeModel">The model used to determine how loud a voice is at different distances.</param>
        public Channel3DProperties(int audibleDistance, int conversationalDistance, float audioFadeIntensityByDistanceaudio, AudioFadeModel audioFadeModel)
        {
            _audibleDistance = audibleDistance;
            _conversationalDistance = conversationalDistance;
            _audioFadeIntensityByDistance = audioFadeIntensityByDistanceaudio;
            _audioFadeModel = audioFadeModel;
        }

        /// <summary>
        /// Checks if the current member variables have valid values.
        /// </summary>
        /// <returns>Whether all member variables have valid values.</returns>
        internal bool IsValid()
        {
            return _audibleDistance > 0
                && _conversationalDistance >= 0 && _conversationalDistance <= _audibleDistance
                && _audioFadeIntensityByDistance >= 0
                && Enum.IsDefined(typeof(AudioFadeModel), _audioFadeModel);
        }

        /// <summary>
        /// Creates a 3D Positional URI from the values of the member variables.
        /// </summary>
        /// <returns>A string embedded with value's of the member variables, and formatted to fit the design of a positional channel's URI.</returns>
        public override String ToString()
        {
            return $"!p-{_audibleDistance}-{_conversationalDistance}-{_audioFadeIntensityByDistance.ToString("0.000", new System.Globalization.CultureInfo("en-US"))}-{(int)_audioFadeModel}";
        }
    }
}