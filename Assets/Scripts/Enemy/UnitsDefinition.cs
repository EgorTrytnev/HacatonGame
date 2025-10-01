using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitsDefinition : MonoBehaviour
{
    [SerializeField] private string nameUnit;
    [SerializeField]private CollorTeam collorTeam;
    public string GetUnitName()
    {
        return nameUnit;
    }
    public void SetTeam(CollorTeam team)
    {
        collorTeam = team;
    }
    public CollorTeam GetTeam()
    {
        return collorTeam;
    }
}
