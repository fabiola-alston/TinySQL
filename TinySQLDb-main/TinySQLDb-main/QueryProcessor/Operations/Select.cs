using Entities;
using StoreDataManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryProcessor.Operations
{
    internal class Select
    {
        public OperationStatus Execute(string sentence)
        {
            return Store.GetInstance().Select(sentence);
        }
    }
}
