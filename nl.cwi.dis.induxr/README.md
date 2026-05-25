# INDUX-R Orchestrator Wrapper

The INDUX-R Orchestrator Wrapper is a Unity package designed to facilitate the creation of networked, shared social VR experiences. it provides a high-level API for interacting with the Orchestrator backend, managing sessions, user synchronization, shared objects, and real-time communication.

## Features

- **Session Management**: Create, join, leave, and list sessions.
- **User Authentication**: Simple login/logout system with support for device types (VR, AR, etc.).
- **Shared Objects**: Synchronize transforms and state of game objects across participants with ownership management.
- **Triggers**: Event-based synchronization using JSON payloads.
- **Conversation Bubbles**: Dynamic group management for focused interactions (e.g., spatial audio groups).
- **Real-time Broadcasts**: Send and receive custom data messages over Socket.IO channels.
- **Voice Support**: Integrated voice transmitter and receiver components (utilizing Concentus for Opus).
- **Avatar Synchronization**: Base behaviours for local and remote avatars.

## Requirements

- **Unity**: 6000.0 or newer.
- **Dependencies**:
  - `com.itisnajim.socketiounity` (1.1.4)
  - `com.unity.nuget.newtonsoft-json` (3.2.1)
  - `com.unity.xr.interaction.toolkit` (3.3.1)
  - **Concentus**: Included in the `Plugins` folder (Opus codec implementation).

## Setup

1. **Installation**: Add this package to your Unity project via the Package Manager (using the git URL or local path).
2. **Orchestrator Controller**: Add the `OrchestratorController` prefab (found in `Runtime/Orchestrator/Prefabs`) to your initial scene.
3. **Configuration**:
   - Use `OrchestratorController.Instance.SocketConnectAsync(url)` to establish a connection to your Orchestrator backend.
   - The connection returns an `App.Orchestrator` instance which serves as the primary entry point for the API.

## Getting Started (Minimal Application)

Based on the `OrchestratorSample`, here is how to set up a minimal application using the wrapper.

### 1. Establish Connection & Login
Establish a connection to the Orchestrator and log in with a username.

```csharp
using Orchestrator.Wrapping;
// ...

// 1. Connect
var orchestrator = await OrchestratorController.Instance.SocketConnectAsync("https://your-orchestrator-url");

// 2. Login
var userId = await orchestrator.Login("Username", "OptionalPassword");
```

### 2. Session Management
Once logged in, you can list, create, or join sessions.

```csharp
// List available sessions
var sessions = await orchestrator.GetSessions();

// Join the first available session
if (sessions.Count > 0) {
    var joinedSession = await orchestrator.JoinSession(sessions[0].Id);
}

// Or create a new one (requires a Room object, obtainable via GetRooms())
var rooms = await orchestrator.GetRooms();
var newSession = await orchestrator.CreateSession("My Session", rooms[0]);
```

### 3. In-Session Interaction
After joining a session, you can access it via `OrchestratorController.Instance.Orchestrator.CurrentSession`.

#### Handle Users & Avatars
Listen for users joining/leaving to manage their representations.

```csharp
var session = OrchestratorController.Instance.Orchestrator.CurrentSession;

session.OnUserJoined += (user) => {
    Debug.Log($"{user.Name} joined!");
    // Instantiate remote avatar
};
```

#### Shared Objects & Triggers
Use `TriggerBehaviour` for event-based synchronization.

```csharp
// Sending a trigger
var data = new JObject { { "action", "pulse" } };
triggerBehaviour.PublishTrigger(data);

// Receiving a trigger
triggerBehaviour.OnTriggerReceived += (data) => {
    Debug.Log($"Action received: {data.Value["action"]}");
};
```

### 4. Local Avatar Setup
To synchronize your movement with other participants, you need to set up a local avatar.

#### Create an Avatar Prefab
1. Create a 3D model with a `SkinnedMeshRenderer`.
2. Attach the `LocalAvatar` component to the prefab.
3. (Optional) Assign a `Notification` object to be shown when the user's hand is raised.

#### Instantiate & Initialize
After joining a session, instantiate your avatar and link it to the current user.

```csharp
using Orchestrator.Behaviour.Avatar;
// ...

var session = OrchestratorController.Instance.Orchestrator.CurrentSession;
var user = session.Self;

// Instantiate the prefab
var localAvatar = Instantiate(localPlayerPrefab, spawnPos, Quaternion.identity).GetComponent<LocalAvatar>();

// Initialize with the SelfUser object
localAvatar.Initialize(user);
```

The `LocalAvatar` component will automatically start broadcasting bone transformations from the `SkinnedMeshRenderer` to other participants at the specified `updateRate`.

## Entry Points & Scripts

### Primary API
- `Orchestrator.App.Orchestrator`: The main class for handling login, sessions, and room management.
- `Orchestrator.Wrapping.OrchestratorController`: A MonoBehaviour singleton that manages the socket connection and dispatches events to the API.

### Key Behaviours
- `SharedObjectBehaviour`: Attach to any GameObject to synchronize its position and rotation.
- `TriggerBehaviour`: Attach to GameObjects to enable event-based communication via `PublishTrigger`.
- `VoiceTransmitter` / `VoiceReceiver`: Components for handling real-time audio communication.
- `LocalAvatar` / `RemoteAvatar`: Behaviours for synchronizing player avatars.

## Scripts & Automation
- Currently, no external scripts or CI/CD scripts are bundled directly within the package folder.
- TODO: Add build or deployment scripts if applicable.

## Environment Variables
- This package does not rely on system environment variables. Configuration is typically handled via code when calling `SocketConnectAsync(url)`.
- TODO: Document any backend-specific env vars if relevant for local testing.

## Tests
- TODO: Automated tests are not currently present in the package directory. Manual verification is required using the sample scenes in the parent project.

## Project Structure

```text
nl.cwi.dis.induxr/
├── Plugins/            # Third-party libraries (Concentus/Opus)
├── Runtime/
│   └── Orchestrator/
│       ├── App/        # High-level Application API
│       ├── Behaviour/  # Unity MonoBehaviours (Shared Objects, Voice, Avatars)
│       ├── Data/       # Data models and Interfaces
│       ├── Prefabs/    # Ready-to-use Unity Prefabs
│       ├── Util/       # Utilities (Versioning, ID generation)
│       └── Wrapping/   # Low-level Socket.IO wrapper and Controller
└── package.json        # Package manifest
```

## License

This project is licensed under the BSD 2-Clause License. See the [LICENSE](LICENSE) file for details.

Copyright (c) 2026, Thomas Röggla, cwi-dis. All rights reserved.
