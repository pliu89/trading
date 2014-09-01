using System;
using System.Collections.Generic;


namespace UV.Lib.Engines
{
    using UV.Lib.FrontEnds.Clusters;

	public interface IEngineContainer
	{
		//
		// Indentification
		//
		// IEngineHub ParentEngineHub { get; }
		int EngineContainerID { get; }          // unique index associated with parent EngineHub's list.
		string EngineContainerName { get; }     // non-unique user-friendly name of this container.

		//
		// Access objects inside this object
		//
		List<IEngine> GetEngines();             // list of engines inside this container.
		Cluster GetCluster();

		//
		// GUI Configuration
		//
		ClusterConfiguration ClusterConfiguation { get; }

		//
		// Event processing
		//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		/// <returns>true if any of its engines IsUpdateRequired = true.</returns>
		bool ProcessEvent(EventArgs e);
	}//end interface
}
