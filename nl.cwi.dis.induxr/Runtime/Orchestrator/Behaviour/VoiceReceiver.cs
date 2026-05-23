using System;
using System.Collections.Generic;
using Concentus;
using Orchestrator.Data;
using UnityEngine;

namespace Orchestrator.Behaviour
{
    public class VoiceReceiver : MonoBehaviour
    {
        private const string Channel = "voice";

        private App.Session _session;
        private readonly Dictionary<string, VoicePlayback> _playbacks = new();

        public void Bind(App.Session session)
        {
            _session = session;
            _session.OnBroadcastDataReceived += OnBroadcast;
        }

        private void OnDestroy()
        {
            // Unsubscribe from broadcasts
            if (_session != null) _session.OnBroadcastDataReceived -= OnBroadcast;
            // Dispose of playback sources
            foreach (var p in _playbacks.Values) p.Dispose();
        }

        private void OnBroadcast(BroadcastData data)
        {
            // Ensure the message is for our channel and contains data
            if (data.Channel != Channel || data.Bytes == null) return;
            // Parse the packet
            if (!TryParse(data.Bytes, out var userId, out var seq, out var off, out var len)) return;
            // Drop packet if it's from us
            if (userId == _session.Self.Id) return;       // ignore our own loopback

            // If there is no playback source for the user, create one
            if (!_playbacks.TryGetValue(userId, out var pb)) {
                pb = new VoicePlayback(_session.FindUserById(userId));
                _playbacks[userId] = pb;
            }

            // Add packet to playback source
            pb.Push(seq, data.Bytes, off, len);
        }

        private static bool TryParse(byte[] bytes, out string userId, out ushort seq, out int opusOffset, out int opusLength)
        {
            userId = null;
            seq = 0;
            opusOffset = 0;
            opusLength = 0;

            if (bytes.Length < 4) return false;

            int idLen = bytes[0];

            if (bytes.Length < 3 + idLen) return false;

            userId  = System.Text.Encoding.UTF8.GetString(bytes, 1, idLen);
            seq     = (ushort)(bytes[1 + idLen] | (bytes[2 + idLen] << 8));
            opusOffset = 3 + idLen;
            opusLength = bytes.Length - opusOffset;

            return true;
        }
    }

    internal class VoicePlayback : IDisposable
    {
        private const int SampleRate = 48000;
        private const int FrameSamples = 960;

        private readonly IOpusDecoder _decoder = OpusCodecFactory.CreateDecoder(SampleRate, 1);
        private readonly Queue<float[]> _frames = new();
        private readonly object _lock = new();
        private readonly GameObject _go;
        private float[] _current;
        private int _currentOffset;
        private ushort _lastSeq;
        private bool _haveLast;

        public VoicePlayback(App.User user)
        {
            _go = new GameObject($"Voice_{user?.Name ?? "unknown"}");

            // TODO: parent to user's avatar transform for spatialised audio
            var source = _go.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.loop = true;
            source.clip = AudioClip.Create("voice_stream", SampleRate, 1, SampleRate, stream: true, OnAudioRead);
            source.Play();
        }

        public void Push(ushort seq, byte[] pkt, int offset, int len)
        {
            if (_haveLast && (short)(seq - _lastSeq) <= 0) return;
            _lastSeq = seq;
            _haveLast = true;

            var decoded = new float[FrameSamples];
            _decoder.Decode(pkt.AsSpan(offset, len), decoded, FrameSamples);
            lock (_lock) _frames.Enqueue(decoded);
        }

        private void OnAudioRead(float[] buffer) // called on audio thread
        {
            var written = 0;

            while (written < buffer.Length)
            {
                if (_current == null || _currentOffset >= _current.Length)
                {
                    lock (_lock)
                    {
                        if (_frames.Count == 0)
                        {
                            Array.Clear(buffer, written, buffer.Length - written); // underrun → silence
                            return;
                        }

                        _current = _frames.Dequeue();
                        _currentOffset = 0;
                    }
                }

                var copy = Math.Min(buffer.Length - written, _current.Length - _currentOffset);
                Array.Copy(_current, _currentOffset, buffer, written, copy);
                _currentOffset += copy;
                written += copy;
            }
        }

        public void Dispose()
        {
            if (_go) UnityEngine.Object.Destroy(_go);
        }
    }
}
