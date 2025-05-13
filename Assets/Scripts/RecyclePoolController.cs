using System;
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
    List<RecycleItemBehaviour> _willBeDestroyedBehaviourList;

    public void Initialize()
    {
        _recycleItemBehaviourList = new List<RecycleItemBehaviour>();
        _willBeDestroyedBehaviourList = new List<RecycleItemBehaviour>();
    }
    /// <summary>
    /// Generate a gameObject via sourcePrefab.
    /// </summary>
    /// <param name="sourcePrefab">The source prefab.</param>
    /// <param name="parentTransform">The parent transform.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Temporary disable targetObject. 
    /// </summary>
    /// <param name="targetObject">The object to disable.</param>
    public void RecycleOneObject(GameObject targetObject)
    {
        var recycledObjectBehaviour = targetObject.GetComponent<RecycleItemBehaviour>();
        if (recycledObjectBehaviour == null) return;

        _recycleItemBehaviourList.Add(recycledObjectBehaviour);
        recycledObjectBehaviour.transform.SetParent(transform);
        targetObject.SetActive(false);

        #region To limite _recycleItemBehaviourList.Count.

        int removedCount = Mathf.RoundToInt(_recycleItemBehaviourList.Count * onceRemovedCountPercentage);
        if (_recycleItemBehaviourList.Count > maxPoolCount)
        {
            for (var i = 0; i <= removedCount; i++)
            {
                var willDestroyedBehaviour = _recycleItemBehaviourList[0];
                _recycleItemBehaviourList.Remove(willDestroyedBehaviour);
                _willBeDestroyedBehaviourList.Add(willDestroyedBehaviour);
            }
        }

        #endregion
        
    }
    void LateUpdate()
    {
        if (_willBeDestroyedBehaviourList.Count > 0)
        {
            foreach (RecycleItemBehaviour willBeDestroyedBehaviour in _willBeDestroyedBehaviourList)
                Destroy(willBeDestroyedBehaviour.gameObject);
            
            _willBeDestroyedBehaviourList.Clear();
        }
    }

}
