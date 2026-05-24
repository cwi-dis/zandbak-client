using System;
using Concentus;
using Concentus.Enums;
using Orchestrator.App;
using UnityEngine;

namespace Orchestrator.Behaviour
{
    public class VoiceTransmitter : MonoBehaviour
    {
        private const int SampleRate = 48000;
        private const int FrameSamples = 960;
        private const string Channel = "voice";

        [SerializeField]
        private bool pushToTalk = true;
        [SerializeField]
        private KeyCode talkKey = KeyCode.V;
        [SerializeField]
        private int bitrate = 24000;

        private Session _session;
        private string _device;
        private AudioClip _mic;
        private IOpusEncoder _encoder;
        private int _readHead;

        private readonly float[] _frame = new float[FrameSamples];
        private readonly short[] _pcm   = new short[FrameSamples];
        private readonly byte[]  _opus  = new byte[1275];   // max Opus packet
        private ushort _seq;

        public void Bind(Session session)
        {
            _session = session;
            StartCoroutine(BindRoutine());
        }

        private System.Collections.IEnumerator BindRoutine()
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Debug.LogError("Microphone permission denied");
                yield break;
            }

            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("No mic device");
                yield break;
            }

            _device = Microphone.devices[0];
            _mic = Microphone.Start(_device, loop: true, lengthSec: 1, frequency: SampleRate);
            _encoder = OpusCodecFactory.CreateEncoder(SampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
            _encoder.Bitrate = bitrate;
            _readHead = 0;
        }

        private void Update()
        {
            if (_session == null || !_mic) return;

            if (pushToTalk && !Input.GetKey(talkKey)) {
                _readHead = Microphone.GetPosition(_device);   // skip backlog while muted
                return;
            }

            var writeHead = Microphone.GetPosition(_device);
            var available = writeHead - _readHead;
            if (available < 0) available += _mic.samples;

            while (available >= FrameSamples)
            {
                _mic.GetData(_frame, _readHead);
                _readHead = (_readHead + FrameSamples) % _mic.samples;
                available -= FrameSamples;

                for (var i = 0; i < FrameSamples; i++)
                    _pcm[i] = (short)Mathf.Clamp(_frame[i] * 32767f, -32768f, 32767f);

                var len = _encoder.Encode(_pcm, FrameSamples, _opus, _opus.Length);
                if (len <= 0) continue;

                _session.BroadcastBytes(Channel, BuildPacket(_session.Self.Id, _seq++, _opus, len));
            }
        }

        private static byte[] BuildPacket(string userId, ushort seq, byte[] opus, int opusLen)
        {
            var id = System.Text.Encoding.UTF8.GetBytes(userId);
            var pkt = new byte[1 + id.Length + 2 + opusLen];
            var o = 0;

            pkt[o++] = (byte)id.Length;
            Buffer.BlockCopy(id, 0, pkt, o, id.Length); o += id.Length;

            pkt[o++] = (byte)(seq & 0xff);
            pkt[o++] = (byte)(seq >> 8);
            Buffer.BlockCopy(opus, 0, pkt, o, opusLen);

            return pkt;
        }

        private void OnDestroy()
        {
            if (_device != null) Microphone.End(_device);
        }
    }
}
