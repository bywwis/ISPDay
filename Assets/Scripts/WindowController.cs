using UnityEngine;
using UnityEngine.SceneManagement;

public class WindowController : MonoBehaviour
{
    // Перезагрузка уровня
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Загрузка следующего уровня
    public void LoadNextScene()
    {
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextLevelIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextLevelIndex);
        }
    }

    // Возврат в главное менюe
    public void BackToMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    //Закрытие окна
    public void CloseWindow()
    {
        Destroy(gameObject); 
    }
}
