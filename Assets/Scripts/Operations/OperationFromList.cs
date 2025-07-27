using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Operations
{
    /// <summary>
    /// Choose an operation from a list
    /// </summary>
    public class OperationFromList : Operation
    {
        public List<float> operationWeights { get; set; } = new List<float>();
        public List<Operation> operations { get; set; } = new List<Operation>();

        public void Initialize(AttachedObjectDetails details)
        {
            attachedObjectDetails = details;
        }

        public override void execute()
        {
            Operation operation = ChooseOperation();
            operation.execute();
        }

        private Operation ChooseOperation()
        {
            float totalWeight = operationWeights.Sum();
            float randomNumber = Random.Range(0, totalWeight - float.Epsilon);
            float cumulativeWeight = 0;

            for (int i = 0; i < operations.Count; i++)
            {
                cumulativeWeight += operationWeights[i];
                if (randomNumber <= cumulativeWeight)
                {
                    return operations[i];
                }
            }

            /* If no operation is selected within the loop (which should not happen), return the last one */
            return operations[operations.Count - 1];
        }
    }
}