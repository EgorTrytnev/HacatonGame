using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private RectTransform infoText;
    [SerializeField] private float speedDumpCam = 0.3f;
    [SerializeField] private float speedDumpText = 0.3f;
    [SerializeField] private Vector3 offsetText;

    Vector3 curVelCam = Vector3.zero;
    Vector3 curVelText = Vector3.zero;

    void Start()
    {
        infoText = GameObject.Find("InfoMobSpawn").GetComponent<RectTransform>();
    }


    void Update()
    {
        Vector3 targetPosition = new Vector3(player.position.x, player.position.y, transform.position.z);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(player.position);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref curVelCam, speedDumpCam);
        Vector3 targetPersText = screenPos + offsetText;
        infoText.position = Vector3.SmoothDamp(infoText.position, targetPersText, ref curVelText, speedDumpText);
    }

    public void SetPlayer(Transform player)
    {
        this.player = player;
    }
}
