﻿using UnityEngine;
using System.Collections;

using uNature.Core.Sectors;

namespace uNature.Core.FoliageClasses
{
    public sealed class FoliageCore_Chunk : Chunk
    {
        #region Variables
        [SerializeField]
        private FoliageManagerInstance attachedFoliageInstance;

        /// <summary>
        /// Checks if an foliage instance exist on this chunk.
        /// </summary>
        public bool isFoliageInstanceAttached
        {
            get
            {
                return attachedFoliageInstance != null;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// On Draw Gizmos
        /// </summary>
        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (FoliageCore_MainManager.instance == null) return;

            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = isFoliageInstanceAttached ? Color.magenta : new Color(1f / 255, 1f / 165, 0, 1);

            int integralAreaValue = FoliageCore_MainManager.FOLIAGE_INSTANCE_AREA_SIZE;

            Gizmos.DrawWireCube(new Vector3(integralAreaValue / 2, 0, integralAreaValue / 2), new Vector3(integralAreaValue, 1, integralAreaValue));

            Gizmos.color = Color.white;
        }

        public override void OnCreated()
        {
            base.OnCreated();

            BoxCollider bc = GetComponent<BoxCollider>();

            if (bc == null)
            {
                bc = gameObject.AddComponent<BoxCollider>();
            }

            bc.center = new Vector3(size.x / 2, 0.1f, size.y / 2);
            bc.size = new Vector3(size.x, 0.1f, size.y);
            bc.isTrigger = true;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            BoxCollider bc = GetComponent<BoxCollider>();

            if (bc == null)
            {
                bc = gameObject.AddComponent<BoxCollider>();
            }

            bc.center = new Vector3(size.x / 2, 0.1f, size.y / 2);
            bc.size = new Vector3(size.x, 0.1f, size.y);
            bc.isTrigger = true;
        }

        /// <summary>
        /// Get the attached Foliage Manager Instance and if not availabe, create one.
        /// </summary>
        /// <returns></returns>
        public FoliageManagerInstance GetOrCreateFoliageManagerInstance()
        {
            if(!isFoliageInstanceAttached)
            {
                attachedFoliageInstance = FoliageManagerInstance.CreateInstance(this);
            }

            return attachedFoliageInstance;
        }

        /// <summary>
        /// Check for in bounds
        /// </summary>
        /// <param name="normalizedPosition"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public bool InBounds(Vector3 normalizedPosition, float distance)
        {
            Vector3 min = new Vector3(transform.position.x - distance, 0, transform.position.z - distance);
            Vector3 max = new Vector3(transform.position.x + size.x + distance, 0, transform.position.z + size.y + distance);

            return normalizedPosition.x >=  min.x && normalizedPosition.z >= min.z
                && normalizedPosition.x <= max.x && normalizedPosition.z <= max.z;
        }
        #endregion
    }
}
