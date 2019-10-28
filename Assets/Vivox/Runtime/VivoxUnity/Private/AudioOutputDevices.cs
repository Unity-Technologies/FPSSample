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
using System.Diagnostics;
using System.Linq;
using VivoxUnity.Common;

namespace VivoxUnity.Private
{
    internal class AudioOutputDevices : IAudioDevices
    {

        #region Member Variables

        private AudioDevice _systemDevice;
        private AudioDevice _communicationDevice;
        private AudioDevice _activeDevice;
        private AudioDevice _effectiveDevice;
        private int _volumeAdjustment;
        private bool _muted;

        private readonly VxClient _client;
        private readonly ReadWriteDictionary<string, IAudioDevice, AudioDevice> _devices = new ReadWriteDictionary<string, IAudioDevice, AudioDevice>();

        #endregion

        #region Helpers

        int ConvertGain(int gain)
        {
            return gain + 50;
        }

        #endregion

        public AudioOutputDevices(VxClient client)
        {
            _client = client;
            _systemDevice = new AudioDevice { Key = "Default Communication Device", Name = "Default Communication Device" };
            _communicationDevice = new AudioDevice { Key = "Default Communication Device", Name = "Default Communication Device" };
            _activeDevice = _systemDevice;

            VxClient.Instance.EventMessageReceived += OnEventMessageReceived;
        }

        #region IAudioDevices

        public event PropertyChangedEventHandler PropertyChanged;

        public IAudioDevice SystemDevice => _systemDevice;
        public IAudioDevice CommunicationDevice => _communicationDevice;
        public IAudioDevice ActiveDevice => _activeDevice;
        public IAudioDevice EffectiveDevice => _effectiveDevice;
        public IReadOnlyDictionary<string, IAudioDevice> AvailableDevices => _devices;

        public IAsyncResult BeginSetActiveDevice(IAudioDevice device, AsyncCallback callback)
        {
            if (device == null) throw new ArgumentNullException();

            AsyncNoResult result = new AsyncNoResult(callback);
            var request = new vx_req_aux_set_render_device_t();
            request.render_device_specifier = device.Key;
            return _client.BeginIssueRequest(request, ar =>
            {
                try
                {
                    _client.EndIssueRequest(ar);

                    // When trying to set the active device to what is already the active device, return.
                    if (_activeDevice.Key == device.Key)
                    {
                        return;
                    }
                    _activeDevice = (AudioDevice)device;

                    if (_activeDevice == AvailableDevices["Default System Device"])
                    {
                        _effectiveDevice = new AudioDevice
                        {
                            Key = _systemDevice.Key,
                            Name = _systemDevice.Name
                        };
                    }
                    else if (_activeDevice == AvailableDevices["Default Communication Device"])
                    {
                        _effectiveDevice = new AudioDevice
                        {
                            Key = _communicationDevice.Key,
                            Name = _communicationDevice.Name
                        };
                    }
                    else
                    {
                        _effectiveDevice = new AudioDevice
                        {
                            Key = device.Key,
                            Name = device.Name
                        };
                    }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EffectiveDevice)));

                    result.SetComplete();
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    result.SetComplete(e);
                }
            });
        }

        public void EndSetActiveDevice(IAsyncResult result)
        {
            ((AsyncNoResult)result).CheckForError();
        }

        public int VolumeAdjustment
        {
            get { return _volumeAdjustment; }
            set
            {
                if (value < -50 || value > 50)
                    throw new ArgumentOutOfRangeException();
                if (value == _volumeAdjustment) return;
                _volumeAdjustment = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VolumeAdjustment)));
                var request = new vx_req_aux_set_speaker_level_t();
                request.level = ConvertGain(value);
                _client.BeginIssueRequest(request.base_, ar =>
                {
                    try
                    {
                        _client.EndIssueRequest(ar);
                    }
                    catch (Exception e)
                    {
                        VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    }
                });
            }
        }

        public bool Muted
        {
            get { return _muted; }
            set
            {
                if (value == _muted) return;
                _muted = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Muted)));
                var request = new vx_req_connector_mute_local_speaker_t(); 
                request.mute_level = (value ? 1 : 0);
                _client.BeginIssueRequest(request, ar =>
                {
                    try
                    {
                        _client.EndIssueRequest(ar);
                    }
                    catch (Exception e)
                    {
                        VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    }
                });
            }
        }

        public IAsyncResult BeginRefresh(AsyncCallback callback)
        {
            AsyncNoResult result = new AsyncNoResult(callback);
            var request = new vx_req_aux_get_render_devices_t();
            _client.BeginIssueRequest(request, ar =>
            {
                vx_resp_aux_get_render_devices_t response;
                try
                {
                    response = _client.EndIssueRequest(ar);
                    _devices.Clear();
                    for (var i = 0; i < response.count; ++i)
                    {
                        var device = VivoxCoreInstance.get_device(i, response.render_devices);
                        var id = device.device;
                        var name = device.display_name;
                        _devices[id] = new AudioDevice { Key = id, Name = name };
                    }

                    _systemDevice = new AudioDevice
                    {
                        Key = response.default_render_device.device,
                        Name = response.default_render_device.display_name
                    };
                    _communicationDevice = new AudioDevice
                    {
                        Key = response.default_communication_render_device.device,
                        Name = response.default_communication_render_device.display_name
                    };
                    var effectiveDevice = new AudioDevice
                    {
                        Key = response.effective_render_device.device,
                        Name = response.effective_render_device.display_name,
                    };
                    if (!effectiveDevice.Equals(_effectiveDevice))
                    {
                        // Only fire the event if the effective device has truly changed.
                        _effectiveDevice = effectiveDevice;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EffectiveDevice)));
                    }
                    result.SetComplete();
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    result.SetComplete(e);
                    if (VivoxDebug.Instance.throwInternalExcepetions)
                    {
                        throw;
                    }
                    return;
                }
            });
            return result;
        }

        public void EndRefresh(IAsyncResult result)
        {
            (result as AsyncNoResult)?.CheckForError();
        }

        #endregion

        private void OnEventMessageReceived(vx_evt_base_t eventMessage)
        {
            if (eventMessage.type == vx_event_type.evt_audio_device_hot_swap)
            {
                HandleDeviceHotSwap(eventMessage);
            }
        }

        private void HandleDeviceHotSwap(vx_evt_base_t eventMessage)
        {
            BeginRefresh(new AsyncCallback((IAsyncResult result) =>
            {
                try
                {
                    EndRefresh(result);
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"BeginRefresh failed: {e}");
                    if (VivoxDebug.Instance.throwInternalExcepetions)
                    {
                        throw;
                    }
                    return;
                }
            }));
        }

        public void Clear()
        {
            _devices.Clear();
            _activeDevice = _systemDevice;
            _effectiveDevice = null;
            _muted = false;
            _volumeAdjustment = 0;
        }
    }
}
