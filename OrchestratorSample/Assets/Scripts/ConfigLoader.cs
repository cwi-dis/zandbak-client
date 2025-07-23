using System.Collections;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class AppConfig
{
    public string orchestratorUrl;
}

public class ConfigLoader : MonoBehaviour
{
    public static AppConfig Config { get; private set; }
    private const string ConfigFileName = "config.json";

    void Awake()
    {
        if (Config != null)
        {
            Debug.Log("Config already loaded.");
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        StartCoroutine(LoadConfig());
    }

    private IEnumerator LoadConfig()
    {
        var basePath = Path.GetDirectoryName(Application.dataPath);

        if (Application.platform == RuntimePlatform.OSXPlayer)
        {
            basePath = Path.GetDirectoryName(basePath);
        }

        if (basePath == null)
        {
            Debug.LogError("Failed to get base path for config file.");
            LoadDefaultConfig();
            yield break;
        }

        var settingsFilePath = Path.Combine(basePath, ConfigFileName);
        Debug.Log($"Loading config from: {settingsFilePath}");

        if (File.Exists(settingsFilePath))
        {
            try
            {
                var jsonText = File.ReadAllText(settingsFilePath);
                Config = JsonConvert.DeserializeObject<AppConfig>(jsonText);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to parse config JSON: {ex.Message}");
                LoadDefaultConfig();
                yield break;
            }
        }
        else
        {
            Debug.LogError($"Config file not found at: {settingsFilePath}");
            LoadDefaultConfig();
            yield break;
        }

        Debug.Log("Config loaded from file!");
    }

    private void LoadDefaultConfig()
    {
        Config = new AppConfig()
        {
            orchestratorUrl = "http://localhost:8090",
        };

        Debug.LogWarning("Using default config.");
    }
}
