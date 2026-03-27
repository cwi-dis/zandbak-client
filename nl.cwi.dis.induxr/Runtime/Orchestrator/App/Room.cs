namespace Orchestrator.App
{
    public class Room
    {
        private readonly Orchestrator _orchestrator;
        private Data.Room _roomData;

        public Data.Room RoomData
        {
            set => _roomData = value;
        }

        public string Id => _roomData.Id;
        public string Name => _roomData.Name;
        public string Description => _roomData.Description;
        public string Model => _roomData.Model;

        public Room(Orchestrator orchestrator, Data.Room roomData)
        {
            _orchestrator = orchestrator;
            _roomData = roomData;
        }
    }
}
