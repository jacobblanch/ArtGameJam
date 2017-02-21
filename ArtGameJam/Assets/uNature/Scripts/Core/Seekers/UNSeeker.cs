using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using uNature.Core.Targets;
using uNature.Core.Threading;
using uNature.Core.FoliageClasses;

namespace uNature.Core.Seekers
{
    /// <summary>
    /// Seekers are basically GameObjects in the scene which should interact with the objects in the game.
    /// </summary>
    public class UNSeeker : FoliageReceiver
    {
        #region Variables
        /// <summary>
        /// What was the last position our AOI was updated on?
        /// </summary>
        Vector3 lastMovement = Vector3.zero;

        /// <summary>
        /// How far will it look ?
        /// </summary>
        public float seekingDistance = 20f;

        /// <summary>
        /// Will the seeker handle the instances interaction
        /// </summary>
        public bool detectTreeInstancesInteraction = true;

        /// <summary>
        /// Disable this if you want to do your own trees logic.
        /// </summary>
        public bool attackTrees = true;
        #endregion

        /// <summary>
        /// Check for movement.
        /// </summary>
        protected override void Update()
        {
            base.Update();

            if(Vector3.Distance(transform.position, lastMovement) > checkDistance)
            {
                lastMovement = transform.position;

                UNTarget.CheckTargets(this, seekingDistance);
            }

            if (attackTrees && detectTreeInstancesInteraction && playerCamera != null && Input.GetMouseButtonDown(0))
            {
                Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 10))
                {
                    if (hit.transform.GetComponentInParent<uNature.Core.Pooling.IHarvestableItem>() != null)
                    {
                        hit.transform.GetComponentInParent<uNature.Core.Pooling.IHarvestableItem>().Hit(20);
                    }
                }
            }
        }

        /// <summary>
        /// Called on start, initiate initial check targets.
        /// </summary>
        public virtual IEnumerator Start()
        {
            lastMovement = transform.position;

            yield return new WaitForSeconds(0.1f);

            if (!Application.isPlaying) yield break;

            lastMovement = transform.position;
            UNTarget.CheckTargets(this, seekingDistance);
        }
    }
}