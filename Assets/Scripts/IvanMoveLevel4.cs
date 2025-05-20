using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class IvanMoveLevel4 : CycleMovementController
{
    protected override int GetInitialCheckpointIndex() => 18;
    protected override int GetTargetCheckpointIndex() => 70;
    protected override string GetWrongItemTag() => "wrongChair";
    protected override int GetRequiredItemsCount() => 1;
    protected override int GetMaxStepsWithoutCycle() => 10;
    protected override int GetMaxStepsWithCycle() => 26;

    protected override void Start()
    {
        storyMessages = new string[] {
            "Любимое занятие Ивана! Наконец-то! Он очень любит решать алгоритмические задачи и писать код.",
            "Оглянувшись вокруг, Иван замечает, что все стулья заняты. Нужно где-то найти ещё один стул! Преподаватель сказал, что он может взять его в лаборантской."
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
            ShowCompletionDialog($"Спустя 1,5 часа работы Иван сделал и защитил лабораторную работу! После успешной сдачи лабы Иван решил работать над проектом. Это можно сделать в коворкинге.");
        }
        else
        {
            ShowBadEndDialog($"Ты что-то перепутал. Попробуй еще раз!");
        }
    }
}