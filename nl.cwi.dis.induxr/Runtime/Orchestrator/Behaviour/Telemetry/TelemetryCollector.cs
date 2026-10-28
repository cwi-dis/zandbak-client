using System;
using System.Collections.Generic;
using Orchestrator.Data;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.Behaviour.Telemetry
{
    public class TelemetryCollector : MonoBehaviour
    {
        public static TelemetryCollector Instance { get; private set; }

        [Header("Settings")]
        public float reportInterval = 5f;
        public float syncInterval = 10f;

        public event Action<TelemetryData> OnTelemetryReported;

        private Orchestrator.App.Orchestrator _orchestrator;
        private float _reportTimer;
        private float _syncTimer;

        // RTT State
        private float _lastRtt;

        // Jitter State
        private float _lastPoseArrivalTime;
        private float _jitter;

        // Packet Loss State
        private readonly Dictionary<string, ushort> _lastVoiceSeq = new();
        private long _packetsExpected;
        private long _packetsLost;

        public float RttMs => _lastRtt;
        public float JitterMs => _jitter * 1000f;
        public float PacketLossRate => _packetsExpected > 0 ? (float)_packetsLost / _packetsExpected : 0;

        public void Awake()
        {
            Instance = this;
            _orchestrator = OrchestratorController.Instance.Orchestrator;
            _orchestrator.OnSessionJoined += OnSessionJoined;

            if (_orchestrator.CurrentSession != null)
            {
                OnSessionJoined(_orchestrator.CurrentSession);
            }
        }

        private void OnSessionJoined(App.Session session)
        {
            session.OnBroadcastDataReceived += OnBroadcastReceived;

            foreach (var user in session.Users)
            {
                user.OnAvatarPoseReceived += OnAvatarPoseReceived;
            }

            session.OnUserJoined += (user) => user.OnAvatarPoseReceived += OnAvatarPoseReceived;
        }

        private void Update()
        {
            if (_orchestrator?.Self == null) return;

            _syncTimer += Time.deltaTime;
            if (_syncTimer >= syncInterval)
            {
                _syncTimer = 0;
                SyncClock();
            }

            _reportTimer += Time.deltaTime;
            if (!(_reportTimer >= reportInterval)) return;

            _reportTimer = 0;
            ReportTelemetry();
        }

        private async void SyncClock()
        {
            var start = _orchestrator.GetMonotonicTime();
            var ntpTime = (long)await _orchestrator.GetNtpTime();
            var end = _orchestrator.GetMonotonicTime();

            var rttMs = end - start;
            _lastRtt = rttMs;

            var serverTime = ntpTime + (rttMs / 2);
            _orchestrator.UpdateNtpOffset(serverTime - end);
        }

        private void OnAvatarPoseReceived(AvatarPoseData pose)
        {
            var now = Time.realtimeSinceStartup;

            if (_lastPoseArrivalTime > 0)
            {
                var delta = now - _lastPoseArrivalTime;
                // Simple jitter calculation: moving average of absolute differences in inter-arrival times
                // Assuming a fixed expected interval (e.g., 0.1s for 10Hz), but we look at variance.
                _jitter = _jitter * 0.9f + Math.Abs(delta - 0.1f) * 0.1f;
            }

            _lastPoseArrivalTime = now;
        }

        private void OnBroadcastReceived(BroadcastData data)
        {
            if (data.Channel != "voice") return;

            // Extract seq from voice packet
            // pkt[0] = idLen
            // pkt[1..1+idLen] = userId
            // pkt[1+idLen] = seq low
            // pkt[1+idLen+1] = seq high
            var pkt = data.Bytes;
            if (pkt == null || pkt.Length < 3) return;

            int idLen = pkt[0];
            if (pkt.Length < 1 + idLen + 2) return;

            // We don't necessarily need the string userId if we just want global loss,
            // but tracking per user is better for accuracy.
            var userId = System.Text.Encoding.UTF8.GetString(pkt, 1, idLen);
            var seq = (ushort)(pkt[1 + idLen] | (pkt[2 + idLen] << 8));

            if (_lastVoiceSeq.TryGetValue(userId, out ushort lastSeq))
            {
                int diff = (ushort)(seq - lastSeq);

                if (diff > 1 && diff < 100) // Reasonable gap
                {
                    _packetsLost += (diff - 1);
                }

                if (diff > 0)
                {
                    _packetsExpected += diff;
                }
            }
            else
            {
                _packetsExpected++;
            }

            _lastVoiceSeq[userId] = seq;
        }

        private async void ReportTelemetry()
        {
            var lossRate = _packetsExpected > 0 ? (float)_packetsLost / _packetsExpected : 0;

            var data = new TelemetryData
            {
                RttMs = _lastRtt,
                JitterMs = _jitter * 1000f,
                PacketLossRate = lossRate,
                Timestamp = _orchestrator.GetTimeWithOffset()
            };

            var result = await _orchestrator.ReportTelemetry(data);
            OnTelemetryReported?.Invoke(result);

            // Reset loss counters for next interval
            _packetsExpected = 0;
            _packetsLost = 0;
        }
    }
}
