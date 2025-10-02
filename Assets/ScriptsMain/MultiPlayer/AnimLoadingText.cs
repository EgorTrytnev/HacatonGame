using UnityEngine;
using TMPro;

public class AnimLoadingText : MonoBehaviour
{
    [SerializeField] private float timeChange = 0.5f; // интервал для добавления точки
    private TextMeshProUGUI text;
    [SerializeField] private int countPoint = 3;
    private int curCountPoint = 0;

    private float timer = 0f;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        text.text = "Loading";
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= timeChange)
        {
            timer = 0f;
            curCountPoint++;
            if (curCountPoint > countPoint)
            {
                curCountPoint = 0;
                text.text = "Loading";
            }
            else
            {
                text.text += ".";
            }
        }
    }
}
