using UnityEngine;

public class StatModifier : Mod
{
    static bool saving = false;
    static string fileName
    {
        get
        {
            string time = "";
            if (saving)
                time = System.DateTime.Now.ToString(SaveAndLoad.dateTimeFormattingSaveFile);
            else
                time = new System.DateTime(SaveAndLoad.WorldToLoad.lastPlayedDateTicks).ToString(SaveAndLoad.dateTimeFormattingSaveFile);
            string path = System.IO.Path.Combine(SaveAndLoad.WorldPath, SaveAndLoad.CurrentGameFileName, time);
            if (System.IO.Directory.Exists(path))
                return System.IO.Path.Combine(path,"StatModifier.json");
            return System.IO.Path.Combine(SaveAndLoad.WorldPath, SaveAndLoad.CurrentGameFileName, time + SaveAndLoad.latestStringNameEnding, "StatModifier.json");
        }
    }
    static float[,] oldValues = null;
    private static float modiValue = 1f;
    static float currentModifier
    {
        get
        {
            return modiValue;
        }
        set {
            modiValue = value;
            HarmonyLib.Traverse fetcher = HarmonyLib.Traverse.Create(typeof(GameModeValueManager)).Field("gameModeValues");
            SO_GameModeValue[] data = fetcher.GetValue<SO_GameModeValue[]>();
            for (int i = 0;i < data.Length;i++)
            {
                data[i].nourishmentVariables.foodDecrementRateMultiplier = oldValues[i, 0] * modiValue;
                data[i].nourishmentVariables.thirstDecrementRateMultiplier = oldValues[i, 1] * modiValue;
            }
        }

    }
    public void Start()
    {
        HarmonyLib.Traverse fetcher = HarmonyLib.Traverse.Create(typeof(GameModeValueManager)).Field("gameModeValues");
        SO_GameModeValue[] data = fetcher.GetValue<SO_GameModeValue[]>();
        oldValues = new float[data.Length, 2];
        for (int i = 0; i < data.Length; i++)
        {
            oldValues[i, 0] = data[i].nourishmentVariables.foodDecrementRateMultiplier;
            oldValues[i, 1] = data[i].nourishmentVariables.thirstDecrementRateMultiplier;
        }
        Debug.Log("Mod StatModifier has been loaded!");
    }

    [ConsoleCommand(name: "statmodifier", docs: "Changes or returns current stat modifier")]
    public static string MyCommand(string[] args)
    {
        if (args.Length == 0)
            return currentModifier.ToString();
        if (args.Length > 1) 
            return "Too many arguments";
        try
        {
            currentModifier = float.Parse(args[0]);
            return "Stat modifier is now " + currentModifier.ToString();
        } catch
        {
            return "Failed to parse \"" + args[0] + "\" as a number";
        }
    }

    override public void WorldEvent_WorldSaved()
    {
        saving = true;
        JSONObject customSaveData = getSaveJson();
        if (customSaveData.HasField("Modifier"))
        {
            customSaveData.SetField("Modifier", currentModifier);
        }
        else
        {
            customSaveData.AddField("Modifier", currentModifier);
        }
        saveJson(customSaveData);
        saving = false;
    }

    public override void WorldEvent_WorldLoaded()
    {
        JSONObject customSaveData = getSaveJson();
        if (customSaveData.HasField("Modifier"))
        {
            float newValue = 0;
            customSaveData.GetField(ref newValue ,"Modifier");
            currentModifier = newValue;
        } else
        {
            currentModifier = 1;
            Debug.Log("StatModifier save data not found for this save. Defaulting to 1");
        }
    }

    public void OnModUnload()
    {
        HarmonyLib.Traverse fetcher = HarmonyLib.Traverse.Create(typeof(GameModeValueManager)).Field("gameModeValues");
        SO_GameModeValue[] data = fetcher.GetValue<SO_GameModeValue[]>();
        for (int i = 0; i < data.Length; i++)
        {
            data[i].nourishmentVariables.foodDecrementRateMultiplier = oldValues[i, 0];
            data[i].nourishmentVariables.thirstDecrementRateMultiplier = oldValues[i, 1];
        }
        Debug.Log("Mod StatModifier has been unloaded!");
    }

    private JSONObject getSaveJson()
    {
        JSONObject data;
        try
        {
            data = new JSONObject(System.IO.File.ReadAllText(fileName));
        } catch
        {
            data = JSONObject.Create();
            saveJson(data);
        }
        return data;
    }

    private void saveJson(JSONObject data)
    {
        try
        {
            System.IO.File.WriteAllText(fileName, data.ToString());
        } catch (System.Exception err)
        {
            Debug.Log("An error occured while trying to save StatModifier settings: " + err.Message);
        }
    }
}