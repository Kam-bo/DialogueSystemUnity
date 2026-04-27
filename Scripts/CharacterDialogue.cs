using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public interface ISpeaker
{
    DialogueCharacterConfig Config { get; }
    Speaker SpeakerID{ get; } // only getter
    void ShowDialogue(DialogueData dialogueLine);
    void HideDialogue();
    void HandleSkipButton();
}

public class CharacterDialogue : MonoBehaviour, ISpeaker
{
    public DialogueCharacterConfig Config => _config;
    public Speaker SpeakerID => _config.Speaker;
    
    [Header("General")]
    [SerializeField] DialogueCharacterConfig _config;
    
    [Header("Dialogue Box")]
    [SerializeField] Image _imageTextBox;
    [SerializeField] RectTransform _transformTextBox;
    [SerializeField] TextMeshPro _textMeshPro;
    [SerializeField] RectTransform _rotatableTextBox;
    [SerializeField] Renderer _renderer;

    float _textBoxWidthOffset => _config.textBoxWidthOffset; 
    float _textBoxHeightOffset => _config.textBoxHeightOffset;
    
    [Header("Animation")]
    [SerializeField] Animator _circleAnimator;
    
    Camera _camera;
    
    DialogueData _dialogueData;
    
    string _currentDialogue;        // current dialogue even if not fully finished
    string _dialogueLineText;       // current full dialogue
    
    Coroutine _dialogueCoroutine, _circleCoroutine;
    
    bool _subtitlesVisible = false; // just for checking visibility edge for subtitles
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _camera = Camera.main;
        GameEvents.OnCharacterDialogueRegister?.Invoke(this);
    }
 
    private void OnDisable() =>
        GameEvents.OnCharacterDialogueUnregister?.Invoke(this);

    public void ShowDialogue(DialogueData data)
    {
        _dialogueData = data;
        if (_dialogueCoroutine != null) StopCoroutine(_dialogueCoroutine);
        
        _currentDialogue = "";
        _subtitlesVisible = false; 
        _dialogueCoroutine = StartCoroutine(ShowText());
    }

    public void HideDialogue()
    {
        if (_dialogueCoroutine != null) StopCoroutine(_dialogueCoroutine);
        if (_circleCoroutine != null) StopCoroutine(_circleCoroutine);

        _textMeshPro.text = "";
        _imageTextBox.enabled = false;
        _circleAnimator.gameObject.SetActive(false);

        if (_subtitlesVisible)
        {
            GameEvents.OnHideSubtitles?.Invoke();
            _subtitlesVisible = false;
        }
    }
    
    public void HandleSkipButton()
    {
        if (_dialogueData.DialogueOptions == DialogueOptions.KeepTalking) // on keepTalking it should not be possible to skip dialogue
            return;
        
        if (_dialogueCoroutine != null) // if text is still showing, show it all
        {
            StopCoroutine(_dialogueCoroutine);
            _dialogueCoroutine = null;
            _currentDialogue = _dialogueLineText;
            _textMeshPro.text = _currentDialogue;
            if (!ShowSubtitles())
                ResizeUIToText();
            ShowIndicator();
        }
        else // if text is already finished then send signal
        {
            if (_circleCoroutine != null) 
                StopCoroutine(_circleCoroutine);
            _circleAnimator.gameObject.SetActive(false);
            GameEvents.OnCharacterDialogueEnded(this);
        }
    }

    private IEnumerator ShowText()
    {
        _dialogueLineText = _dialogueData.Text;
        for (int i = 0; i < _dialogueLineText.Length; i++)
        {
            _currentDialogue += _dialogueLineText[i];
            _textMeshPro.text = _currentDialogue;
            if (!ShowSubtitles())
                ResizeUIToText();
            PlaySound(_currentDialogue[i], _dialogueData.Vocal);
            yield return new WaitForSeconds(_dialogueData.Speed); // wait for another letter of the text 
        }
        Sounds.TextSound.Stop();
        _dialogueCoroutine = null;
        
        if (_dialogueData.DialogueOptions is DialogueOptions.Normal or DialogueOptions.EndDialogue)
            ShowIndicator(); // onCharacterDialogueEnded is called inside this method after animation ended
        else if (_dialogueData.DialogueOptions is DialogueOptions.KeepTalking)
            GameEvents.OnCharacterDialogueEnded(this);
    }
    
    private void PlaySound(char word, Vocalization vocal) // NOTE: sound system placeholder -> to be extracted to AudioManager
    {
        float pitch = PickPitchByVocalization(vocal);
        if (!Sounds.TextSound.Playing && word!=' ')
            Sounds.TextSound.SetRandomPitch(new Vector2(pitch - 0.1f, pitch + 0.1f))
                     .Play();
    }

    private float PickPitchByVocalization(Vocalization vocal) => vocal switch // NOTE: sound system placeholder -> to be extracted to AudioManager
    {
        Vocalization.Normal => 1f,
        Vocalization.Pitch  => 1.5f,
        Vocalization.Deep   => 0.2f,
        _ => throw new ArgumentOutOfRangeException(nameof(vocal))
    };

    void ResizeUIToText()
    {
        _transformTextBox.sizeDelta = 
            new Vector2(_textMeshPro.preferredWidth+_textBoxWidthOffset, _textMeshPro.preferredHeight+_textBoxHeightOffset);
    }
    
    private void ShowIndicator()
    {
        if (_dialogueData.DialogueOptions is not (DialogueOptions.Normal or DialogueOptions.EndDialogue))
            return;
        
        _circleAnimator.gameObject.SetActive(true);
        _circleAnimator.Play("SliderAnimation", 0, 0f);
        _circleCoroutine = StartCoroutine(WaitForCircle());
    }
    
    private IEnumerator WaitForCircle()
    {
        while (_circleAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f || _circleAnimator.IsInTransition(0))
        {
            ShowSubtitles();
            yield return null;
        }
        _circleAnimator.gameObject.SetActive(false);
        GameEvents.OnCharacterDialogueEnded(this);
    }
    
    // Method to decide which dialogue should be visible - text above character or subtitles
    bool ShowSubtitles()
    {
        if (IsCurrentlyVisible())
        {
            _textMeshPro.enabled = true;
            _imageTextBox.enabled = true;
            RotateText();
            if (_subtitlesVisible) // subtitles were showing - hide them now
            {
                GameEvents.OnHideSubtitles?.Invoke();
                _subtitlesVisible = false;
            }
            return false;
        }
        else
        {
            _textMeshPro.enabled = false;
            _imageTextBox.enabled = false;
            if (!_subtitlesVisible)  // subtitles weren't showing - display them
            {
                GameEvents.OnDisplaySubtitles?.Invoke();
                _subtitlesVisible = true;
            }
            else // character was already not visible
            {
                GameEvents.OnUpdateSubtitles?.Invoke(_currentDialogue);
            }
            return true;
        }
    }

    bool IsCurrentlyVisible()
    {
        // if object is visible viewpos.x/y should be in range of 0..1
        // viewpos.z is the distance between object and camera
        // if viewpos.z < 0 then object is behind camera 
        // viewpos.z should be avaible in settings to change from which distance should the dialogue be visible
        
        Vector3 pointToCheck = _renderer.bounds.center;
        Vector3 viewPos = _camera.WorldToViewportPoint(pointToCheck);
        
        bool isVisible = viewPos.x is < 1 and > 0 && 
                         viewPos.y is < 1 and > 0 &&
                         viewPos.z is < 3 and > 0;
        
        return isVisible;
    }

    void RotateText()
    {
        Vector3 direction = _rotatableTextBox.position - _camera.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion baseRotation = Quaternion.LookRotation(direction);
            _rotatableTextBox.rotation = baseRotation;
        }
    }
    
}
