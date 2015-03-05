using UnityEngine;
using System.Collections;

public class CreditsScript : MonoBehaviour {

    public Transform[] Credits;
    int currentCreditNumber;

	// Use this for initialization
	void Start () {
        currentCreditNumber = 0;
	}

    public void RunCredits()
    {
        StartCoroutine(RotateCredits());
    }

    IEnumerator RotateCredits()
    {
        //rotate previous credit out, and current credit in.
        for (int i = 0; i < 20; i++)
        {
            yield return new WaitForSeconds(0.05f);
            if (currentCreditNumber != Credits.Length)
            {
                Credits[currentCreditNumber].transform.eulerAngles += new Vector3(0, 9, 0);
            }
            if (currentCreditNumber != 0)
            {
                Credits[currentCreditNumber - 1].transform.eulerAngles += new Vector3(0, 9, 0);
            }
        }

        //move to next credit in list
        //if last credit, inform menu that credits can be pressed again
        currentCreditNumber++;
        if (currentCreditNumber != Credits.Length + 1)
        {
            yield return new WaitForSeconds(2);
            StartCoroutine(RotateCredits());
        }
        else
        {
            currentCreditNumber = 0;
            GameObject.Find("CreditsButton").SendMessage("CreditsHaveStopped");
        }
    }
}
