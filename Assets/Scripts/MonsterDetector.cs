using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class MonsterDetector : MonoBehaviour
{
    List<MonsterBehaviour> monsterList = new List<MonsterBehaviour>();
    [ShowInInspector, ReadOnly]
    public List<MonsterBehaviour> MonsterList
    {
        get {
            monsterList.RemoveAll(m => m is null || m.Health <= 0);
            return monsterList;
        }
    }

    Collider2D trigger;
    void Awake()
    {
        trigger = GetComponent<Collider2D>();
        if (trigger == null)
            Debug.LogError($"{gameObject.name} is missing trigger!");
        else if (trigger.enabled)
            trigger.enabled = false;
    }

    public void Initialize()
    {
        monsterList = new List<MonsterBehaviour>();
        if (trigger == null)
            Debug.LogError($"{gameObject.name} is missing trigger!");
        else
        {
            trigger.enabled = true;
            trigger.isTrigger = true;
        }

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var monster = other.GetComponent<MonsterBehaviour>();
        if (monster != null && !monsterList.Contains(monster))
            monsterList.Add(monster);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var monsterBehaviour = other.GetComponent<MonsterBehaviour>();
        if (monsterBehaviour != null && monsterList.Contains(monsterBehaviour))
            monsterList.Remove(monsterBehaviour);
    }
    public MonsterBehaviour GetTheNearestMonster()
    {
        if (MonsterList.Count <= 0) return null;

        MonsterBehaviour nearestMonster = null;
        float minDistance = float.MaxValue;

        foreach (MonsterBehaviour monster in MonsterList)
        {
            float dist = Vector3.Distance(monster.transform.position, transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestMonster = monster;
            }
        }

        return nearestMonster;
    }
    void OnDrawGizmos()
    {
        if (trigger == null) return;

        Gizmos.color = Color.yellow;

        if (trigger is CircleCollider2D circle)
        {
            Gizmos.DrawWireSphere(circle.transform.position + (Vector3)circle.offset, circle.radius);
        }
        else if (trigger is BoxCollider2D box)
        {
            Gizmos.matrix = box.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
        }
        else if (trigger is CapsuleCollider2D capsule)
        {
            // Unity 没有内置 Gizmo 绘制 Capsule2D，你可以近似为椭圆 + Box
            Gizmos.DrawWireCube(capsule.transform.position + (Vector3)capsule.offset, capsule.size);
        }
        else if (trigger is PolygonCollider2D polygon)
        {
            Gizmos.matrix = polygon.transform.localToWorldMatrix;
            for (int i = 0; i < polygon.pathCount; i++)
            {
                var path = polygon.GetPath(i);
                for (int j = 0; j < path.Length; j++)
                {
                    Vector2 current = path[j];
                    Vector2 next = path[(j + 1) % path.Length];
                    Gizmos.DrawLine(current, next);
                }
            }
        }
    }

}
