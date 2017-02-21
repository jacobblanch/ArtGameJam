using UnityEngine;
using uNature.Core.Threading;

using System.Collections.Generic;
using UniLinq;

namespace uNature.Core.FoliageClasses
{
    public abstract class BaseInteraction : ThreadItem
    {
        private static List<BaseInteraction> _interactions;
        public static List<BaseInteraction> interactions
        {
            get
            {
                if(_interactions == null)
                {
                    _interactions = FindObjectsOfType<BaseInteraction>().ToList();
                }

                return _interactions;
            }
        }

        protected virtual bool includedInInteractionMap
        {
            get
            {
                return true;
            }
        }

        private Vector3 lastPosition = Vector3.zero;

        protected override void OnEnable()
        {
            base.OnEnable();

            interactions.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            interactions.Remove(this);
        }

        protected override void OnPositionChanged(Vector3 newPosition)
        {
            base.OnPositionChanged(newPosition);

            if (includedInInteractionMap)
            {
                if (Vector3.Distance(transform.position, lastPosition) > 1f)
                {
                    FoliageReceiver.CallInteractionsRefresh();
                }
            }
        }

        /// <summary>
        /// Update the interaction
        /// </summary>
        /// <param name="receiver"></param>
        public void UpdateInteraction(FoliageReceiver receiver)
        {
            UpdateInteraction(receiver, GetNormalizedPosition(receiver));
        }

        /// <summary>
        /// Please dont use map.SetPixels, it will be assigned later on automatically..
        /// </summary>
        /// <param name="receiver"></param>
        protected virtual void UpdateInteraction(FoliageReceiver receiver, Vector2 normalizedPosition)
        {
        }

        protected Vector2 GetNormalizedPosition(FoliageReceiver receiver)
        {
            return GetNormalizedPosition(threadPositionDepth, receiver);
        }

        protected Vector2 GetNormalizedPosition(Vector2 position, FoliageReceiver receiver)
        {
            if (receiver.latestChunk == null) return Vector2.zero;

            var fManagerTransform = receiver.middleFoliageChunkFromNeighbors.isFoliageInstanceAttached ?
                receiver.middleFoliageChunkFromNeighbors.GetOrCreateFoliageManagerInstance() : null;

            Vector2 fManagerDepth = new Vector2(fManagerTransform.transform.position.x, fManagerTransform.transform.position.z);

            return receiver.interactionMap.TransformCord(position - fManagerDepth - receiver.latestChunk.position);
        }

        public static List<BaseInteraction> GetRelevantInteractions(FoliageReceiver receiver)
        {
            List<BaseInteraction> relevantInteractions = new List<BaseInteraction>();
            BaseInteraction interaction;

            for(int i = 0; i < interactions.Count; i++)
            {
                interaction = interactions[i];

                if(Vector3.Distance(receiver.threadPositionDepth, interaction.threadPositionDepth) <= receiver.interactionMap.radius)
                {
                    relevantInteractions.Add(interaction);
                }
            }

            return relevantInteractions;
        }
    }
}
