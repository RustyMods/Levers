using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Levers;
#nullable disable
public class DoorExtension : MonoBehaviour
{
    [HarmonyPatch(typeof(Door), nameof(Door.Awake))]
    private static class Door_Awake_Patch
    {
        private static void Postfix(Door __instance)
        {
            if (!__instance.GetComponent<Piece>()) return;
            __instance.gameObject.AddComponent<DoorExtension>();
        }
    }

    public static List<DoorExtension> instances = [];
    public Door m_door;
    public Piece m_piece;
    public Lever m_connectedLever;
    
    public void Start()
    {
        m_door = GetComponent<Door>();
        m_piece = GetComponent<Piece>();
        if (m_door == null || m_piece == null) return;
        instances.Add(this);
    }

    public void OnDestroy()
    {
        instances.Remove(this);
        if (m_connectedLever != null)
        {
            m_connectedLever.BreakConnection();
        }
    }

    public static bool TryGetNearestDoor(Vector3 pos, float range, out DoorExtension door)
    {
        door = null;
        float nearest = float.MaxValue;
        for (int i = 0; i < instances.Count; ++i)
        {
            var ext =  instances[i];
            if (!ext.m_piece.IsPlacedByPlayer()) continue;
            if (ext.m_connectedLever != null) continue;
            var dist = Vector3.Distance(pos, ext.transform.position);
            if (dist < nearest)
            {
                door = ext;
                nearest = dist;
            }
        }

        return door != null;
    }
}