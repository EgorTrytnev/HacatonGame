using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimText : MonoBehaviour
{
    [SerializeField] private GameObject text;
    private Animator textAnim;

    void Start()
    {
        textAnim = text.GetComponent<Animator>();
    }
    public void changeAlphaText()
    {
        textAnim.SetTrigger("ChangeT");
    }


}
