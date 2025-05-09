using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;

public class MapController : MonoBehaviour
{
    [Serializable]private class GridInfo
    {
        [SerializeField]
        private int index;
        public int Index => index;

        [SerializeField]
        Sprite sprite;
        public Sprite Sprite
        {
            get { return sprite; }
        }

        [SerializeField] string description;
    }
    [InfoBox("The road sprite and its relevant index.")]
    [SerializeField] List<GridInfo> GridInfoList;

    Sprite GetGridSprite(int index)
    {
        var match = GridInfoList.FirstOrDefault(item => item.Index == index);
        if (match != null)
        {
            return match.Sprite;
        }

        Debug.LogWarning($"No sprite found for road index {index}");
        return null;
    }

    void UpdateNodeList()
    {
        float gridSpacing = GetGridSpacing();
        if (gridSpacing <= 0f)
        {
            Debug.LogError("Grid spacing is invalid.");
            return;
        }
        var nodeList = GetComponentsInChildren<NodeBehaviour>().ToList();
        if (!nodeList.Any())
        {
            Debug.LogError("Cannot find any NodeBehaviour in " + gameObject.name+" .");
            return;
        }

        foreach (var node in nodeList)
        {
            var spriteRenderer = node.Transform.GetComponent<SpriteRenderer>();
            if (!spriteRenderer)
            {
                Debug.LogWarning($"No SpriteRenderer found on Node at {node.gameObject.name} .");
                continue;
            }
            if (!node.IsWalkable)
            {
                spriteRenderer.sprite = null;
                continue;
            }

            Vector3 pos = node.Transform.position;
            int code = 0;
            foreach (var neighbor in nodeList)
            {
                if (!neighbor.IsWalkable || neighbor == node)
                    continue;

                Vector3 dir = neighbor.Transform.position - pos;

                // 排除非正交方向（如斜对角）
                if (Mathf.Abs(dir.x) > 0.1f && Mathf.Abs(dir.y) > 0.1f)
                    continue;

                // 判断上（y+）
                if (Mathf.Abs(dir.x) < 0.1f && Mathf.Abs(dir.y - gridSpacing) < 0.1f)
                {
                    code += 1;
                }
                // 判断下（y-）
                else if (Mathf.Abs(dir.x) < 0.1f && Mathf.Abs(dir.y + gridSpacing) < 0.1f)
                {
                    code += 2;
                }
                // 判断左（x-）
                else if (Mathf.Abs(dir.y) < 0.1f && Mathf.Abs(dir.x + gridSpacing) < 0.1f)
                {
                    code += 4;
                }
                // 判断右（x+）
                else if (Mathf.Abs(dir.y) < 0.1f && Mathf.Abs(dir.x - gridSpacing) < 0.1f)
                {
                    code += 8;
                }
            }

            spriteRenderer.sprite = GetGridSprite(code);
        }
    }
    float GetGridSpacing()
    {
        var nodeList = GetComponentsInChildren<NodeBehaviour>().ToList();
        if (nodeList.Count < 2)
            return 0f;

        // 找到两个不同位置的节点
        for (int i = 0; i < nodeList.Count - 1; i++)
        {
            for (int j = i + 1; j < nodeList.Count; j++)
            {
                float dist = Vector3.Distance(nodeList[i].Transform.position, nodeList[j].Transform.position);
                if (dist > 0.01f)
                {
                    return Mathf.Round(dist * 100f) / 100f; // 保留两位小数
                }
            }
        }

        return 0f;
    } //计算nodeList中node之间的间距。

    void Start()
    {
        UpdateNodeList();
    }
}


