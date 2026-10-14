using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FnI.BusinessAccessLayer.Inventory.Transaction
{
    public static class FIFOOperation
    {
        public static T Pop<T>(this List<T> theList)
        {
            var local = theList[theList.Count - 1];
            theList.RemoveAt(theList.Count - 1);
            return local;
        }

        public static void Push<T>(this List<T> theList, T item)
        {
            theList.Add(item);
        }   
    }
}
