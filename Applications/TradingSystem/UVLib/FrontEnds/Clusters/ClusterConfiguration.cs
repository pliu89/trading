using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.FrontEnds.Clusters
{
    [Serializable]
    public class ClusterConfiguration
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //

        // ClusterDisplay Configuration
        public int GuiID = 0;						// GUI ID I will be displayed in, GUI #0 is the default.
        public int GuiRow = -1;						// Position inside GUI.
        public int GuiColumn = -1;					// Position inside GUI.

        // Cluster internal Configuration			
        public int BoxRowColumns = 6;				// number of market depth columns to display


        #endregion// members


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        // ****						Clone()						**** 
        //
        /// <summary>
        /// Using serialization techniques, this method creates a new clone
        /// of this object.
        /// </summary>
        /// <returns>A new object that is a copy of this one.</returns>
        public ClusterConfiguration Clone()
        {
            ClusterConfiguration original = this;
            if (!typeof(ClusterConfiguration).IsSerializable)
            {
                throw (new ArgumentException("Type must be serializable.", "original"));
            }
            if (Object.ReferenceEquals(original, null)) { return default(ClusterConfiguration); }

            System.Runtime.Serialization.IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();            
            using (System.IO.Stream stream = new System.IO.MemoryStream())
            {
                formatter.Serialize(stream, original);
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                return (ClusterConfiguration)formatter.Deserialize(stream);
            }
        }// Clone()
        //
        //
        //
        //
        #endregion//Public Methods



    }//end class
}