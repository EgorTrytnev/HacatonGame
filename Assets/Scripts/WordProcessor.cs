using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
public class WordProcessor : MonoBehaviour
{

    [DllImport("libWordGenerator")]
    private static extern void getRandomWord(System.Text.StringBuilder buffer, int bufferSize);

    //тут будет функция инициализации траескрибатора 
    

    private string GetWord()
    {
        System.Text.StringBuilder buffer = new System.Text.StringBuilder(256);
        getRandomWord(buffer, buffer.Capacity);
        return buffer.ToString();
    }
    Queue<string> myWords = new Queue<string>();
    void Start()
    {
        StartCoroutine(WordGen());
        StartCoroutine(WordProcessing());
    }

    IEnumerator WordGen()
    {

        string word = GetWord();
        lock (myWords)
        {
            myWords.Enqueue(word);
            Debug.Log($"Word {word} is added in Queue");
        }

        yield return new WaitForSeconds(1f);
        StartCoroutine(WordGen());
    }
    
    IEnumerator WordProcessing()
    {
        while (true)
        {
            if (myWords.Count > 0)
            {
                string word;
                lock (myWords)
                {
                    word = myWords.Dequeue();
                    Debug.Log($"Word {word} is DELETED in Queue");
                }
                //функция обработки
            }
            
            yield return null;
        }
    }
    
    
}
