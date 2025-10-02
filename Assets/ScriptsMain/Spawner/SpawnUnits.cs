using System;
using UnityEngine;
using Photon.Pun;

public class SpawnUnits : MonoBehaviour
{
    [SerializeField] private GameObject[] allMobs; // ссылки на префабы в Resources

    public GameObject SpawnUnit(Transform pos, string name = "default", object[] instantiationData = null)
    {
        foreach (GameObject unit in allMobs)
        {
            var unitDef = unit.GetComponent<UnitsDefinition>();
            if (unitDef != null && string.Equals(unitDef.GetUnitName(), name, StringComparison.OrdinalIgnoreCase))
            {
                // Важно: unit.name должен совпадать с именем префаба в Resources
                return PhotonNetwork.Instantiate(unit.name, pos.position, Quaternion.identity, 0, instantiationData);
            }
        }
        Debug.LogWarning($"SpawnUnits: префаб для {name} не найден");
        return null;
    }
}
