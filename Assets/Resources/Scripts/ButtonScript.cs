using UnityEngine;
using System.Collections;

public class ButtonScript : MonoBehaviour {

    static bool activated;
    Vector3 startPosition;
    Color UIColour = new Color(0,0,1);
    static bool creditsRolling;

	// Use this for initialization
	void Start () {
        activated = false;
        startPosition = transform.localPosition;
        creditsRolling = false;
	}

    IEnumerator ButtonPressed()
    {
        GetComponent<BoxCollider>().enabled = false;
        //Button activated effect - slide right and fade out
        while (renderer.material.color.a > 0)
        {
            yield return new WaitForSeconds(0.02f);
            renderer.material.color = new Color(UIColour.r, UIColour.g, UIColour.b, renderer.material.color.a - 0.05f);
            transform.localPosition += new Vector3(renderer.material.color.a / 20, 0, 0);
        }
        ButtonEffect();
    }

    IEnumerator ButtonReset()
    {
        transform.localPosition = startPosition;
        //fade button back in
        while (renderer.material.color.a < 1)
        {
            yield return new WaitForSeconds(0.02f);
            renderer.material.color = new Color(UIColour.r, UIColour.g, UIColour.b, renderer.material.color.a + 0.05f);
        }
        GetComponent<BoxCollider>().enabled = true;
        activated = false;
    }

    void OnTriggerEnter(Collider c)
    {
        if (!activated)
        {
            activated = true;
            StartCoroutine(ButtonPressed());
        }
    }

    void ButtonEffect()
    {
        switch (gameObject.name)
        {
            case "ResetButton":
                ResetButtonEffect();
                break;
            case "ExplodeButton":
                ExplodeButtonEffect();
                break;
            case "CreditsButton":
                CreditsButtonEffect();
                break;
            default:
                break;
        }
    }

    void ResetButtonEffect()
    {
        GameObject model = GameObject.FindGameObjectWithTag("MainModel");
        foreach (GrabbableObject g in model.transform.GetComponentsInChildren<GrabbableObject>())
        {
            g.ResetPosition();
        }
        StartCoroutine(ButtonReset());
    }

    void ExplodeButtonEffect()
    {
        GameObject model = GameObject.FindGameObjectWithTag("MainModel");
        foreach (GrabbableObject g in model.transform.GetComponentsInChildren<GrabbableObject>())
        {
            g.Explode();
        }
        StartCoroutine(ButtonReset());
    }

    void CreditsButtonEffect()
    {
        if (!creditsRolling)
        {
            creditsRolling = true;
            GameObject.FindGameObjectWithTag("Credits").SendMessage("RunCredits");
        }
        StartCoroutine(ButtonReset());
        
    }

    public void CreditsHaveStopped()
    {
        creditsRolling = false;
    }
}
