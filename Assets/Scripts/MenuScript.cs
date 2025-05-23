using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    public Button continueButton;
    public AudioSource clickSound;
    public GameObject guidePage;

    void Start()
    {
        // Проверяем, есть ли сохраненный прогресс
        continueButton.interactable = SaveLoadManager.LoadProgress() != null;
    }

    // Метод для кнопки "Продолжить"
    public void ContinueGame()
    {
        clickSound.Play();
        Invoke(nameof(LoadContinue), clickSound.clip.length);
    }

    // Метод для кнопки "Начать игру"
    public void StartGame()
    {
        clickSound.Play();
        Invoke(nameof(LoadNew), clickSound.clip.length);
    }

    // Метод для кнопки "Выход"
    public void ExitGame()
    {
        clickSound.Play();
        Invoke(nameof(QuitGame), clickSound.clip.length);
    }

    // Метод для кнопки "Гайд"
    public void OpenGuideClick()
    {
        clickSound.Play();
        Invoke(nameof(OpenGuide), clickSound.clip.length);
    }

    // Метод для кнопки "Отзыв"
    public void RequestClick() 
    {
        clickSound.Play();
        Invoke(nameof(OpenRequest), clickSound.clip.length);
    }

    // Метод для кнопки "Уровень-Лабиринт"
    public void OpenMazeClick()
    {
        clickSound.Play();
        Invoke(nameof(OpenMaze), clickSound.clip.length);
    }

    private void LoadContinue()
    {
        GameProgress progress = SaveLoadManager.LoadProgress();
        if (progress != null)
        {
            // Загружаем последний пройденный уровень
            string lastCompletedLevel = progress.completedLevels[progress.completedLevels.Count - 1];
            int nextLevelIndex = SceneUtility.GetBuildIndexByScenePath(lastCompletedLevel) + 1;

            if (nextLevelIndex < SceneManager.sceneCountInBuildSettings - 1)
            {
                SceneManager.LoadScene(nextLevelIndex);
            }
            else
            {
                SceneManager.LoadScene(lastCompletedLevel);
            }
        }
    }

    private void LoadNew()
    {
        SaveLoadManager.ResetProgress(); // Сбрасываем прогресс
        SceneManager.LoadScene("level1"); // Загружаем первый уровень
    }

    private void QuitGame()
    {
        Application.Quit();
        Debug.Log("Игра закрыта");
    }

    private void OpenGuide()
    {
        gameObject.SetActive(false);
        guidePage.gameObject.SetActive(true);
    }

    private void OpenRequest()
    {
        Application.OpenURL("https://forms.yandex.ru/u/681b1ebaeb6146d7ee95ddfd/");
    }

    public void OpenMaze()
    {
        SceneManager.LoadScene("MazeLevel");
    }
}

