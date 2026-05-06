using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveService
{
    const string SaveFolderName = "Saves";
    const string SaveExtension = ".json";
    public const string AutoSaveFileName = "autosave.json";

    public string SaveFolderPath => Path.Combine(Application.persistentDataPath, SaveFolderName);

    public List<SaveSlotInfo> GetSlots()
    {
        EnsureSaveFolder();

        List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
        string[] files = Directory.GetFiles(SaveFolderPath, "*" + SaveExtension);
        for (int i = 0; i < files.Length; i++)
        {
            if (string.Equals(Path.GetFileName(files[i]), AutoSaveFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            SaveData data = LoadFromPath(files[i]);
            if (data == null) continue;

            slots.Add(new SaveSlotInfo
            {
                slotId = data.slotId,
                filePath = files[i],
                displayName = string.IsNullOrEmpty(data.displayName)
                    ? Path.GetFileNameWithoutExtension(files[i])
                    : data.displayName,
                savedAtUtc = data.savedAtUtc,
                sceneName = data.sceneName,
                levelIndex = data.levelIndex,
                money = data.money,
            });
        }

        slots.Sort((a, b) => string.CompareOrdinal(b.savedAtUtc, a.savedAtUtc));
        return slots;
    }

    public void Save(string slotId, SaveData data)
    {
        data.slotId = slotId;
        if (string.IsNullOrEmpty(data.displayName))
            data.displayName = slotId;

        SaveToPath(GetSlotPath(slotId), data);
    }

    public SaveData Load(string slotId)
    {
        string path = GetSlotPath(slotId);
        return File.Exists(path) ? LoadFromPath(path) : null;
    }

    public string SaveFile(string fileName, SaveData data)
    {
        string path = GetSaveFilePath(fileName);
        SaveToPath(path, data);
        return path;
    }

    public SaveData LoadFile(string filePath)
    {
        return File.Exists(filePath) ? LoadFromPath(filePath) : null;
    }

    public void Delete(string slotId)
    {
        string path = GetSlotPath(slotId);
        if (File.Exists(path))
            File.Delete(path);
    }

    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public void SaveAutoSave(SaveData data)
    {
        data.slotId = "autosave";
        if (string.IsNullOrEmpty(data.displayName))
            data.displayName = "Auto-save";

        SaveToPath(GetAutoSavePath(), data);
    }

    public SaveData LoadAutoSave()
    {
        string path = GetAutoSavePath();
        return File.Exists(path) ? LoadFromPath(path) : null;
    }

    public void DeleteAutoSave()
    {
        string path = GetAutoSavePath();
        if (File.Exists(path))
            File.Delete(path);
    }

    string GetAutoSavePath()
    {
        return Path.Combine(SaveFolderPath, AutoSaveFileName);
    }

    string GetSlotPath(string slotId)
    {
        return Path.Combine(SaveFolderPath, $"slot_{SanitizeSlotId(slotId)}{SaveExtension}");
    }

    string GetSaveFilePath(string fileName)
    {
        string safeName = SanitizeSlotId(Path.GetFileNameWithoutExtension(fileName));
        return Path.Combine(SaveFolderPath, safeName + SaveExtension);
    }

    void SaveToPath(string path, SaveData data)
    {
        EnsureSaveFolder();

        data.savedAtUtc = DateTime.UtcNow.ToString("O");
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    void EnsureSaveFolder()
    {
        if (!Directory.Exists(SaveFolderPath))
            Directory.CreateDirectory(SaveFolderPath);
    }

    SaveData LoadFromPath(string path)
    {
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load save file {path}: {exception.Message}");
            return null;
        }
    }

    string SanitizeSlotId(string slotId)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            slotId = slotId.Replace(invalidChar, '_');

        return string.IsNullOrWhiteSpace(slotId) ? "default" : slotId;
    }
}
