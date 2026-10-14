using System.Collections.Generic;
using UnityEngine;

namespace Levers;

public static class MeshSetter
{
    public static List<MeshData> meshesToReplace = [];

    public static void Replace()
    {
        foreach (var dat in meshesToReplace)
        {
            dat.Replace();
        }
    }
}

public class MeshData
{
    public GameObject prefab;
    public bool copyMaterials;
    public string sourcePrefab;
    public string childPath;

    public MeshData(GameObject prefab, string sourcePrefab, string childPath)
    {
        this.prefab = prefab;
        this.sourcePrefab = sourcePrefab;
        this.childPath = childPath;
        MeshSetter.meshesToReplace.Add(this);
    }

    public void Replace()
    {
        if (!prefab.TryGetComponent(out MeshFilter meshFilter))
        {
            Debug.LogWarning(prefab.name + " does not have mesh filter");
            return;
        }
        
        if (!PrefabTools.TryGetPrefab(sourcePrefab, out var source))
        {
            Debug.LogWarning("Failed to find " + sourcePrefab);
            return;
        }
        var child = source.transform.Find(childPath);
        if (child == null)
        {
            Debug.LogWarning("Failed to find child: " + childPath);
            return;
        }
        if (!child.TryGetComponent(out MeshFilter filter))
        {
            Debug.LogWarning(child.name + " does not have mesh filter");
            return;
        }
        meshFilter.mesh = filter.mesh;

        if (prefab.TryGetComponent(out MeshCollider collider))
        {
            collider.sharedMesh = filter.mesh;
        }

        if (copyMaterials && 
            prefab.TryGetComponent(out MeshRenderer meshRenderer) && 
            child.TryGetComponent(out MeshRenderer renderer))
        {
            meshRenderer.sharedMaterials = renderer.sharedMaterials;
        }
    }
}