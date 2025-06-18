using System;
using System.Collections.Generic;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.App
{
    public class Orchestrator : MonoBehaviour
    {
        private List<Session> _sessions;

        private void Start()
        {
        }

        public void GetSessions(Action<List<Session>> callback)
        {
            callback(_sessions);
        }
    }
}