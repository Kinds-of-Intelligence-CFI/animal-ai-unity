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

        public override AttachedObjectDetails attachedObjectDetails
        {
            get => base.attachedObjectDetails;
            set
            {
                base.attachedObjectDetails = value;
                if (operations != null)
                {
                    foreach (var op in operations)
                    {
                        op.attachedObjectDetails = value;
                    }
                }
            }
        }

        public override void execute()
        {
            Operation operation = ChooseOperation();
            operation.execute();
        }

        private Operation ChooseOperation()
        {
            if (operationWeights.Count == 0)
                return operations[Random.Range(0, operations.Count)];

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