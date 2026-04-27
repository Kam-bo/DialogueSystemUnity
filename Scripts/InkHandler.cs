using System;
using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;

public class InkHandler : MonoBehaviour
{
    public TextAsset inkAsset;
    
    public Story _inkStory;
    
    private InkLineData dialogueData = new InkLineData();

    private void OnEnable()
    {
        GameEvents.OnNextStoryLine += GetNextStoryLine;
        GameEvents.OnChooseChoice += ChooseChoice;
    }

    private void OnDisable()
    {
        GameEvents.OnNextStoryLine -= GetNextStoryLine;
        GameEvents.OnChooseChoice -= ChooseChoice;
    }

    void Awake()
    {
        _inkStory = new Story(inkAsset.text);
    }
    
    void ResetTags() => GameEvents.OnTagsReset?.Invoke();
    
    void GetNextStoryLine()
    {
        if (_inkStory.canContinue)
        {
            ClearDialogueTags();
            string storyLine = _inkStory.Continue();
            dialogueData.Text = storyLine;
            
            GetTags(_inkStory.currentTags);
            GetChoices();
            
            GameEvents.OnNewDialogueLine?.Invoke(dialogueData);
        }
        else
            Debug.LogError("Possible end of ink file? No more text to display");
    }

    #region Choices
    void GetChoices()
    {
        if (_inkStory.currentChoices.Count<1)
            return;
        
        // check all choices
        List<Choice> choices = _inkStory.currentChoices;
        List<DialogueChoiceData> choiceDataList = new List<DialogueChoiceData>();
        
        // get all choices
        for (int i = 0; i < choices.Count; i++) 
        {
            DialogueChoiceData choiceData = CreateChoiceData(choices[i]);
            if(!choiceData.tags.Contains(ChoiceType.GameplayChoice))
                choiceDataList.Add(choiceData);
        }

        if (choiceDataList.Count > 0)
            GameEvents.OnNewDialogueChoices?.Invoke(choiceDataList); // send them
    }

    private DialogueChoiceData CreateChoiceData(Choice choice)
    {
        if (choice.tags == null || choice.tags.Count == 0)
            return new DialogueChoiceData(choice.text, null);

        List<ChoiceType> choiceTags = new List<ChoiceType>(choice.tags.Count);
        foreach (var inkTag in choice.tags)
        {
            if (TryParseInkTag(inkTag, out var parsedTag))
            {
                ChoiceType choiceTag = HandleChoiceTags(parsedTag);
                choiceTags.Add(choiceTag);
            }
        }
        return new DialogueChoiceData(choice.text, choiceTags);
    }
    
    // This method is used for the ink choices that need player interaction within a game world
    // e.g if player is looking for the correct food in the fridge, everytime player picks something it sends signal
    // to the ink to pick choice[0]
    // Conditions within Ink will handle which dialogue should be played and if this dialogue should be repeated until 
    // correct one will be picked
    public void TryAction()
    {
        if(_inkStory.currentChoices.Count == 1)
        {
            _inkStory.ChooseChoiceIndex(0);
            GetNextStoryLine();
        }
        else
            Debug.LogError("There is more than one or none option to choose from. Please check your ink file.");

    }

    public void ChooseChoice(int choice)
    {
        _inkStory.ChooseChoiceIndex(choice);
        GetNextStoryLine();
    }

    #endregion
    
    #region Tags
    // Text content from the game will appear 'as is' when the engine runs.
    // However, it can sometimes be useful to mark up a line of content with extra information
    // to tell the game what to do with that content.
    // These don't show up in the main text flow, but can be read off by the game and used as you see fit.
    // Tags are read as strings so to keep it synchronized they are parsed as Enums in Unity to easliy use and manipulate them.
    
    void GetTags(List<string> tags)
    {
        if (tags.Count == 0)
            return;

        foreach (var inkTag in tags)
        {
            if (TryParseInkTag(inkTag, out var parsedTag))
                HandleTags(parsedTag);
        }
    }
    
    bool TryParseInkTag(string inkTag, out InkTags result)
    {
        if (Enum.TryParse(inkTag.ToUpper(), out result))
        {
            return true;
        }

        Debug.LogError("Mismatch, tag: " + inkTag);
        return false;
    }

    void ClearDialogueTags() // expect Speaker cuz it will be changed in Dialogue Controller
    {
        dialogueData.SpeakerOverride = null;
        dialogueData.Text = "";
        dialogueData.SpeedOverride = null;
        dialogueData.VocalOverride = null;
        dialogueData.DialogueOptions = DialogueOptions.Normal;
    }

    void HandleTags(InkTags inkTag) 
    {
        switch (inkTag)
        {
            // ==== SPEAKERS ==== 
            case InkTags.NOONE:
                dialogueData.SpeakerOverride = Speaker.None;
                break;
            case InkTags.PLAYER:
                dialogueData.SpeakerOverride = Speaker.Player;
                break;
            case InkTags.FLOWER:
                dialogueData.SpeakerOverride = Speaker.Flower;
                break;
            case InkTags.FLY:
                dialogueData.SpeakerOverride = Speaker.Fly;
                break;
            
            // ==== DIALOGUE OPTIONS ====
            case InkTags.KEEP_TALKING:
                dialogueData.DialogueOptions = DialogueOptions.KeepTalking;
                break;
            case InkTags.PAUSE_DIALOGUE:
                dialogueData.DialogueOptions = DialogueOptions.PauseDialogue;
                break;
            case InkTags.END_DIALOGUE:
                dialogueData.DialogueOptions = DialogueOptions.EndDialogue;
                break;

            default:
                Debug.LogError($"Tag {inkTag} not handled");
                break;
        }
    }
    ChoiceType HandleChoiceTags(InkTags inkTag)
    {
        switch (inkTag)
        {
            // ==== DIALOGUE CHOICE TYPE ====
            case InkTags.DIALOGUE_CHOICE:
                return ChoiceType.DialogueChoice;;
            case InkTags.GAMEPLAY_CHOICE:
                return ChoiceType.GameplayChoice;
            default:
                Debug.LogError($"Choice Tag {inkTag} not handled");
                return ChoiceType.DialogueChoice;
        }

    }

    #endregion
    
    
}
