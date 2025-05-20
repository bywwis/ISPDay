using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class IvanMoveLevel3 : CycleMovementController
{
    protected override int GetInitialCheckpointIndex() => 9;
    protected override int GetTargetCheckpointIndex() => 31;
    protected override string GetWrongItemTag() => "fish";

    protected override void Start()
    {
        storyMessages = new string[] {
            "Обед! Иван очень любит покушать! В детстве он очень любил уху, которую готовила мама. Но из-за того, что он ел её очень часто у Ивана развилась аллергия.",
            "Помоги Ивану выбрать 3 блюда и оплатить их."
        };

        base.Start();
        ShowStoryDialog();
    }


    protected override void CheckLevelCompletion()
    {
        if (currentCheckPoint == targetCheckPoint && !hasWrongItem && collectedItemsCount == GetRequiredItemsCount())
        {
            ShowCompletionDialog($"Иван очень вкусно покушал, а главное у него снова не случилось аллергии. Это успех! После плотного обеда, пора трудиться дальше! Практическое занятие не ждёт!");
        }
        else if(currentCheckPoint == targetCheckPoint && !hasWrongItem && collectedItemsCount < GetRequiredItemsCount())
        {
            ShowBadEndDialog($"Ой, Иван меньше еды взял, из-за этого он будет сидеть голодным.");
        }
        else if (currentCheckPoint != targetCheckPoint && !hasWrongItem && collectedItemsCount == GetRequiredItemsCount())
        {
            ShowBadEndDialog($"О нет, Иван забыл оплатить обед.");
        }
        else if (currentCheckPoint == targetCheckPoint && hasWrongItem && collectedItemsCount == GetRequiredItemsCount())
        {
            ShowBadEndDialog($"Кажетя одно из блюд было с рыбой. У Ивана случилась аллергия.");
        }
        else
        {
            ShowBadEndDialog($"Кажется, здесь что-то не так, попробуй ещё раз!");
        }
    }

}