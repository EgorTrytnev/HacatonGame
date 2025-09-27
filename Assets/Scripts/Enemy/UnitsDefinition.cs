using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitsDefinition : MonoBehaviour
{
    [SerializeField] private string nameUnit;

    public string GetUnitName()
    {
        return nameUnit;
    }
}
