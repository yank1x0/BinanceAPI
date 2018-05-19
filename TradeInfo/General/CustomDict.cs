using System;
using System.Collections.Generic;

namespace TradeInfo
{
    public class CustomDict<TKey, TValue> : Dictionary<TKey,TValue>
    {
        public TValue this[TKey key]
        {
            get
            {
                if (ContainsKey(key))
                    return base[key];
                else
                    throw new Exception("Failed getting key '" + key + "' from CustomDict.");
            }

            set
            {
                base[key] = value;
            }
        }

    }
}
