using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.Breconcile.BookReaders
{
    using Misty.Lib.IO.Xml;

    using Misty.Lib.Products;
    using Misty.Lib.OrderHubs;

    /// <summary>
    /// This is a series for a single instrument.  Its a time series of fill events.
    /// </summary>
    public class EventSeries
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        public InstrumentName Name;                                 // In
        public Fill InitialState = null;
        public List<Fill> Series = new List<Fill>();

        public List<Node> PreStartNodeList = new List<Node>();      // nodes that need to be processed earlier than our Initial State.
        public Fill FinalState = null;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public EventSeries(InstrumentName name)
        {
            Name = name;
        }
        //
        //       
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        // *****************************************************************
        // ****                         Append()                        ****
        // *****************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newNodes"></param>
        public void Append( List<Node> newNodes )
        {
            // Handle special case of first loading.  Otherwise, we need not try to create InitialState
            while (InitialState == null && newNodes.Count > 0 )
            {   // If there is no intial state, we are in trouble, so need to store these in PreStart list.
                // If later we do an insert (load earlier drop files) we will add these events from the PreStart list.
                Node aNode = newNodes[0];
                newNodes.RemoveAt(0);
                if (aNode.Name.Contains("BookLifo"))
                {
                    Fill aFill = GetFillFromFillBook(aNode);
                    InitialState = aFill;
                }
                else
                    PreStartNodeList.Add(aNode);
            }

            // Extract all nodes.
            bool lastNodeWasBook = false;                               // to speed things up, ignore double book events
            while (newNodes.Count > 0)
            {
                Node aNode = newNodes[0];
                newNodes.RemoveAt(0);
                if (aNode.Name.Contains("BookLifo"))
                {   // This is an intermediate summary snapshot.
                    // TODO: Use this to validate our current state.
                    if (!lastNodeWasBook)
                    {
                        /*
                        Fill effectiveFill = GetFillFromFillBook(aNode);
                        int qty = this.InitialState.Qty;
                        foreach (Fill aFill in this.Series)
                            qty += aFill.Qty;
                        if (qty != effectiveFill.Qty)
                        {   // This is an error.
                            Console.WriteLine("Error in {0}. Total={1} Book={2} {3}", this.Name, qty, effectiveFill, aNode);
                        }
                        */ 
                        lastNodeWasBook = true;
                    }

                }
                else if (aNode.Name.Contains("Ambre.TTServices.Fills.FillEventArgs"))
                {
                    Fill aFill = GetFillFromFillEvent(aNode);
                    Series.Add(aFill);
                    lastNodeWasBook = false;
                }                
            }

            UpdateFinalState();

        }// Append()
        //
        //
        // *****************************************************************
        // ****                       Insert()                          ****
        // *****************************************************************
        /// <summary>
        /// Simple version:  is to look for the earliest fill book in the list, accept it
        /// as our initial state, then fill in all the following fills.
        /// </summary>
        /// <param name="newNodes"></param>
        public void Insert(List<Node> newNodes)
        {
            // Search for earliest fill book snapshot.
            int earliestBookPtr = 0;
            Fill initialFill = null;
            while (earliestBookPtr < newNodes.Count)
            {
                if (newNodes[earliestBookPtr].Name.Contains("BookLifo"))
                {
                    initialFill = GetFillFromFillBook(newNodes[earliestBookPtr]);
                    break;
                }
                earliestBookPtr++;
            }
            if (initialFill == null)
            {   // We never found a fill book!
                PreStartNodeList.InsertRange(0, newNodes);              // Store these nodes in the pre-start nodes.
                return;
            }

            // Accept the new initial state.
            InitialState = initialFill;

            // Proceed to insert the old nodes in the pre start list.
            while (PreStartNodeList.Count > 0)
            {
                Node aNode = PreStartNodeList[PreStartNodeList.Count - 1];  // pull a node off the back of the list
                PreStartNodeList.RemoveAt(PreStartNodeList.Count - 1);  // remove it from the list
                if (aNode.Name.Contains("Ambre.TTServices.Fills.FillEventArgs"))
                {
                    Fill aFill = GetFillFromFillEvent(aNode);
                    Series.Insert(0, aFill);                                // insert this Fill
                }
            }
            // Load all the new fills that followed the earliest fill book.
            int n = newNodes.Count - 1;                                     // point to last node.
            while (n > earliestBookPtr)
            {
                Node aNode = newNodes[n];                                   // pull a node off the back of the list
                newNodes.RemoveAt(n);                                       // remove it from the list
                if (aNode.Name.Contains("Ambre.TTServices.Fills.FillEventArgs"))
                {
                    Fill aFill = GetFillFromFillEvent(aNode);
                    Series.Insert(0, aFill);                                // insert this Fill in front of series
                }
                n--;
            }
            newNodes.RemoveAt(earliestBookPtr);                             // remove the book node (which is already set as InitialState).
            // Store the remaining nodes (earlier than the earliest book) in the pre start list.
            if (newNodes.Count > 0)
                PreStartNodeList.AddRange(newNodes);
           
        }// Insert()
        //
        //
        //
        //
        // *****************************************************************
        // ****                  TryGetStateAt()                        ****
        // *****************************************************************
        /// <summary>
        /// TODO: need to fully test this!
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public bool TryGetStateAt(DateTime localTime, out Fill state, out List<Fill> playedFills)
        {
            state = null;
            playedFills = new List<Fill>();
            if (InitialState == null || InitialState.LocalTime.CompareTo(localTime) > 0)
            {   // desired localTime is earlier than our InitialState.
                state = InitialState;                       // pass earliest thing we know about the state.
                return false;                               // tell user we did not get what he wanted.
            }
            if (FinalState.LocalTime.CompareTo(localTime) < 0)
            {
                state = FinalState;
                playedFills.AddRange(Series);
                return true;                                // TODO: Fix this. If we look ahead, we can confirm that this is correct for sure.
            }

            int ptr = 0;
            while (ptr < this.Series.Count && this.Series[ptr].LocalTime.CompareTo(localTime) < 0)
                ptr++;                                      // while ptr is earlier than localTime, increment ptr.
            int lastChangeIndex = ptr - 1;                  // this is the previous state change event, just before desired time.
            if (lastChangeIndex < 0)
                state = InitialState;
            else if (lastChangeIndex == this.Series.Count - 1)
                state = FinalState;
            else
            {
                int qty;
                DateTime exchTime;
                DateTime locTime;
                if (TryGetStateAt(lastChangeIndex, out qty, out exchTime, out locTime))
                {
                    state = new Fill();
                    state.Qty = qty;
                    state.ExchangeTime = exchTime;
                    state.LocalTime = locTime;
                }
            }

            if (lastChangeIndex > 0 && lastChangeIndex < this.Series.Count)
            {
                for (ptr = 0; ptr <= lastChangeIndex; ++ptr)
                {
                    playedFills.Add(Series[ptr]);
                }
            }

            return true;
        }// TryGetStateAt()
        //
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        /// <summary>
        /// Given a FillBook node, we try to extract the position in the book and return
        /// it as a single effect fill.
        /// </summary>
        /// <param name="aFillBookNode"></param>
        /// <returns></returns>
        private Fill GetFillFromFillBook(Node aFillBookNode)
        {
            DateTime dt;
            Fill aFill = new Fill();
            if (DateTime.TryParse(aFillBookNode.Attributes["LocalTimeLast"], out dt))
                aFill.LocalTime = dt;
            if (DateTime.TryParse(aFillBookNode.Attributes["ExchangeTimeLast"], out dt))
                aFill.ExchangeTime = dt;

            // Extract fills
            foreach (IStringifiable istr in aFillBookNode.SubElements)
            {
                Node subNode = (Node)istr;
                if (subNode.Name.Equals(typeof(Fill).FullName))
                {
                    int qty = 0;
                    double p = 0;
                    if (Int32.TryParse(subNode.Attributes["Qty"], out qty))
                        aFill.Qty += qty;
                    if (Double.TryParse(subNode.Attributes["Price"], out p))
                        aFill.Price += qty * p;
                }
            }
            if (aFill.Qty != 0)
                aFill.Price = aFill.Price / aFill.Qty;          // average cost 
            else
                aFill.Price = 0;
            return aFill;
        } // ExtractEffectiveFillFromBook()
        //
        private Fill GetFillFromFillEvent(Node fillEventNode)
        {
            DateTime dt;
            Fill aFill = new Fill();
            if (DateTime.TryParse(fillEventNode.Attributes["LocalTime"], out dt))
                aFill.LocalTime = dt;
            if (DateTime.TryParse(fillEventNode.Attributes["ExchangeTime"], out dt))
                aFill.ExchangeTime = dt;
            int Q = 0;
            if (Int32.TryParse(fillEventNode.Attributes["Qty"], out Q))
                aFill.Qty = Q;
            double P;
            if (Double.TryParse(fillEventNode.Attributes["Price"], out P))
                aFill.Price = P;            
            return aFill;
        } // ExtractEffectiveFillFromBook()
        //
        //
        //
        private void UpdateFinalState()
        {
            // Add up net position.
            if (this.InitialState == null)
                return;
            int qty = 0;
            qty = this.InitialState.Qty;
            foreach (Fill fill in this.Series)
                qty += fill.Qty;

            // Update the final state event, using times from last fill.
            Fill lastFill;
            if (this.Series.Count < 1)
                lastFill = this.InitialState;
            else
                lastFill = this.Series[this.Series.Count - 1];
            if (this.FinalState == null)
                this.FinalState = new Fill();
            this.FinalState.ExchangeTime = lastFill.ExchangeTime;
            this.FinalState.LocalTime = lastFill.LocalTime;            
            this.FinalState.Qty = qty;
            this.FinalState.Price = 0;

        }// UpdateFinalState()
        //
        //
        //
        //
        private bool TryGetStateAt(int lastChangeIndex, out int Qty, out DateTime exchTime, out DateTime localTime)
        {
            if (lastChangeIndex <= 0)
            {
                Qty = InitialState.Qty;
                exchTime = InitialState.ExchangeTime;
                localTime = InitialState.LocalTime;
                return true;
            }
            else if (lastChangeIndex >= this.Series.Count - 1)
            {
                Qty = FinalState.Qty;
                exchTime = FinalState.ExchangeTime;
                localTime = FinalState.LocalTime;
                return true;
            }
            else
            {
                Qty = this.InitialState.Qty;
                for (int i = 0; i <= lastChangeIndex; ++i)
                    Qty += this.Series[i].Qty;
                exchTime = this.Series[lastChangeIndex].ExchangeTime;
                localTime = this.Series[lastChangeIndex].LocalTime;                
                return true;
            }
        }
        //
        //
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers

    }
}
