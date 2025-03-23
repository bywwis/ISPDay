using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveLoadManager
{
    private static string savePath = Application.persistentDataPath + "/progress.json";

    // Сохранение прогресса
    public static void SaveProgress(string levelName)
    {
        GameProgress progress = LoadProgress() ?? new GameProgress();

        // Добавляем уровень в список пройденных, если его там еще нет
        if (!progress.completedLevels.Contains(levelName))
        {
            progress.completedLevels.Add(levelName);
        }

        // Сохраняем в JSON
        string json = JsonUtility.ToJson(progress);
        File.WriteAllText(savePath, json);

        Debug.Log("Progress saved: " + json);
    }

    // Загрузка прогресса
    public static GameProgress LoadProgress()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            return JsonUtility.FromJson<GameProgress>(json);
        }
        return null;
    }

    // Проверка, пройден ли уровень
    public static bool IsLevelCompleted(string levelName)
    {
        GameProgress progress = LoadProgress();
        return progress != null && progress.completedLevels.Contains(levelName);
    }

    public static void ResetProgress()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Прогресс сброшен: файл сохранения удален.");
        }
        else
        {
            Debug.Log("Файл сохранения не найден, сбрасывать нечего.");
        }
    }
}