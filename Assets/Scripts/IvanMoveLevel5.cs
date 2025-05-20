using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IvanMoveLevel5 : ConditionalMovementController
{
    [SerializeField] protected GameObject DialogeWindowFinal;

    protected override int GetIvanInitialCheckpointIndex() => 90;
    protected override int GetIvanTargetCheckpointIndex() => 65;
    protected override int GetPaulinaInitialCheckpointIndex() => 24;
    protected override int GetPaulinaTargetCheckpointIndex() => 75;

    protected override void Start()
    {
        storyMessages = new string[] {
            "Иван и Паулина встретились после пар, чтобы обсудить план работы над их командным проектом. Паулина начала рассказывать Ивану, что самые гениальные решения ей приходили рядом с суккулентами.",
            "Помоги им дойти до свободных мест в коворкинге."
        };

        goodEndMessages = new string[] {
            "Иван достал свой ноутбук, и они начали работать над задачей для командного проекта.",
            "После нескольких часов работы Иван и Паулина закончили задачу и почувствовали гордость за свои усилия. ",
            "Уставший, но довольный, Иван закрыл ноутбук и решил, что пора немного отдохнуть и порадовать себя чем-то приятным — прогулкой на свежем воздухе.",
            "Ты прошел этот день вместе с Иваном, и это было только начало! Твои навыки и умения помогли ему преодолеть все трудности. "
        };

        base.Start();
        ShowStoryDialog();
    }

    protected override void CheckLevelCompletion()
    {
        if (currentIvanCheckPoint == checkPoints[GetIvanTargetCheckpointIndex()] &&
            currentPaulinaCheckPoint == checkPoints[GetPaulinaTargetCheckpointIndex()])
        {
            ShowCompletionDialog();
        }
        else
        {
            ShowBadEndDialog($"Иван и Паулина там сидеть не могут! Попробуй ещё раз!");
        }
    }

    protected override void ShowFinalDialog() { CreateWindow(DialogeWindowFinal,$"Не останавливайся на достигнутом и продолжай развиваться! Впереди еще много интересных дней, и Иван будет рад снова видеть тебя в своей истории!"); }
}