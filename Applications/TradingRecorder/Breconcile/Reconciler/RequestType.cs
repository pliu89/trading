using System;

namespace Ambre.Breconcile.Reconciler
{
    public enum RequestType
    {
        None
        ,Stop                           // Reconciler should shutdown.
        ,MonitorCopyNewFiles            // Reconciler should (repeatedly) try to copy new remote files to local dir.
        ,ReconcileStatement             // Reads statements, extract settlement datetime, recreates books.
        //
        //
        ,MultipleSequentialRequests     // request is wrapper for multiple requests in Data list.
        //
        //
        ,DebugTest                      // special request for debuggin.

    }
}
