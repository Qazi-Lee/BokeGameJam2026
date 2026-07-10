using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextTipUI : BaseMonoManager<TextTipUI>
{
    public GameObject main;
    public List<GameObject> texts;
    private Coroutine showTextCoroutine;

    protected override void Awake()
    {
        base.Awake();
        HideAll();
    }

    public void ShowText(List<int> indexList,float time)
    {
        if (indexList == null || indexList.Count == 0)
        {
            HideAll();
            return;
        }

        StopShowTextCoroutine();
        showTextCoroutine = StartCoroutine(ShowTextSequence(indexList, time));
    }

    public void ShowText(int index,float time)
    {
        ShowText(new List<int> { index }, time);
    }

    public void HideText()
    {
        StopShowTextCoroutine();
        HideAll();
    }

    private IEnumerator ShowTextSequence(List<int> indexList, float time)
    {
        if (main != null)
        {
            main.SetActive(true);
        }

        float clampedTime = Mathf.Max(0f, time);

        for (int i = 0; i < indexList.Count; i++)
        {
            if (!TryShowText(indexList[i]))
            {
                continue;
            }

            if (clampedTime > 0f)
            {
                yield return new WaitForSeconds(clampedTime);
            }
            else
            {
                yield return null;
            }
        }

        HideAll();
        showTextCoroutine = null;
    }

    private bool TryShowText(int index)
    {
        if (texts == null || index < 0 || index >= texts.Count || texts[index] == null)
        {
            return false;
        }

        SetAllTextsActive(false);
        texts[index].SetActive(true);
        return true;
    }

    private void HideAll()
    {
        SetAllTextsActive(false);

        if (main != null)
        {
            main.SetActive(false);
        }
    }

    private void SetAllTextsActive(bool isActive)
    {
        if (texts == null)
        {
            return;
        }

        for (int i = 0; i < texts.Count; i++)
        {
            if (texts[i] != null)
            {
                texts[i].SetActive(isActive);
            }
        }
    }

    private void StopShowTextCoroutine()
    {
        if (showTextCoroutine == null)
        {
            return;
        }

        StopCoroutine(showTextCoroutine);
        showTextCoroutine = null;
    }
}
