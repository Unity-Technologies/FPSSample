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
using System.ComponentModel;

namespace VivoxUnity
{

    /// <summary>
    /// This interface is used to enumerate and manage audio devices.
    /// </summary>
    public interface IAudioDevices : INotifyPropertyChanged
    {
        /// <summary>
        /// Call BeginSetActiveDevice() with this device if you wish to use whatever the operating system uses for the device.
        /// </summary>
        IAudioDevice SystemDevice { get; }
        /// <summary>
        /// The available devices on this system. 
        /// </summary>
        /// <remarks>
        /// <see cref="IAudioDevices.BeginRefresh"/> must be called before accessing this property or values could be stale.
        /// </remarks>
        IReadOnlyDictionary<string, IAudioDevice> AvailableDevices { get; }
        /// <summary>
        /// Call this to set the active audio device. This will take effect immediately for all open sessions.
        /// </summary>
        /// <param name="device">The active device.</param>
        /// <param name="callback">called upon completion.</param>
        /// <returns>an IAsyncResult.</returns>
        IAsyncResult BeginSetActiveDevice(IAudioDevice device, AsyncCallback callback);
        /// <summary>
        /// Call this to pick up failures from the BeginSetActiveDevice() asynchronous method.
        /// </summary>
        /// <param name="result">The value returned from BeginSetActiveDevice().</param>
        void EndSetActiveDevice(IAsyncResult result);
        /// <summary>
        /// The active audio device.
        /// </summary>
        IAudioDevice ActiveDevice { get; }
        /// <summary>
        /// The effective system device. 
        /// </summary>
        /// <remarks>
        /// <para> If the active device is set to SystemDevice or CommunicationDevice, then the effective device will reflect the actual device used.</para>
        /// <para> Note: A PropertyChanged event will fire for this property when its value changes.</para>
        /// </remarks>
        IAudioDevice EffectiveDevice { get; }

        /// <summary>
        /// AudioGain for the device.
        /// </summary>
        /// <remarks>
        /// This is a value between -50 and 50. Positive values make the audio louder, negative values make the audio softer.
        /// 0 leaves the value unchanged (default). This applies to all active audio sessions.
        /// </remarks>
        int VolumeAdjustment { get; set; }
        /// <summary>
        /// Whether audio is muted for this device.
        /// </summary>
        /// <remarks>
        /// Set to true in order to stop the audio device from capturing or rendering audio. 
        /// Default is false.
        /// </remarks>
        bool Muted { get; set; }
        /// <summary>
        /// Refresh the list of available devices. 
        /// </summary>
        /// <param name="cb">the function to call when the operation completes.</param>
        /// <returns>an IAsyncResult.</returns>
        /// <remarks>
        /// BeginRefresh must be called before accessing the <see cref="IAudioDevices.ActiveDevice"/>, <see cref="IAudioDevices.EffectiveDevice"/>, and <see cref="IAudioDevices.AvailableDevices"/> properties.
        /// It may take up to 200 milliseconds before the list of devices is refreshed.
        /// </remarks>
        IAsyncResult BeginRefresh(AsyncCallback cb);
        /// <summary>
        /// Call this to pick up failures from the BeginRefresh() asynchronous method.
        /// </summary>
        /// <param name="result">the result returned from BeginRefresh.</param>
        void EndRefresh(IAsyncResult result);

    }
}
