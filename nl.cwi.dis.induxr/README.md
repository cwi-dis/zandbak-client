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
- TODO: Identify and include the specific license for this library. (Check the root project or internal documentation).
