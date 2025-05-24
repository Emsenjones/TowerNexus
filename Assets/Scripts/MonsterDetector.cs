using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class MonsterDetector : MonoBehaviour
{
    List<MonsterBehaviour> monsterList = new List<MonsterBehaviour>();
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
        {
            Debug.LogError($"{gameObject.name} is missing trigger!");
            return;
        }
        if (trigger.enabled) trigger.enabled = false;
        visibleArea.SetVisible(trigger, false);
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
            visibleArea.SetVisible(trigger, false);
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
    [Serializable] class VisibleArea
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite circleSprite;
        [SerializeField] Sprite squareSprite;
        [SerializeField] Color color = Color.white;
        /// <summary>
        /// Set the visible area of the MonsterDetector.
        /// </summary>
        /// <param name="trigger2d">The trigger is to detect monsters.</param>
        /// <param name="isVisible">is visible?</param>
        public void SetVisible(Collider2D trigger2d, bool isVisible)
        {
            if (spriteRenderer == null)
            {
                Debug.LogError($"{GetType().Name} is missing a SpriteRenderer!");
            }
            else if (trigger2d is CircleCollider2D circle)
            {
                spriteRenderer.sprite = circleSprite;

                float diameter = circle.radius * 2f;
                Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
                spriteRenderer.transform.localScale = new Vector3(diameter / spriteSize.x,
                    diameter / spriteSize.y, 1f);
                spriteRenderer.transform.localPosition = circle.offset;
            }
            else if (trigger2d is BoxCollider2D box)
            {
                spriteRenderer.sprite = squareSprite;

                Vector2 colliderSize = box.size;
                Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
                spriteRenderer.transform.localScale = new Vector3(colliderSize.x / spriteSize.x,
                    colliderSize.y / spriteSize.y, 1f);
                spriteRenderer.transform.localPosition = box.offset;
            }

            spriteRenderer.color = color;
            //spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f); // The color is white with transparent.
            spriteRenderer.enabled = isVisible;
        }
    }
    [SerializeField] VisibleArea visibleArea;
    public void SetVisible(bool isVisible)
    {
        if (trigger == null)
            Debug.LogError($"{gameObject.name} is missing trigger!");
        else
            visibleArea.SetVisible(trigger, isVisible);
    }
    // void OnDrawGizmos()
    // {
    //     if (trigger == null) return;
    //
    //     Gizmos.color = Color.yellow;
    //
    //     if (trigger is CircleCollider2D circle)
    //     {
    //         Gizmos.DrawWireSphere(circle.transform.position + (Vector3)circle.offset, circle.radius);
    //     }
    //     else if (trigger is BoxCollider2D box)
    //     {
    //         Gizmos.matrix = box.transform.localToWorldMatrix;
    //         Gizmos.DrawWireCube(box.offset, box.size);
    //     }
    //     else if (trigger is CapsuleCollider2D capsule)
    //     {
    //         // Unity 没有内置 Gizmo 绘制 Capsule2D，你可以近似为椭圆 + Box
    //         Gizmos.DrawWireCube(capsule.transform.position + (Vector3)capsule.offset, capsule.size);
    //     }
    //     else if (trigger is PolygonCollider2D polygon)
    //     {
    //         Gizmos.matrix = polygon.transform.localToWorldMatrix;
    //         for (int i = 0; i < polygon.pathCount; i++)
    //         {
    //             var path = polygon.GetPath(i);
    //             for (int j = 0; j < path.Length; j++)
    //             {
    //                 Vector2 current = path[j];
    //                 Vector2 next = path[(j + 1) % path.Length];
    //                 Gizmos.DrawLine(current, next);
    //             }
    //         }
    //     }
    // }

}
