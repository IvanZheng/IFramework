using System.Collections.Generic;

namespace Kafka.Client.Cluster
{
    /// <summary>
    ///     Represents broker partition
    /// </summary>
    public class Partition
    {
        public Partition(string topic,
                         int partId,
                         Replica leader = null,
                         HashSet<Replica> assignedReplicas = null,
                         HashSet<Replica> inSyncReplica = null,
                         HashSet<Replica> catchUpReplicas = null,
                         HashSet<Replica> reassignedReplicas = null)
        {
            Topic = topic;
            Leader = leader;
            PartId = partId;
            AssignedReplicas = assignedReplicas ?? new HashSet<Replica>();
            InSyncReplicas = inSyncReplica ?? new HashSet<Replica>();
            CatchUpReplicas = catchUpReplicas ?? new HashSet<Replica>();
            ReassignedReplicas = reassignedReplicas ?? new HashSet<Replica>();
        }

        /// <summary>
        ///     Gets the partition ID.
        /// </summary>
        public int PartId { get; }

        public string Topic { get; }

        public Replica Leader { get; set; }

        public HashSet<Replica> AssignedReplicas { get; }

        public HashSet<Replica> InSyncReplicas { get; }

        public HashSet<Replica> CatchUpReplicas { get; }

        public HashSet<Replica> ReassignedReplicas { get; }

        public override string ToString()
        {
            return string.Format("Topic={0},PartId={1},LeaderBrokerId={2}"
                                 , Topic
                                 , PartId
                                 , Leader == null ? "NA" : Leader.BrokerId.ToString());
        }
    }
}