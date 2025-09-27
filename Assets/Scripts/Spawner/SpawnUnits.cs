using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnUnits : MonoBehaviour
{

    [SerializeField] private GameObject[] allMobs;
    public GameObject SpawnUnit(Transform pos, string Name = "default")
    {
        foreach (GameObject unit in allMobs)
        {
            UnitsDefinition unitName = unit.GetComponent<UnitsDefinition>();
            if (unitName.GetUnitName().ToLower() == Name.ToLower())
            {
                Debug.Log("In Spawner");
                GameObject retUnit = Instantiate(unit, pos.position, Quaternion.identity);
                return retUnit;
            }
        }
        return null;
    }
}
