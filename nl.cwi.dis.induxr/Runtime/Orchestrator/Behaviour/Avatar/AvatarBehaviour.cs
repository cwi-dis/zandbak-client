using Orchestrator.App;
using UnityEngine;

namespace Orchestrator.Behaviour.Avatar
{
    public abstract class AvatarBehaviour : MonoBehaviour
    {
        public abstract void Initialize(User user);
    }
}
