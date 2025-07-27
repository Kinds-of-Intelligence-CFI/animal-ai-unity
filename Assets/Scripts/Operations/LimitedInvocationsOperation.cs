using UnityEngine;
using System.Collections.Generic;

namespace Operations
{
    /// <summary>
    /// Allow an attached operation to be invoked only a limited number of times.
    /// 
    /// -1 allows unlimited invocations.
    /// </summary>
    public class LimitedInvocationsOperation : Operation
    {
        public float maxInvocations { get; set; }
        public Operation operation { get; set; }

        private int invocations = 0;

        public void Initialize(AttachedObjectDetails details)
        {
            attachedObjectDetails = details;
        }

        public override void execute()
        {
            if (
                maxInvocations != -1
                && invocations >= maxInvocations
            )
            {
                Debug.Log($"Max invocations reached for operation: {operation.GetType().Name}");
                return;
            }
            operation.execute();
            invocations += 1;
        }
    }
}