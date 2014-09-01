using System;

namespace AmbreMaintenance
{
    public class AuditTrailEventArgs : EventArgs
    {
        public object[] Data = null;
        public AuditTrailEventType auditTrailEventType;

        public AuditTrailEventArgs()
        {

        }
    }

    public enum AuditTrailEventType
    {
        LoadAuditTrailFills = 0,
        PlayAuditTrailFills = 1
    }
}
