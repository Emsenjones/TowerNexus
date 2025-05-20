using System;
using System.Collections.Generic;
using UnityEngine;

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
}
