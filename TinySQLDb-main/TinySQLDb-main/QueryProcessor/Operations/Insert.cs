﻿using Entities;
using StoreDataManager;

namespace QueryProcessor.Operations
{
    internal class Insert
    {
        internal OperationStatus Execute()
        {
            return Store.GetInstance().Insert();
        }
    }
}
