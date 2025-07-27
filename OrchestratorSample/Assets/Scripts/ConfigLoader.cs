using System.Collections;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class AppConfig
{
    [JsonProperty("orchestratorUrl")] public string OrchestratorUrl;
}

public class ConfigLoader : MonoBehaviour
{
    public static AppConfig Config { get; private set; }
    private const string ConfigFileName = "config.json";

    void Awake()
    {
        // Check if the application config was already loaded
        if (Config != null)
        {
            Debug.Log("Config already loaded.");
            Destroy(gameObject);
            return;
        }

        // Make sure the ConfigLoader sticks around and start a coroutine for loading the config from a file
        DontDestroyOnLoad(gameObject);
        StartCoroutine(LoadConfig());
    }

    private IEnumerator LoadConfig()
    {
        // Get the current working directory
        var basePath = Path.GetDirectoryName(Application.dataPath);

        // Go up one level on macOS, to make sure we are outside the app bundle
        if (Application.platform == RuntimePlatform.OSXPlayer)
        {
            basePath = Path.GetDirectoryName(basePath);
        }

        // Log error, load default config and return if the current working directory could not be determined
        if (basePath == null)
        {
            Debug.LogError("Failed to get base path for config file.");
            LoadDefaultConfig();
            yield break;
        }

        // Build a path with the current working directory and the name of the config file
        var settingsFilePath = Path.Combine(basePath, ConfigFileName);
        Debug.Log($"Loading config from: {settingsFilePath}");

        // Make sure the config file exists
        if (File.Exists(settingsFilePath))
        {
            try
            {
                // Read the config file and deserialize it from JSON
                var jsonText = File.ReadAllText(settingsFilePath);
                Config = JsonConvert.DeserializeObject<AppConfig>(jsonText);
            }
            catch (JsonException ex)
            {
                // Load default config if the JSON could not be parsed
                Debug.LogError($"Failed to parse config JSON: {ex.Message}");
                LoadDefaultConfig();
                yield break;
            }
        }
        else
        {
            // Load default config if the config file does not exist
            Debug.LogError($"Config file not found at: {settingsFilePath}");
            LoadDefaultConfig();
            yield break;
        }

        Debug.Log("Config loaded from file!");
    }

    private void LoadDefaultConfig()
    {
        // Load default config with localhost as the default URL
        Config = new AppConfig()
        {
            OrchestratorUrl = "http://localhost:8090",
        };

        Debug.LogWarning("Using default config.");
    }
}
