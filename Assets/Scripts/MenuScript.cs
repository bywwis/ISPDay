using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    public Button continueButton;

    void Start()
    {
        // Проверяем, есть ли сохраненный прогресс
        continueButton.interactable = SaveLoadManager.LoadProgress() != null;
    }

    // Метод для кнопки "Продолжить"
    public void ContinueGame()
    {
        GameProgress progress = SaveLoadManager.LoadProgress();
        if (progress != null)
        {
            // Загружаем последний пройденный уровень
            string lastCompletedLevel = progress.completedLevels[progress.completedLevels.Count - 1];
            int nextLevelIndex = SceneUtility.GetBuildIndexByScenePath(lastCompletedLevel) + 1;

            if (nextLevelIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextLevelIndex);
            }
            else
            {
                SceneManager.LoadScene(lastCompletedLevel);
            }
        }
    }

    // Метод для кнопки "Начать игру"
    public void StartGame()
    {
        // Сбрасываем прогресс
        SaveLoadManager.ResetProgress();

        // Загружаем первый уровень
        SceneManager.LoadScene("level1");
    }

    // Метод для кнопки "Выход"
    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Игра закрыта");
    }
}