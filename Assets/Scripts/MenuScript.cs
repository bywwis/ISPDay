using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    // Ссылки на кнопки
    public Button continueButton;
    public Button startGameButton;
    public Button handbookButton;
    public Button exitButton;

    [SerializeField]
    private GameObject GuideWindow;

    void Start()
    {
        // Если нет сохраненной игры, кнопка "Продолжить" будет неактивна
        continueButton.interactable = CheckForSavedGame();
    }

    // Метод для проверки наличия сохраненной игры
    private bool CheckForSavedGame()
    {
        return false; 
    }

    // Метод для кнопки "Продолжить"
    public void ContinueGame()
    {
        // Загружаем сцену с сохраненной игрой
        SceneManager.LoadScene("SavedGameScene"); // Замените на имя сцены
    }

    // Метод для кнопки "Начать игру"
    public void StartGame()
    {
        // Загружаем сцену с новой игрой
        SceneManager.LoadScene("level1"); // Замените на имя вашей сцены
    }

    // Метод для кнопки "Справочник"
    public void OpenHandbook()
    {
        GuideWindow.SetActive(true);
    }

    // Метод для кнопки "Выход"
    public void ExitGame()
    {
        Application.Quit();

        Debug.Log("Игра закрыта");
    }
}