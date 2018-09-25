using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace IFramework.EntityFrameworkCore
{
    public static class IsolationLevelExtensions
    {
        public static System.Data.IsolationLevel ToDataIsolationLevel(this IsolationLevel level)
        {
            System.Data.IsolationLevel systemLevel = System.Data.IsolationLevel.Serializable;
            switch (level)
            {
                case IsolationLevel.Chaos:
                    systemLevel = System.Data.IsolationLevel.Chaos;
                    break;
                case IsolationLevel.Serializable:
                    systemLevel = System.Data.IsolationLevel.Serializable;
                    break;
                case IsolationLevel.ReadCommitted:
                    systemLevel = System.Data.IsolationLevel.ReadCommitted;
                    break;
                case IsolationLevel.ReadUncommitted:
                    systemLevel = System.Data.IsolationLevel.ReadUncommitted;
                    break;
                case IsolationLevel.RepeatableRead:
                    systemLevel = System.Data.IsolationLevel.RepeatableRead;
                    break;
                case IsolationLevel.Snapshot:
                    systemLevel = System.Data.IsolationLevel.Snapshot;
                    break;
                case IsolationLevel.Unspecified:
                    systemLevel = System.Data.IsolationLevel.Snapshot;
                    break;
            }
            return systemLevel;
        }

        public static IsolationLevel ToSystemIsolationLevel(this System.Data.IsolationLevel level)
        {
            IsolationLevel systemLevel = IsolationLevel.Serializable;
            switch (level)
            {
                case System.Data.IsolationLevel.Chaos:
                    systemLevel = IsolationLevel.Chaos;
                    break;
                case System.Data.IsolationLevel.Serializable:
                    systemLevel = IsolationLevel.Serializable;
                    break;
                case System.Data.IsolationLevel.ReadCommitted:
                    systemLevel = IsolationLevel.ReadCommitted;
                    break;
                case System.Data.IsolationLevel.ReadUncommitted:
                    systemLevel = IsolationLevel.ReadUncommitted;
                    break;
                case System.Data.IsolationLevel.RepeatableRead:
                    systemLevel = IsolationLevel.RepeatableRead;
                    break;
                case System.Data.IsolationLevel.Snapshot:
                    systemLevel = IsolationLevel.Snapshot;
                    break;
                case System.Data.IsolationLevel.Unspecified:
                    systemLevel = IsolationLevel.Snapshot;
                    break;
            }

            return systemLevel;
        }
    }
}
