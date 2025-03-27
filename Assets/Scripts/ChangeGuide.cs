using UnityEngine;
using UnityEngine.UI;

public class ChangeGuide : MonoBehaviour
{
    public Sprite MovingGuide;
    public Sprite InteractionGuide;
    public Sprite ConditionGuide;
    public Sprite CycleGuide;

    public Button cycleButton;
    public Button conditionButton;
    public Button interactionButton;
    public Button movingButton;

    private Image imageComponent;

    void Start()
    {
        imageComponent = GetComponent<Image>();
        movingButton.interactable = false;
    }

    public void OpenCycleGuide()
    {
        imageComponent.sprite = CycleGuide;
        SetButtonsInteractable(cycleButton); 
    }

    public void OpenConditionGuide()
    {
        imageComponent.sprite = ConditionGuide;
        SetButtonsInteractable(conditionButton);
    }

    public void OpenInteractionGuide()
    {
        imageComponent.sprite = InteractionGuide;
        SetButtonsInteractable(interactionButton);
    }

    public void OpenMovingGuide()
    {
        imageComponent.sprite = MovingGuide;
        SetButtonsInteractable(movingButton);
    }

    private void SetButtonsInteractable(Button activeButton)
    {
        if (activeButton == cycleButton)
        {
            cycleButton.interactable = false;
        }
        else
        {
            cycleButton.interactable = true;
        }
        if (activeButton == conditionButton)
        {
            conditionButton.interactable = false;
        }
        else
        {
            conditionButton.interactable = true;
        }
        if (activeButton == interactionButton)
        {
            interactionButton.interactable = false;
        }
        else
        {
            interactionButton.interactable = true;
        }
        if (activeButton == movingButton)
        {
            movingButton.interactable = false;
        }
        else
        {
            movingButton.interactable = true;
        }
    }
}
