using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    public event Action<ResourceData, int> OnResourceChanged;

    [System.Serializable]
    private class ResourceEntry
    {
        public ResourceData data;
        public int amount;
    }

    [SerializeField]
    private List<ResourceEntry> resources = new List<ResourceEntry>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ResetAllResources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ResetAllResources()
    {
        foreach (var r in resources)
            r.amount = 0;
    }

    public int GetResource(ResourceData data)
    {
        var entry = resources.Find(r => r.data == data);
        return entry != null ? entry.amount : 0;
    }

    public void AddResource(ResourceData data, int amount)
    {
        if (data == null)
            return;

        var entry = resources.Find(r => r.data == data);
        if (entry != null)
        {
            entry.amount += amount;
        }
        else
        {
            resources.Add(new ResourceEntry
            {
                data = data,
                amount = amount
            });
        }

        OnResourceChanged?.Invoke(data, entry.amount);
    }

    public bool SpendResource(ResourceData data, int amount)
    {
        var entry = resources.Find(r => r.data == data);
        if (entry != null && entry.amount >= amount)
        {
            entry.amount -= amount;
            OnResourceChanged?.Invoke(data, entry.amount);
            return true;
        }
        return false;
    }
}