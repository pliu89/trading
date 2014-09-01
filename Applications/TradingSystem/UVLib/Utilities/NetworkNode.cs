using System;
using System.Collections.Generic;

namespace UV.Lib.Utilities
{
	public class NetworkNode<T>
	{
		//
		// This node is an input node for other OUT nodes, called "Masters" in a Slavery network.  
		//	1. Any signal it receives is transmitted to each of its "Masters", amplified by the "MasterWeight[]", 
		//		or "OutWeight[]".
		//  2. The number of "Masters" ("Out" nodes) is pre-defined and fixed. So the OutList is actually an array.
		//	3. This node may also be a Master or Out node for another node; it may receive inputs from other nodes.
		//		3a. In a Slavery network, this node's "In" nodes are called Slaves.  Why, because they pass their
		//			signals to this node (their Master).
		//	4. All In nodes "Slaves" must add themselves to this node's InList for convenience.
		//
		public T[] OutList = new T[0];					// default number of masters is zero.
		public double[] OutWeights = new double[0];
		public string[] OutNames = new string[0];		// At start up, slaves must provide names of their Masters
														// who are looked up later after all engines are constructed.
		
		public List<T> InList = new List<T>();			// Slaves of this node attach themselves here at initialization.


		// Example 1: Usage in a Slavery network:
		//
		//
		//  InList<T>                    OutList[T]		// note: Outlist associated with weights!
		// ------------                 ----------------
		//
		//
		//                 _________ 
		//                /         \   MasterWeight[0]
		// Slave[0]--->--|           |--------->----------  Master[0], MasterName[0]
		//	             | this node |  
		// Slave[1]--->--|           |--------->----------  Master[1], MasterName[1]
		//                \_________/   MasterWeight[1]
		//
		//
		// Note the asymmetry above!  The input layer is simply associated with a list of objects. 
		// While the output layer has a list of objects, their names, but MORE IMPORTANTLY amplification
		// weights.  
		// This node knows only the weights of the connections to the NEXT LAYER.
		// Initialization:			
		//  Since the node needs to know its output, Slaves know all their Masters.  That is, 
		// Slaves will be responsible for initializing the network connections to their masters.


		// Example 2: Usage in a Vassalage network
		//
		//
		//  InList<T>                    OutList[T]		// note: Outlist associated with weights!
		// ------------                 ----------------
		//
		//
		//                 _________ 
		//                /         \   VassalWeight[0]
		// Lord[0]---<---|           |-------<-----------  Vassal[0], VassalName[0]
		//	             | this node |  
		// Lord[1]---<---|           |-------<-----------  Vassal[1], VassalName[1]
		//                \_________/   VassalWeight[1]
		//
		//
		// Here, this node is a Lord (if it has non-zero vassals).  A Lord can optionally take positions from its 
		// Vassals, according to weights.  Think about a butterfly taking +1 SpreadA and -1 SpreadB.   
		// This node my also have Lords above him.
		// The important point, is that a Lord has to know the amplification weights, while a Slave in the above
		// example needs to know the proper weights.
		// 
		// Initialization: 
		//	This is the opposite of the Slavery network in some ways.  Here, Lords will know all their Vassals,
		// and initialize the networks. 




	}//end class
}
