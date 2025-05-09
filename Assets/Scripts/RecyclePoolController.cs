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
    

    [InfoBox("Maximum poolCount")]
    [SerializeField] int maxPoolCount = 100;
    [ShowInInspector, PropertyRange(0, 1)]
    [SerializeField][InfoBox("This is how many items will be deleted when poolCount is out of range.")]
    float onceRemovedCountPercentage = 0.5f;
    
    List<RecycleItemBehaviour> _recycleItemBehaviourList;

    public void Initialize()
    {
        _recycleItemBehaviourList = new List<RecycleItemBehaviour>();
    }

    public GameObject GenerateOneObject(GameObject sourcePrefab, Transform parentTransform = null)
    {
        GameObject returnedObject = null;
        foreach (var recycleBehaviour in _recycleItemBehaviourList)
        {
            if (recycleBehaviour.IsFromTheSameSource(sourcePrefab))
            {
                returnedObject = recycleBehaviour.gameObject;
                _recycleItemBehaviourList.Remove(recycleBehaviour);
                returnedObject.SetActive(true);
                returnedObject.transform.SetParent(parentTransform);
                return returnedObject;
            }
        }
        
        returnedObject = Instantiate(sourcePrefab, parentTransform);
        returnedObject.AddComponent<RecycleItemBehaviour>().Initialize(sourcePrefab);
        return returnedObject;
    }

    public void RecycleOneObject(GameObject recycledObject)
    {
        var recycledObjectBehaviour = recycledObject.GetComponent<RecycleItemBehaviour>();
        if (!recycledObjectBehaviour) return;
        
        _recycleItemBehaviourList.Add(recycledObjectBehaviour);
        recycledObjectBehaviour.transform.SetParent(transform);
        recycledObject.SetActive(false);

        #region To limite _recycleItemBehaviourList.Count.
        int removedCount = Mathf.RoundToInt(_recycleItemBehaviourList.Count * onceRemovedCountPercentage);
        if (_recycleItemBehaviourList.Count > maxPoolCount)
        {
            for (var i = 0; i <= removedCount; i++)
            {
                var willDestroyedBehaviour = _recycleItemBehaviourList[0];
                _recycleItemBehaviourList.Remove(willDestroyedBehaviour);
                Destroy(willDestroyedBehaviour.gameObject);
            }
        }

        #endregion

    }

}
