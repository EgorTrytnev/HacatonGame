using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PhotonView))]
public class HeatPointsController : MonoBehaviourPun
{
    [SerializeField] private int HP = 10;
    [SerializeField] private float healthTime = 10f;
    [SerializeField] private Slider slider;

    private int curHP;
    private float curHealthTime;
    private bool isDead;

    void Start()
    {
        curHP = HP;
        curHealthTime = healthTime;
        if (slider) { slider.maxValue = HP; slider.value = HP; }
    }

    void Update()
    {
        if (isDead) return;

        if (photonView.IsMine)
        {
            curHealthTime -= Time.deltaTime;
            if (curHP < HP && curHealthTime <= 0f)
            {
                ApplyHealLocal(1);
                // сообщить другим о новом значении
                photonView.RPC(nameof(RPC_SetHp), RpcTarget.Others, curHP);
                curHealthTime = healthTime;
            }
        }

        if (slider) slider.value = curHP;
    }

    void ApplyHealLocal(int amount)
    {
        curHP = Mathf.Min(HP, curHP + amount);
    }

    // Вызывается владельцу цели
    [PunRPC]
    public void RPC_TakeDamage(int damage, PhotonMessageInfo info)
    {
        if (!photonView.IsMine) return;
        if (isDead) return;

        curHP -= damage;
        // разослать новое HP другим
        photonView.RPC(nameof(RPC_SetHp), RpcTarget.Others, curHP);

        if (curHP <= 0)
        {
            isDead = true;
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    void RPC_SetHp(int newHp)
    {
        // выполняется у НЕ владельцев, но можно и у всех
        curHP = newHp;
        if (slider) slider.value = curHP;
    }
}
