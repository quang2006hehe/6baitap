using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public GameObject cardPrefab;  // Kéo cái Card Prefab vào đây
    public Transform gamePanel;    // Kéo cái GamePanel vào đây
    public Sprite[] frontSprites;  // Kéo 8 ảnh mặt trước vào đây

    private Card firstCard, secondCard;
    private bool isChecking = false;
    private int pairsFound = 0;

    void Start()
    {
        GenerateCards();
    }

    void GenerateCards()
    {
        // 1. Tạo danh sách 16 ID (8 cặp từ 0 đến 7)
        List<int> cardIDs = new List<int>();
        for (int i = 0; i < 8; i++)
        {
            cardIDs.Add(i);
            cardIDs.Add(i);
        }

        // 2. Trộn ngẫu nhiên danh sách ID (Shuffle)
        for (int i = 0; i < cardIDs.Count; i++)
        {
            int temp = cardIDs[i];
            int randomIndex = Random.Range(i, cardIDs.Count);
            cardIDs[i] = cardIDs[randomIndex];
            cardIDs[randomIndex] = temp;
        }

        // 3. Sinh ra 16 thẻ bài trong GamePanel
        foreach (int id in cardIDs)
        {
            GameObject newObj = Instantiate(cardPrefab, gamePanel);
            Card cardScript = newObj.GetComponent<Card>();
            cardScript.id = id;
            cardScript.frontImage = frontSprites[id];
        }
    }

    public void CardClicked(Card clickedCard)
    {
        if (isChecking || clickedCard == firstCard) return;

        if (firstCard == null)
        {
            firstCard = clickedCard;
        }
        else
        {
            secondCard = clickedCard;
            StartCoroutine(CheckForMatch());
        }
    }

    IEnumerator CheckForMatch()
    {
        isChecking = true;
        yield return new WaitForSeconds(0.6f); // Đợi một chút để người chơi kịp nhìn

        if (firstCard.id == secondCard.id)
        {
            // Nếu đúng cặp
            firstCard.SetMatched();
            secondCard.SetMatched();
            pairsFound++;
            if (pairsFound == 8) Debug.Log("BẠN THẮNG RỒI!");
        }
        else
        {
            // Nếu sai cặp -> Úp lại
            firstCard.Close();
            secondCard.Close();
        }

        firstCard = null;
        secondCard = null;
        isChecking = false;
    }
}