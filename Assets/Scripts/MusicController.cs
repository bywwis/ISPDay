using UnityEngine;

public class MusicController : MonoBehaviour
{
    private static MusicController instance;

    void Awake()
    {
        // Проверяем существование экземпляра
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Не уничтожать при загрузке новой сцены
        }
        else
        {
            // Если музыкальный менеджер уже существует
            Destroy(gameObject);
        }
    }
}