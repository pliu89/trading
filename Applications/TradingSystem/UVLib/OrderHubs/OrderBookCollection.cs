using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace UV.Lib.OrderHubs
{
    using UV.Lib.Products;


    public class OrderBookCollection : Dictionary<InstrumentName,OrderBook>
    {

        #region no Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
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
        public bool TryUpdate(OrderBookEventArgs eventArg)//, out Order initialOrder, out Order finalOrder)
        {
            //initialOrder = null;
            //finalOrder = null;
            bool isSuccess = false;
            OrderBook orderBook;
            switch (eventArg.EventType)
            {
                case OrderBookEventArgs.EventTypes.NewOrder:
                    if (this.TryGetValue(eventArg.Instrument, out orderBook))
                    {
                        isSuccess = orderBook.TryAddOrder(eventArg.Order);
                        //finalOrder = eventArg.Order;
                    }
                    break;
                case OrderBookEventArgs.EventTypes.DeletedOrder:                    
                    Order deletedOrder;
                    if (this.ContainsKey(eventArg.Instrument))
                    {
                        isSuccess = this[eventArg.Instrument].TryDeleteOrder(eventArg.Order.Tag, out deletedOrder);
                        //initialOrder = deletedOrder;
                    }
                    break;
                case OrderBookEventArgs.EventTypes.ChangedOrder:


                   break;
                case OrderBookEventArgs.EventTypes.CreatedBook:
                    if (!this.ContainsKey(eventArg.Instrument))
                    {
                        if (OrderBook.TryCreate(eventArg, out orderBook))
                        {
                            this.Add(eventArg.Instrument, orderBook);
                            isSuccess = true;
                        }
                    }
                    break;
                default:
                   return false;
            }//switch()
            // Exit
            return isSuccess;
        }//TryUpdate;
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
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
