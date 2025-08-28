using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using XRMultiplayer;

/// <summary>
/// Networked Projectile Launcher.
/// </summary>
public class NetworkProjectileLauncher : NetworkBehaviour
{
    [SerializeField]
    [Tooltip("The point that the project is created")]
    Transform m_StartPoint = null;

    // [SerializeField]
    // [Tooltip("The projectile that's created")]
    // GameObject m_ProjectilePrefab = null;

    [SerializeField]
    [Tooltip("The speed at which the projectile is launched")]
    float m_LaunchSpeed = 1000f;

    [SerializeField]
    [Tooltip("The speed at which the projectile is launched")]
    int m_MaxProjectilesAllowed = 15;
    readonly List<Projectile> m_ProjectileQueue = new();

    [Header("Audio")]
    [SerializeField] AudioSource m_AudioSource;
    [SerializeField] AudioClip m_AudioClip;

    /// <summary>
    /// Networked Color. This value gets set when ownership is gained.
    /// </summary>
    readonly NetworkVariable<Color> m_ProjectileColor = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Backup color to use for the local player if ownership has not been established when firing the launcher.
    /// </summary>
    /// <remarks>
    /// This will only be used if the player picks up the launcher and fires immediately.
    /// This Color is not synchronized over the network and will result in inconsistency between players when used.
    /// </remarks>
    Color m_BackupColor;

    PoolerProjectiles m_ProjectilePooler;

    void Awake()
    {
        m_ProjectilePooler = FindFirstObjectByType<PoolerProjectiles>();
    }
    /// <inheritdoc/>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            m_ProjectileColor.Value = XRINetworkGameManager.LocalPlayerColor.Value;
        }
    }

    /// <summary>
    /// Synchronize the firing of the projectile.
    /// </summary>
    /// <param name="activate"></param>
    public void FireLauncher(bool activate)
    {
        if (activate)
        {
            Color fireColor = m_BackupColor;
            if (m_ProjectileColor.Value != default)
            {
                fireColor = m_ProjectileColor.Value;
            }

            GameObject newObject = m_ProjectilePooler.GetItem();
            if (!newObject.TryGetComponent(out Projectile projectile))
            {
                Utils.Log("Projectile component not found on projectile object.", 1);
                return;
            }

            projectile.transform.SetPositionAndRotation(m_StartPoint.position, m_StartPoint.rotation);
            projectile.Setup(IsOwner, fireColor, OnProjectileDestroy);
            m_AudioSource.PlayOneShot(m_AudioClip);

            if (newObject.TryGetComponent(out Rigidbody rigidBody))
            {
                rigidBody.isKinematic = true;
                rigidBody.isKinematic = false;
                Vector3 force = m_StartPoint.forward * m_LaunchSpeed;
                rigidBody.AddForce(force);
            }

            m_ProjectileQueue.Add(projectile);
            if (m_ProjectileQueue.Count > m_MaxProjectilesAllowed)
            {
                m_ProjectileQueue[0].ResetProjectile();
            }
        }
    }

    void OnProjectileDestroy(Projectile projectile)
    {
        if (m_ProjectileQueue.Contains(projectile))
        {
            m_ProjectileQueue.Remove(projectile);
        }
        m_ProjectilePooler.ReturnItem(projectile.gameObject);
    }

    /// <inheritdoc/>
    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        if (IsOwner)
        {
            m_ProjectileColor.Value = XRINetworkGameManager.LocalPlayerColor.Value;
        }
    }
}
