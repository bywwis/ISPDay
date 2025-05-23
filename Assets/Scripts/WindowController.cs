using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WindowController : MonoBehaviour
{
    private BaseMovementController movementController;
    public AudioSource clickSound;

    private void Awake()
    {
        movementController = FindObjectOfType<BaseMovementController>();
    }

    // Для кнопки "Далее" в окнах истории/победы
    public void OnNextButton()
    {
        clickSound.Play();
        Invoke(nameof(ActionNextButton), clickSound.clip.length);
    }

    // Перезагрузка уровня
    public void RestartLevel()
    {
        clickSound.Play();
        Invoke(nameof(LevelScenLoad), clickSound.clip.length);
    }

    // Возврат в главное менюe
    public void BackToMenu()
    {
        clickSound.Play();
        Invoke(nameof(CloseLevel), clickSound.clip.length);
    }

    //Закрытие окна
    public void CloseWindow()
    {
        clickSound.Play();
        Invoke(nameof(DestroyWindow), clickSound.clip.length);
    }

    private void DestroyWindow()
    {
        Destroy(gameObject);
    }

    private void LevelScenLoad()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void CloseLevel()
    {
        SceneManager.LoadScene("Menu");
    }

    private void ActionNextButton()
    {
        if (movementController != null)
        {
            movementController.AdvanceStory();
        }
    }
}
