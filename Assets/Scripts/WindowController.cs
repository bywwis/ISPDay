using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WindowController : MonoBehaviour
{
    private BaseMovementController movementController;

    private void Awake()
    {
        movementController = FindObjectOfType<BaseMovementController>();
    }

    // Для кнопки "Далее" в окнах истории/победы
    public void OnNextButton()
    {
        if (movementController != null)
        {
            movementController.AdvanceStory();
        }
    }

    // Перезагрузка уровня
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
