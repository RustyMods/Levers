using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PieceManager;
using UnityEngine;

namespace Levers;
#nullable disable

public static class PrefabTools
{
    public static ZNetScene _scene;
    public static GameObject _root;
    public static Transform root => _root.transform;

    public static List<GameObject> _prefabsToRegister = [];
    
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    private static class FejdStartup_Awake_Prefix
    {
        public static void Prefix(FejdStartup __instance)
        {
            _scene = __instance.m_objectDBPrefab.GetComponent<ZNetScene>();
            _root = new GameObject("root");
            Object.DontDestroyOnLoad(_root);
            _root.SetActive(false);

            if (TryGetPrefab("vfx_lever", out var vfx_lever))
            {
                var fx_lever = Object.Instantiate(vfx_lever, root, false);
                fx_lever.name = "fx_lever_rs";
                Object.Destroy(fx_lever.transform.Find("sfx_distant thunder").gameObject);
                Object.Destroy(fx_lever.transform.Find("sfx_sparks").gameObject);
                Object.Destroy(fx_lever.transform.Find("Sparks").gameObject);
                _prefabsToRegister.Add(fx_lever);

                LeversPlugin.piece_floor_lever.Prefab.GetComponent<Lever>().m_pullEffects.m_effectPrefabs = 
                    [new EffectList.EffectData { m_prefab = fx_lever, }];
                LeversPlugin.piece_wall_lever.Prefab.GetComponent<Lever>().m_pullEffects.m_effectPrefabs = 
                    [new EffectList.EffectData { m_prefab = fx_lever, }];
            }

            if (TryGetPrefab("incinerator", out var incinerator) && incinerator.GetComponentInChildren<Animator>() is
                    { } incinAnim)
            {
                foreach (var b in BuildPiece.registeredPieces)
                {
                    if (b.Prefab.GetComponentInChildren<Animator>() is { } animator)
                    {
                        animator.runtimeAnimatorController = incinAnim.runtimeAnimatorController;
                    }
                }
            }

            
            MeshSetter.Replace();
        }
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    private static class ZNetScene_Awake_Prefix
    {
        private static void Prefix(ZNetScene __instance)
        {
            foreach (var prefab in _prefabsToRegister)
            {
                if (!__instance.m_prefabs.Contains(prefab))
                {
                    __instance.m_prefabs.Add(prefab);
                }
                
            }
        }

        private static void Postfix(ZNetScene __instance)
        {
            foreach (var b in BuildPiece.registeredPieces)
            {
                var prefab = b.Prefab;
                if (prefab.GetComponent<Lever>() is {} lever)
                {
                    lever.m_connectionPrefab = __instance.GetPrefab("piece_workbench_ext3").GetComponent<StationExtension>()
                        .m_connectionPrefab;
                }
            }
            EffectListRef.LoadAll();
        }
    }

    public static bool TryGetPrefab(string id, out GameObject prefab)
    {
        prefab = null;
        
        if (ZNetScene.instance)
        {
            prefab = ZNetScene.instance.GetPrefab(id);
        }
        else if (_scene != null)
        {
            prefab = _scene.m_prefabs.FirstOrDefault(p => p.name == id);
        }

        return prefab != null;
    }
}