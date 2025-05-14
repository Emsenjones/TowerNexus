using System;
using UnityEngine.UI;
using UnityEngine;

public class HealthBarBehaviour : MonoBehaviour
{
    Slider slider;
    RectTransform rectTransform;
    Canvas mainCanvas;
    MonsterBehaviour boundedMonster;
    [SerializeField]
    Vector2 screenOffset;
    Camera mainCamera;
    
    public void Initialize(MonsterBehaviour pairedMonster, Camera passedCamera)
    {
        boundedMonster = pairedMonster;
        slider = GetComponent<Slider>();
        this.mainCamera = passedCamera;
        rectTransform = GetComponent<RectTransform>();
        mainCanvas = GetComponentInParent<Canvas>();
        #region To registrate event actions.
        if (boundedMonster != null)
            boundedMonster.OnIsDamaged += SetValue;
        else
            Debug.LogError($" is missing MonsterBehaviour!");
        MonsterBehaviour.OnDead += DestroyItself;
        MonsterBehaviour.OnArrived += DestroyItself;
        #endregion
        SetValue();
    }
    void SetValue()
    {
        if (boundedMonster == null)
            Debug.LogError($" is missing _pairedMonster!");
        else if (slider == null)
            Debug.LogError($" is missing _slider!");
        else
            slider.value = boundedMonster.HealthRatio;
    }
    void DestroyItself(MonsterBehaviour destroyedMonster)
    {
        if (boundedMonster == null)
            Debug.LogError($"{gameObject.name} is missing _pairedMonster!");
        else if (boundedMonster == destroyedMonster)
            DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject);
    }
    void DestroyItself(MonsterBehaviour destroyedMonster, bool isByRole)
    {
        DestroyItself(destroyedMonster);
    }
    void OnDisable()
    {
        boundedMonster.OnIsDamaged -= SetValue;
        MonsterBehaviour.OnDead -= DestroyItself;
    }
    private void LateUpdate()
    {
        if(mainCamera == null)
            Debug.LogError($"{gameObject.name} is missing _camera!");
        else if (boundedMonster ==null)
            Debug.LogError($"{gameObject.name} is missing _pairedMonster!");
        else if (rectTransform == null)
            Debug.LogError($"{gameObject.name} is missing _rectTransform!");
        else if (mainCanvas == null)
            Debug.LogError($"{gameObject.name} is missing _mainCanvas!");
        else
        {
            Vector2 screenPosition = mainCamera.WorldToScreenPoint(boundedMonster.transform.position) + (Vector3)screenOffset;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainCanvas.transform as RectTransform,
                screenPosition,
                mainCanvas.worldCamera,
                out Vector2 localPosition
            );
            rectTransform.anchoredPosition = localPosition;
        }
    }
}
