/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

// Leap Motion hand script that detects pinches and grabs the closest rigidbody.
public class GrabbingHand : MonoBehaviour
{

    public enum PinchState
    {
        kPinched,
        kReleased,
        kReleasing
    }

    // Layers that we can grab.
    public LayerMask grabbableLayers = ~0;

    // Curve of the trailing off of strength as you release the object.
    public AnimationCurve releaseStrengthCurve;

    //pinch strength for grabbing objects
    public float pinchStrength = 0.8f;

    // Filtering the rotation of grabbed object.
    public float rotationFiltering = 0.4f;

    // Filtering the movement of grabbed object.
    public float positionFiltering = 0.4f;

    // Minimum tracking confidence of the hand that will cause a change of state.
    public float minConfidence = 0.1f;

    // Clamps the movement of the grabbed object.
    public Vector3 maxMovement = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
    public Vector3 minMovement = new Vector3(-Mathf.Infinity, -Mathf.Infinity, -Mathf.Infinity);

    protected PinchState pinch_state_;
    protected Collider active_object_;

    protected float last_max_angular_velocity_;
    protected Quaternion rotation_from_palm_;

    protected Vector3 current_pinch_position_;
    protected Vector3 filtered_pinch_position_;
    protected Vector3 object_pinch_offset_;
    protected Quaternion palm_rotation_;

    void Start()
    {
        pinch_state_ = PinchState.kReleased;
        active_object_ = null;
        last_max_angular_velocity_ = 0.0f;
        rotation_from_palm_ = Quaternion.identity;
        current_pinch_position_ = Vector3.zero;
        filtered_pinch_position_ = Vector3.zero;
        object_pinch_offset_ = Vector3.zero;
        palm_rotation_ = Quaternion.identity;
    }

    void OnDestroy()
    {
        OnRelease();
    }

    // Finds the closest grabbable object within range of the pinch.
    protected Collider FindClosestGrabbableObject(Vector3 pinch_position)
    {
        Collider closest = null;
        Collider[] close_things =
            Physics.OverlapSphere(pinch_position, 0.1f, grabbableLayers);

        for (int j = 0; j < close_things.Length; ++j)
        {
            //pinch position must be inside bounds of object's collider
            if (close_things[j].rigidbody != null && close_things[j].collider.bounds.Contains(pinch_position) &&
                !close_things[j].transform.IsChildOf(transform) &&
                close_things[j].tag != "NotGrabbable")
            {

                GrabbableObject grabbable = close_things[j].GetComponent<GrabbableObject>();
                if (grabbable == null || !grabbable.IsGrabbed())
                {
                    closest = close_things[j];
                }
            }
        }
        return closest;
    }

    // Notify grabbable objects when they are ready to grab :)
    protected void Hover()
    {
        Collider hover = FindClosestGrabbableObject(current_pinch_position_);

        if (hover != active_object_ && active_object_ != null)
        {
            GrabbableObject old_grabbable = active_object_.GetComponent<GrabbableObject>();

            if (old_grabbable != null)
                old_grabbable.OnStopHover();
        }

        if (hover != null)
        {
            GrabbableObject new_grabbable = hover.GetComponent<GrabbableObject>();

            if (new_grabbable != null)
            {
                new_grabbable.OnStartHover();
                if (new_grabbable.IsResetting())
                {
                    return;
                }
            }
        }

        active_object_ = hover;
    }

    protected void StartPinch()
    {
        // Only pinch if we're hovering over an object.
        if (active_object_ == null)
            return;
        HandModel hand_model = GetComponent<HandModel>();
        Leap.Utils.IgnoreCollisions(gameObject, active_object_.gameObject, true);
        GrabbableObject grabbable = active_object_.GetComponent<GrabbableObject>();

        // Setup initial position and rotation conditions.
        palm_rotation_ = hand_model.GetPalmRotation();
        object_pinch_offset_ = Vector3.zero;

        // If we don't center the object, find the closest point in the collider for our grab point.
        if (grabbable == null || !grabbable.centerGrabbedObject)
        {
            Vector3 delta_position = active_object_.collider.bounds.center - current_pinch_position_;

            // If we raycast hits the object, we're outside the collider so grab the hit point.
            // If not, we're inside the collider so just use the pinch position.
            object_pinch_offset_ = active_object_.transform.position - current_pinch_position_;
        }

        filtered_pinch_position_ = active_object_.collider.bounds.center - object_pinch_offset_;
        object_pinch_offset_ = Quaternion.Inverse(active_object_.transform.rotation) *
                               object_pinch_offset_;
        rotation_from_palm_ = Quaternion.Inverse(palm_rotation_) * active_object_.transform.rotation;

        // If we can rotate the object quickly, increase max angular velocity for now.
        if (grabbable == null || grabbable.rotateQuickly)
        {
            last_max_angular_velocity_ = active_object_.rigidbody.maxAngularVelocity;
            active_object_.rigidbody.maxAngularVelocity = Mathf.Infinity;
        }

        if (grabbable != null)
        {
            // Notify grabbable object that it was grabbed.
            grabbable.OnGrab();

            if (grabbable.useAxisAlignment)
            {
                // If this option is enabled we only want to align the object axis with the palm axis
                // so we'll cancel out any rotation about the aligned axis.
                Vector3 palm_vector = grabbable.rightHandAxis;
                if (hand_model.GetLeapHand().IsLeft)
                    palm_vector = Vector3.Scale(palm_vector, new Vector3(-1, 1, 1));

                Vector3 axis_in_palm = rotation_from_palm_ * grabbable.objectAxis;
                Quaternion axis_correction = Quaternion.FromToRotation(axis_in_palm, palm_vector);
                if (Vector3.Dot(axis_in_palm, palm_vector) < 0)
                    axis_correction = Quaternion.FromToRotation(axis_in_palm, -palm_vector);

                rotation_from_palm_ = axis_correction * rotation_from_palm_;
            }
        }
    }

    protected void OnRelease()
    {
        if (active_object_ != null)
        {
            // Notify the grabbable object that is was released.
            GrabbableObject grabbable = active_object_.GetComponent<GrabbableObject>();
            if (grabbable != null)
                grabbable.OnRelease();

            if (grabbable == null || grabbable.rotateQuickly)
                active_object_.rigidbody.maxAngularVelocity = 0;

            Leap.Utils.IgnoreCollisions(gameObject, active_object_.gameObject, false);
        }
        active_object_ = null;

        Hover();
    }

    protected PinchState GetNewPinchState()
    {
        HandModel hand_model = GetComponent<HandModel>();
        Hand leap_hand = hand_model.GetLeapHand();

        Vector leap_thumb_tip = leap_hand.Fingers[0].TipPosition;
        float closest_distance = Mathf.Infinity;

        // Check thumb tip distance to joints on all other fingers.
        // If it's close enough, you're pinching.
        for (int i = 1; i < HandModel.NUM_FINGERS; ++i)
        {
            Finger finger = leap_hand.Fingers[i];

            for (int j = 0; j < FingerModel.NUM_BONES; ++j)
            {
                Vector leap_joint_position = finger.Bone((Bone.BoneType)j).NextJoint;

                float thumb_tip_distance = leap_joint_position.DistanceTo(leap_thumb_tip);
                closest_distance = Mathf.Min(closest_distance, thumb_tip_distance);
            }
        }

        // Scale trigger distance by thumb proximal bone length.

        if (leap_hand.PinchStrength >= pinchStrength)
            return PinchState.kPinched;
        if (leap_hand.PinchStrength < pinchStrength && pinch_state_ != PinchState.kReleased && pinch_state_ != PinchState.kReleasing)
            return PinchState.kReleasing;
        return PinchState.kReleased;
    }

    protected void UpdatePinchPosition()
    {
        HandModel hand_model = GetComponent<HandModel>();
        current_pinch_position_ = 0.5f * (hand_model.fingers[0].GetTipPosition() +
                                          hand_model.fingers[1].GetTipPosition());

        Vector3 delta_pinch = current_pinch_position_ - filtered_pinch_position_;
        filtered_pinch_position_ += (1.0f - positionFiltering) * delta_pinch;
    }

    protected void UpdatePalmRotation()
    {
        HandModel hand_model = GetComponent<HandModel>();
        palm_rotation_ = Quaternion.Slerp(palm_rotation_, hand_model.GetPalmRotation(),
                                          1.0f - rotationFiltering);
    }

    // If we're in a pinch state, just move the object to the right spot using velocities.
    protected void ContinueHardPinch()
    {
        Quaternion target_rotation = palm_rotation_ * rotation_from_palm_;

        Vector3 target_position = filtered_pinch_position_ + target_rotation * object_pinch_offset_;
        target_position.x = Mathf.Clamp(target_position.x, minMovement.x, maxMovement.x);
        target_position.y = Mathf.Clamp(target_position.y, minMovement.y, maxMovement.y);
        target_position.z = Mathf.Clamp(target_position.z, minMovement.z, maxMovement.z);
        Vector3 velocity = (target_position - active_object_.transform.position) / Time.deltaTime;
        active_object_.rigidbody.velocity = velocity;

        Quaternion delta_rotation = target_rotation *
                                    Quaternion.Inverse(active_object_.transform.rotation);

        float angle = 0.0f;
        Vector3 axis = Vector3.zero;
        delta_rotation.ToAngleAxis(out angle, out axis);

        if (angle >= 180)
        {
            angle = 360 - angle;
            axis = -axis;
        }
        if (angle != 0)
            active_object_.rigidbody.angularVelocity = angle * axis;
    }


    void FixedUpdate()
    {
        UpdatePalmRotation();
        UpdatePinchPosition();
        HandModel hand_model = GetComponent<HandModel>();
        Hand leap_hand = hand_model.GetLeapHand();

        if (leap_hand == null)
            return;

        PinchState new_pinch_state = GetNewPinchState();
        if (pinch_state_ == PinchState.kPinched)
        {
            if (new_pinch_state == PinchState.kReleased)
                OnRelease();
            else if (active_object_ != null)
                ContinueHardPinch();
        }
        else if (pinch_state_ == PinchState.kReleasing)
        {
            if (new_pinch_state == PinchState.kReleased)
                OnRelease();
            else if (new_pinch_state == PinchState.kPinched)
                StartPinch();
        }
        else
        {
            if (new_pinch_state == PinchState.kPinched)
                StartPinch();
            else
                Hover();
        }
        pinch_state_ = new_pinch_state;
    }
}