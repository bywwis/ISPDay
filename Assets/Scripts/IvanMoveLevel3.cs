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
            "Помоги Ивану выбрать 3 блюда и расставить их на поднос в правильном порядке."
        };

        base.Start();
        ShowStoryDialog();
    }


    protected override void CheckLevelCompletion()
    {
        bool isAtTarget = targetCheckPoint != null &&
                         Vector3.Distance(playerTransform.position, targetCheckPoint.position) < 0.1f;

        if (isAtTarget && !hasWrongItem && collectedItemsCount >= GetRequiredItemsCount())
        {
            ShowCompletionDialog($"Иван очень вкусно покушал, а главное у него снова не случилось аллергии. Это успех! После плотного обеда, пора трудиться дальше! Практическое занятие не ждёт!");
        }
        else
        {
            ShowBadEndDialog($"Кажется, здесь что-то не так, попробуй выбрать другие позиции из меню, имей в виду, что необходимо собрать 3 блюда и оплатить.");
        }
    }

}