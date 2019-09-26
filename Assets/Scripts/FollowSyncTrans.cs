using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowSyncTrans : MonoBehaviour {

    public float animationTreshold = 1f;

    private Animator animator;
    private Transform target;

	public void Init (Transform trans) {
        target = trans;
        animator = GetComponent<Animator>();
	}
	
	void Update () {
		if(target != null)
        {
            transform.position = Vector3.Lerp(transform.position, target.position, 0.1f);
            transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, 0.1f);

            if((target.position - transform.position).sqrMagnitude / Time.deltaTime > animationTreshold)
            {
                animator.SetBool("isWalking", true);
            }
            else
            {
                animator.SetBool("isWalking", false);
            }
        }
	}
}
