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


    void Start()
    {
        curHP = HP;
        curHealthTime = healthTime;
        slider.maxValue = HP;
    }

    void Update()
    {
        KeepTrackHP();

        CheckHP();
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

    public void TakeDamage(int damage)
    {
        curHP -= damage;
        slider.value += damage;
    }
    public void HealthHP(int hp)
    {
        curHP += hp;
        slider.value -= hp;
    }

    private void CheckHP()
    {
        if (curHP <= 0)
        {
            Debug.Log("Death: " + gameObject.name);
            Destroy(gameObject);
        }
    }
}
