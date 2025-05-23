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

    public AudioSource clickSound;
    public GameObject menuPage;

    private Image imageComponent;

    void Start()
    {
        GameObject guideImageObject = GameObject.Find("GuideImage");
        imageComponent = guideImageObject.GetComponent<Image>();

        movingButton.interactable = false;
    }

    public void OpenCycleGuide()
    {
        clickSound.Play();
        imageComponent.sprite = CycleGuide;
        SetButtonsInteractable(cycleButton); 
    }

    public void OpenConditionGuide()
    {
        clickSound.Play();
        imageComponent.sprite = ConditionGuide;
        SetButtonsInteractable(conditionButton);
    }

    public void OpenInteractionGuide()
    {
        clickSound.Play();
        imageComponent.sprite = InteractionGuide;
        SetButtonsInteractable(interactionButton);
    }

    public void OpenMovingGuide()
    {
        clickSound.Play();
        imageComponent.sprite = MovingGuide;
        SetButtonsInteractable(movingButton);
    }

    public void CloseGuideClick()
    {
        clickSound.Play();
        Invoke(nameof(CloseGuide), clickSound.clip.length);
    }

    private void CloseGuide()
    {
        gameObject.SetActive(false);
        menuPage.gameObject.SetActive(true);
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
