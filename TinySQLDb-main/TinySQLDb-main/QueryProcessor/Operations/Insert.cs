using Entities;
using StoreDataManager;

namespace QueryProcessor.Operations
{
    internal class Insert
    {
        internal OperationStatus Execute(string sentence)
        {
            return Store.GetInstance().Insert(sentence);
        }
    }
}
