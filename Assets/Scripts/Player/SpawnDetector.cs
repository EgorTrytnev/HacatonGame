using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpawnDetector : MonoBehaviour
{
    private bool spawnAllowed = false;

    private AnimText animText;
    private SpawnUnits spawnUnits;
    private List<GameObject> myUnits;

    void Start()
    {
        animText = GetComponent<AnimText>();
        myUnits = new List<GameObject>();
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Spawner"))
        {
            spawnAllowed = true;
            animText.changeAlphaText();
            spawnUnits = collision.gameObject.GetComponent<SpawnUnits>();
        }
    }
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Spawner"))
        {
            spawnAllowed = false;
            spawnUnits = null;
        }
    }

    public bool getSpawnAllowed()
    {
        return spawnAllowed;
    }

    public void SpawnMob(CollorTeam team, string Name = "default")
    {
        
        if (spawnUnits != null)
        {
            Debug.Log("In Detector");
            GameObject unit = spawnUnits.SpawnUnit(transform, Name);
            unit.GetComponent<UnitsDefinition>().SetTeam(team);
            myUnits.Add(unit);
        }
    }

    public void FollowMe(string Name = "default")
    {
        foreach (GameObject unit in myUnits)
        {
            UnitsDefinition unitName = unit.GetComponent<UnitsDefinition>();
            if (unitName.GetUnitName().ToLower() == Name.ToLower())
            {
                unit.GetComponent<EnemyController>().SetTargetPlayer(transform);
            }
        }
    }
    public void StopFollowMe(string Name = "default")
    {
        foreach (GameObject unit in myUnits)
        {
            UnitsDefinition unitName = unit.GetComponent<UnitsDefinition>();
            if (unitName.GetUnitName().ToLower() == Name.ToLower())
            {
                unit.GetComponent<EnemyController>().DeleteTargetPlayer();
            }
        }
    }

    public void AtackEnemy(string Name = "default")
    {
        foreach (GameObject unit in myUnits)
        {
            UnitsDefinition unitName = unit.GetComponent<UnitsDefinition>();
            if (unitName.GetUnitName().ToLower() == Name.ToLower())
            {
                unit.GetComponent<EnemyController>().DeleteTargetPlayer();
                unit.GetComponent<EnemyController>().SetTargetEnemy();
            }
        }
    }
    public int UseTheCommand(string Command = "default", string NameUnit = "default")
    {
        return 0;
    }
}
