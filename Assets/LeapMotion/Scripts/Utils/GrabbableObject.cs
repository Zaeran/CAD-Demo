/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;

public class GrabbableObject : MonoBehaviour
{

    public bool useAxisAlignment = false;
    public Vector3 rightHandAxis;
    public Vector3 objectAxis;

    public bool rotateQuickly = true;
    public bool centerGrabbedObject = false;

    public Rigidbody breakableJoint;
    public float breakForce;
    public float breakTorque;

    protected bool grabbed_ = false;
    protected bool hovered_ = false;

    private bool resetting_ = false;
    private bool exploding_ = false;

    public GameObject baseObject;

    public bool IsHovered()
    {
        return hovered_;
    }

    public bool IsGrabbed()
    {
        return grabbed_;
    }

    public bool IsResetting()
    {
        return resetting_;
    }

    public virtual void OnStartHover()
    {
        hovered_ = true;
    }

    public virtual void OnStopHover()
    {
        hovered_ = false;
    }

    public virtual void OnGrab()
    {
        grabbed_ = true;
        hovered_ = false;
        exploding_ = false;

        if (breakableJoint != null)
        {
            Joint breakJoint = breakableJoint.GetComponent<Joint>();
            if (breakJoint != null)
            {
                breakJoint.breakForce = breakForce;
                breakJoint.breakTorque = breakTorque;
            }
        }
    }

    public virtual void OnRelease()
    {
        grabbed_ = false;

        if (breakableJoint != null)
        {
            Joint breakJoint = breakableJoint.GetComponent<Joint>();
            if (breakJoint != null)
            {
                breakJoint.breakForce = Mathf.Infinity;
                breakJoint.breakTorque = Mathf.Infinity;
            }
        }
        //stop movement of object
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        //if close to base object, snap to correct position and rotation
        if (baseObject != null)
        {
            if (Vector3.Distance(transform.position, baseObject.transform.position) < 0.1f)
            {
                transform.position = baseObject.transform.position;
                transform.rotation = baseObject.transform.rotation;
            }
        }
    }

    void Update()
    {
        if (resetting_)
        {
            //move to reset position
            transform.localPosition = Vector3.Lerp(transform.localPosition, new Vector3(0,0,0), 0.1f);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, 0.1f);
            if (Vector3.Distance(transform.localPosition, new Vector3(0, 0, 0)) < 0.001f)
            {
                resetting_ = false;
            }
            exploding_ = false;
        }

        if (exploding_)
        {
            //move to explode position
            transform.position = Vector3.Lerp(transform.position, GameObject.Find(gameObject.name + "Explode").transform.position, 0.1f);
            transform.rotation = Quaternion.Lerp(transform.rotation, GameObject.Find(gameObject.name + "Explode").transform.rotation, 0.1f);
            if (Vector3.Distance(transform.position, GameObject.Find(gameObject.name + "Explode").transform.position) < 0.001f)
            {
                exploding_ = false;
            }
            resetting_ = false;
        }
    }

    public void ResetPosition()
    {
        resetting_ = true;
    }

    public void Explode()
    {
        exploding_ = true;
    }
}
