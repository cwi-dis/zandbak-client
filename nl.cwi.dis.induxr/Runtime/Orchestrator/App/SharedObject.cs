using UnityEngine;

namespace Orchestrator.App
{
    public class SharedObject
    {
        private readonly Orchestrator _orchestrator;
        private Data.SharedObject _sharedObjectData;

        public Session Session => _orchestrator.CurrentSession;

        public string Id => _sharedObjectData.Id;
        public User Owner => Session.Users.Find((u) => u.Id == _sharedObjectData.Owner.Id);

        public Vector3 Position => new(
            _sharedObjectData.Transform.Position.X,
            _sharedObjectData.Transform.Position.Y,
            _sharedObjectData.Transform.Position.Z
            );

        public Quaternion Rotation => new(
            _sharedObjectData.Transform.Rotation.X,
            _sharedObjectData.Transform.Rotation.Y,
            _sharedObjectData.Transform.Rotation.Z,
            _sharedObjectData.Transform.Rotation.W
            );

        public SharedObject(Orchestrator orchestrator, Data.SharedObject sharedObjectData)
        {
            _orchestrator = orchestrator;
            _sharedObjectData = sharedObjectData;
        }
    }
}
