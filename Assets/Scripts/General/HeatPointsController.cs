using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeatPointsController : MonoBehaviour
{
    [SerializeField] private int HP = 10;
    private int curHP;
    [SerializeField] private float healthTime = 10;
    [SerializeField] private Slider slider;
    private float curHealthTime;
    private bool isDead = false;


    void Start()
    {
        curHP = HP;
        curHealthTime = healthTime;
        slider.maxValue = HP;
    }

    void Update()
    {
        KeepTrackHP();
        checkActive();
    }

    private void KeepTrackHP()
    {
        curHealthTime -= Time.deltaTime;
        if (curHealthTime <= 0)
        {
            if (curHP != HP)
            {
                HealthHP(1);
                curHealthTime = healthTime;
            }
        }
    }

    public bool TakeDamage(int damage)
    {
        curHP -= damage;
        slider.value += damage;
        return CheckHP();

        
    }
    public void HealthHP(int hp)
    {
        curHP += hp;
        slider.value -= hp;
    }

    private bool CheckHP()
    {
        if (curHP <= 0)
        {
            Debug.Log("Death: " + gameObject.name);
            gameObject.SetActive(false);
            return true;
        }
        return false;
    }

    private void checkActive()
    {
        if (!gameObject.activeSelf)
        {
            Destroy(gameObject);
        }
    }

    public bool GetIsDead()
    {
        return isDead;
    }
}
