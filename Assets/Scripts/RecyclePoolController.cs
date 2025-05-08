using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class RecyclePoolController : MonoBehaviour
{
    private class RecycleItemBehaviour : MonoBehaviour
    {
        GameObject _sourcePrefab;

        public void Initialize(GameObject sourcePrefab)
        {
            _sourcePrefab = sourcePrefab;
        }

        public bool IsFromTheSameSource(GameObject comparedPrefab)
        {
            return _sourcePrefab == comparedPrefab;
        }
    }
    
    Transform _sceneTransform;
    
    RectTransform _uiTransform;

    [InfoBox("Maximum poolCount")]
    [SerializeField] int maxPoolCount = 100;
    [ShowInInspector, PropertyRange(0, "maxPoolCount")]
    [SerializeField][InfoBox("This is how many items will be deleted when poolCount is out of range.")]
    int onceRemovedCount;
    
    
    List<RecycleItemBehaviour> _recycleItemBehaviourList;

    public void Initialize(RectTransform uiTransform)
    {
        _recycleItemBehaviourList = new List<RecycleItemBehaviour>();
        _uiTransform = uiTransform;
        _sceneTransform = transform;
    }

    public GameObject GenerateOneObject(GameObject sourcePrefab, bool isUi)
    {
        GameObject returnedObject = null;
        foreach (var recycleBehaviour in _recycleItemBehaviourList)
        {
            if (recycleBehaviour.IsFromTheSameSource(sourcePrefab))
            {
                returnedObject = recycleBehaviour.gameObject;
                _recycleItemBehaviourList.Remove(recycleBehaviour);
                returnedObject.SetActive(true);
                return returnedObject;
            }
        }
        
        returnedObject = Instantiate(sourcePrefab, isUi ? _uiTransform : _sceneTransform);
        returnedObject.AddComponent<RecycleItemBehaviour>().Initialize(sourcePrefab);
        OrganizeItself();
        return returnedObject;
    }

    public void RecycleOneObject(GameObject recycledObject)
    {
        var recycledObjectBehaviour = recycledObject.GetComponent<RecycleItemBehaviour>();
        if (recycledObjectBehaviour)
        {
            _recycleItemBehaviourList.Add(recycledObjectBehaviour);
            OrganizeItself();
        }
        recycledObject.SetActive(false);
        
    }

    void OrganizeItself()
    {
        if (_recycleItemBehaviourList.Count <= maxPoolCount) return;

        for (var i = 0; i < onceRemovedCount; i++)
        {
            var willDestroyedBehaviour = _recycleItemBehaviourList[0];
            _recycleItemBehaviourList.Remove(willDestroyedBehaviour);
            Destroy(willDestroyedBehaviour.gameObject);
        }
    }


}
