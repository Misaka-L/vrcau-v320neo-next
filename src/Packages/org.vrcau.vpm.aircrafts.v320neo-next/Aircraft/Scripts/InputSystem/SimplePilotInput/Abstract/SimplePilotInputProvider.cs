using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.Abstract
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class SimplePilotInputProvider : UdonSharpBehaviour
    {
        [PublicAPI] public bool overrideOthers;

        /// <summary>
        /// x - Pitch (up: 1 down: -1), y - Yaw (left: -1 right: 1), z - Roll (left: 1 right: -1)
        /// </summary>
        [PublicAPI] [NonSerialized] public Vector3 InputVector;

        /// <summary>
        /// x - Pitch (up: 1 down: -1), y - Yaw (left: -1 right: 1), z - Roll (left: 1 right: -1)
        /// </summary>
        [PublicAPI]
        protected void _SetInputVector(Vector3 inputVector) => InputVector = inputVector;
    }
}