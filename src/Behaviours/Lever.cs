using System.Collections;
using System.Text;
using UnityEngine;

namespace Levers;
#nullable disable

public class Lever : MonoBehaviour, Hoverable, Interactable
{
    public ZNetView m_nview;
    public Animator m_animator;
    public string m_name = "$piece_lever";
    public GameObject m_connectionPrefab;
    public GameObject m_connectionInstance;
    public Vector3 m_connectionOffset = Vector3.zero;
    public float m_maxDoorDistance = 25f;
    public float m_hoverOffset = 2f;
    public EffectList m_pullEffects = new();
    public Door m_connectedDoor = null;

    public float m_effectDelayMin = 0f;
    public float m_effectDelayMax = 0.5f;
    public bool m_inUse;
    public void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        m_animator = GetComponentInChildren<Animator>();
        if (m_nview == null || m_nview.GetZDO() == null) return;
        m_nview.Register(nameof(RPC_AnimateLever), RPC_AnimateLever);
        m_nview.Register(nameof(RPC_AnimateLeverReturn), RPC_AnimateLeverReturn);
    }

    public void FindConnection()
    {
        if (m_connectedDoor != null) return;
        
        if (DoorExtension.TryGetNearestDoor(transform.position, m_maxDoorDistance, out var ext))
        {
            m_connectedDoor = ext.m_door;
            ext.m_connectedLever = this;
        }
    }

    public void StartConnectionEffect(Door door, float timeout = 1f)
    {
        if (m_connectionInstance != null) return;
        var pos = door.transform.position;
        m_connectionInstance = Instantiate(m_connectionPrefab, transform.position, Quaternion.identity);
        var dir = (pos - transform.position).normalized;
        Quaternion rot = Quaternion.LookRotation(dir);
        m_connectionInstance.transform.rotation = rot;
        m_connectionInstance.transform.localScale = new Vector3(1f, 1f, dir.magnitude);
        CancelInvoke(nameof(StopConnectionEffect));
        Invoke(nameof(StopConnectionEffect), timeout);

    }

    public void StopConnectionEffect()
    {
        if (m_connectionInstance == null) return;
        Destroy(m_connectionInstance);
        m_connectionInstance = null;
    }

    public void PokeEffect(float timeout = 1f)
    {
        if (m_connectedDoor == null)
        {
            StopConnectionEffect();
        }
        else
        {
            StartConnectionEffect(m_connectedDoor, timeout);
        }
    }

    public void BreakConnection()
    {
        StopConnectionEffect();
        m_connectedDoor = null;
    }
    
    public string GetHoverText()
    {
        if (!m_nview.IsValid()) return "";
        FindConnection();
        PokeEffect(5f);
        StringBuilder sb = new StringBuilder();
        sb.Append(m_name);
        
        if (m_connectedDoor != null)
        {
            var currentState = m_connectedDoor.m_nview.GetZDO().GetInt(ZDOVars.s_state);
            bool isOpen = currentState != 0;
            sb.Append( $" ( {m_connectedDoor.m_name} )");
            sb.Append("\n" + "[<color=yellow><b>$KEY_Use</b></color>] " + 
                      (isOpen ? "$piece_door_close" : "$piece_door_open"));
        }
        else
        {
            sb.Append("( Not Connected )");
        }

        return Localization.instance.Localize(sb.ToString());
    }

    public string GetHoverName() => m_name;

    public float GetHoverOffset() => m_hoverOffset;
    
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (m_inUse) return false;
        
        if (m_connectedDoor == null) return false;
        if (!m_connectedDoor.CanInteract()) return false;
        
        if (m_connectedDoor.m_checkGuardStone && !PrivateArea.CheckAccess(m_connectedDoor.transform.position)) return true;
        if (m_connectedDoor.m_keyItem != null && !m_connectedDoor.HaveKey(user)) return false;
        
        PullLever(user);
        Invoke(nameof(ReturnLever), Random.Range(m_effectDelayMin, m_effectDelayMax)); ;
        
        return true;
    }


    public void PullLever(Humanoid user)
    {
        m_inUse = true;
        m_nview.InvokeRPC(ZNetView.Everybody, nameof(RPC_AnimateLever));
        m_pullEffects.Create(transform.position, transform.rotation);
        var dir = (user.transform.position - m_connectedDoor.transform.position).normalized;
        m_connectedDoor.Open(dir);
    }

    public void ReturnLever()
    {
        m_inUse = false;
        m_nview.InvokeRPC(ZNetView.Everybody, nameof(RPC_AnimateLeverReturn));
    }

    public void RPC_AnimateLever(long sender)
    {
        m_inUse = true;
        m_animator.SetBool("Pulled", true);
    }

    public void RPC_AnimateLeverReturn(long sender)
    {
        m_animator.SetBool("Pulled", false);
        m_inUse = false;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}