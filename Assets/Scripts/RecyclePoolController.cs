using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


public class RecyclePoolController : MonoBehaviour
{
    private class RecycleItemBehaviour : MonoBehaviour
    {
        GameObject sourcePrefab;

        public void Initialize(GameObject prefab)
        {
            this.sourcePrefab = prefab;
        }

        public bool IsFromTheSameSource(GameObject comparedPrefab)
        {
            return sourcePrefab == comparedPrefab;
        }
    }
    

    [InfoBox("Maximum poolCount")]
    [SerializeField] int maxPoolCount = 100;
    [ShowInInspector, PropertyRange(0, 1)]
    [SerializeField][InfoBox("This is how many items will be deleted when poolCount is out of range.")]
    float onceRemovedCountPercentage = 0.5f;
    
    List<RecycleItemBehaviour> recycleItemBehaviourList;
    List<RecycleItemBehaviour> willBeDestroyedBehaviourList;
    public void Initialize()
    {
        recycleItemBehaviourList = new List<RecycleItemBehaviour>();
        willBeDestroyedBehaviourList = new List<RecycleItemBehaviour>();
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
        foreach (var recycleBehaviour in recycleItemBehaviourList)
        {
            if (recycleBehaviour.IsFromTheSameSource(sourcePrefab))
            {
                returnedObject = recycleBehaviour.gameObject;
                recycleItemBehaviourList.Remove(recycleBehaviour);
                returnedObject.transform.SetParent(parentTransform);
                returnedObject.SetActive(true);
                return returnedObject;
            }
        }
        
        returnedObject = Instantiate(sourcePrefab, parentTransform);
        returnedObject.AddComponent<RecycleItemBehaviour>().Initialize(sourcePrefab);
        returnedObject.SetActive(true);
        return returnedObject;
    }

    /// <summary>
    /// To disable targetObject. 
    /// </summary>
    /// <param name="targetObject">The object to disable.</param>
    public void RecycleOneObject(GameObject targetObject)
    {
        var recycledObjectBehaviour = targetObject.GetComponent<RecycleItemBehaviour>();
        if (recycledObjectBehaviour == null) return;

        recycleItemBehaviourList.Add(recycledObjectBehaviour);
        recycledObjectBehaviour.transform.SetParent(transform);
        targetObject.SetActive(false);

        #region To limite _recycleItemBehaviourList.Count.

        int removedCount = Mathf.RoundToInt(recycleItemBehaviourList.Count * onceRemovedCountPercentage);
        if (recycleItemBehaviourList.Count > maxPoolCount)
        {
            for (var i = 0; i <= removedCount; i++)
            {
                var willDestroyedBehaviour = recycleItemBehaviourList[0];
                recycleItemBehaviourList.Remove(willDestroyedBehaviour);
                willBeDestroyedBehaviourList.Add(willDestroyedBehaviour);
            }
        }

        #endregion
        
    }
    void LateUpdate()
    {
        if (willBeDestroyedBehaviourList.Count > 0)
        {
            foreach (RecycleItemBehaviour willBeDestroyedBehaviour in willBeDestroyedBehaviourList)
                Destroy(willBeDestroyedBehaviour.gameObject);
            
            willBeDestroyedBehaviourList.Clear();
        }
    }

}
