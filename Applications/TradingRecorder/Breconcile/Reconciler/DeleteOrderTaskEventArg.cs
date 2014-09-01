using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Misty.Lib.TaskHubs;

namespace Ambre.Breconcile.Reconciler
{
    


    public class DeleteOrderTaskEventArg : TaskEventArg
    {

        public int LookBackDate = 100;                                // set this task a look back date, initial set to 100.

        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            int days;
            foreach (string key in attributes.Keys)
            {
                if (key.Equals("LookBackDate"))
                {
                    days = int.Parse(attributes[key]);
                    this.LookBackDate = days;
                }
            }
        }// SetAttributes()



    }
}
