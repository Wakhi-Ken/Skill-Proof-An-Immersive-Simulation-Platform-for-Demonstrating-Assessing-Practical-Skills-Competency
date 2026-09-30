using UnityEngine;

public class VRArmController : MonoBehaviour
{
    public  Transform leftController;
    public  Transform rightController;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void LateUpdate()
    {
        if (animator != null)
            return;

        //left arm
        if(leftController != null)
        {
           Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);

            if (leftHand != null)
            {
                leftHand.position = leftController.position;
                leftHand.rotation = leftController.rotation;
            }
        }

        //right arm
        if(rightController != null)
        {
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand != null)
            {
                rightHand.position = rightController.position;
                rightHand.rotation = rightController.rotation;
            }
        }
    }


}
