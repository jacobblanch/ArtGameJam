using UnityEngine;
using uNature.Core.Utility;

namespace uNature.Core.FoliageClasses
{
    [ExecuteInEditMode]
    public class TouchBending : BaseInteraction
    {
        public static Vector4[] bendingTargets = new Vector4[20]; // 20 -> max bending targets.

        private static int getFreeID
        {
            get
            {
                for (int i = 0; i < bendingTargets.Length; i++)
                {
                    if (bendingTargets[i].w == 0)
                        return i;
                }

                return -1;
            }
        }

        #region Variables
        public bool simulateOnEditorTime = true;

        [Tooltip("What is the radius of affect?")]
        public float radius = 1;
        
        [Tooltip("On what range will this actually be calculated ? use a small distance if you have a lot of touch bending objects.")]
        public float seekingRange = 50;
        #endregion

        #region Parameters
        private bool inBounds = false;

        private int id = 0;

        [System.NonSerialized]
        private float _transformedRadius = -1;
        public float transformedRadius
        {
            get
            {
                return _transformedRadius;
            }
            set
            {
                if (_transformedRadius != value)
                {
                    _transformedRadius = value;
                    UpdateStaticBendingCache_Radius();
                }
            }
        }

        private bool simulate
        {
            get
            {
                return Application.isPlaying || simulateOnEditorTime;
            }
        }

        [System.NonSerialized]
        Vector3 lastChangedPosition;
        #endregion

        protected override bool includedInInteractionMap
        {
            get
            {
                return false;
            }
        }

        protected override void OnEnable()
        { 
            base.OnEnable();

            if (!simulate) return;

            id = getFreeID;

            if (id == -1)
            {
                Debug.LogError("No more space for touch bending targets, please increase the max bending objects amount! -> " + transform.name);
                this.enabled = false;

                return;
            }

            transformedRadius = radius;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (id == -1) return;

            bendingTargets[id].w = 0;

            UpdateStaticBendingCache_Data();

            id = -1;
        }

        private void UpdateStaticBendingCache_Radius()
        {
            bendingTargets[id].w = transformedRadius;
        }

        private void UpdateStaticBendingCache_Data()
        {
            for (int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
            {
                FoliageDB.unSortedPrototypes[i].UpdateTouchBending();
            }
        }

        private bool CalculateBounds()
        {
            FoliageReceiver fReceiver;

            for(int i = 0; i < FoliageReceiver.FReceivers.Count; i++)
            {
                fReceiver = FoliageReceiver.FReceivers[i];

                if (!fReceiver.isGrassReceiver) continue;

                if (Vector3.Distance(fReceiver.threadPosition, threadPosition) <= seekingRange) return true;
            }

            return false;
        }

        protected override void OnPositionChanged(Vector3 newPosition)
        {
            base.OnPositionChanged(newPosition);

            if (!simulate) return;

            float distance = Vector3.Distance(lastChangedPosition, newPosition);

            if(distance > 0.02f)
            {
                inBounds = CalculateBounds();

                if (!inBounds) return;

                transformedRadius = radius; // look for changes from editor if happened and update...

                bendingTargets[id].x = newPosition.x;
                bendingTargets[id].y = newPosition.y;
                bendingTargets[id].z = newPosition.z;

                UpdateStaticBendingCache_Data();

                lastChangedPosition = newPosition;
            }
        }
    }
}
