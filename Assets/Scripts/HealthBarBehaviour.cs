using System;
using UnityEngine.UI;
using UnityEngine;

public class HealthBarBehaviour : MonoBehaviour
{
    Slider _slider;
    RectTransform _rectTransform;
    Canvas _mainCanvas;
    MonsterBehaviour _boundedMonster;
    [SerializeField]
    Vector2 screenOffset;
    Camera _camera;
    
    public void Initialize(MonsterBehaviour pairedMonster, Camera mainCamera)
    {
        _boundedMonster = pairedMonster;
        _slider = GetComponent<Slider>();
        _camera = mainCamera;
        _rectTransform = GetComponent<RectTransform>();
        _mainCanvas = GetComponentInParent<Canvas>();
        #region To registrate event actions.
        if (_boundedMonster != null)
            _boundedMonster.OnIsDamaged += SetValue;
        else
            Debug.LogError($" is missing MonsterBehaviour!");
        MonsterBehaviour.OnDead += DestroyItself;
        #endregion
        SetValue();
    }
    void SetValue()
    {
        if (_boundedMonster == null)
            Debug.LogError($" is missing _pairedMonster!");
        else if (_slider == null)
            Debug.LogError($" is missing _slider!");
        else
            _slider.value = _boundedMonster.HealthRatio;
    }
    void DestroyItself(MonsterBehaviour destroyedMonster)
    {
        if (_boundedMonster == null)
            Debug.LogError($"{gameObject.name} is missing _pairedMonster!");
        else if (_boundedMonster == destroyedMonster)
            DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject);
    }
    void OnDisable()
    {
        _boundedMonster.OnIsDamaged -= SetValue;
        MonsterBehaviour.OnDead -= DestroyItself;
    }
    private void LateUpdate()
    {
        if(_camera == null)
            Debug.LogError($"{gameObject.name} is missing _camera!");
        else if (_boundedMonster ==null)
            Debug.LogError($"{gameObject.name} is missing _pairedMonster!");
        else if (_rectTransform == null)
            Debug.LogError($"{gameObject.name} is missing _rectTransform!");
        else if (_mainCanvas == null)
            Debug.LogError($"{gameObject.name} is missing _mainCanvas!");
        else
        {
            Vector2 screenPosition = _camera.WorldToScreenPoint(_boundedMonster.transform.position) + (Vector3)screenOffset;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mainCanvas.transform as RectTransform,
                screenPosition,
                _mainCanvas.worldCamera,
                out Vector2 localPosition
            );
            _rectTransform.anchoredPosition = localPosition;
        }
    }
}
