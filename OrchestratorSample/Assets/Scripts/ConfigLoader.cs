using UnityEngine;

[System.Serializable]
public class AppConfig
{
    public string orchestratorUrl;
}

public class ConfigLoader : MonoBehaviour
{
    public static AppConfig Config { get; private set; }
    private const string ConfigFileName = "config";

    void Awake()
    {
        if (Config != null) return;

        var configTextAsset = Resources.Load<TextAsset>(ConfigFileName);

        // config.json was not found, use default values
        if (configTextAsset == null)
        {
            Debug.LogError($"Config file '{ConfigFileName}.json' not found in Resources folder.");

            Config = new AppConfig
            {
                orchestratorUrl = "http://localhost:8090"
            };

            return;
        }

        try
        {
            Config = JsonUtility.FromJson<AppConfig>(configTextAsset.text);
            Debug.Log($"Loaded server address: {Config.orchestratorUrl}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse config file: {e.Message}");
        }

        DontDestroyOnLoad(gameObject);
    }
}
