using Entities;
using StoreDataManager;

namespace QueryProcessor.Operations
{
    internal class SetDatabase
    {
        internal OperationStatus Execute(string sentence)
        {
            return Store.GetInstance().SetDatabase(sentence);
        }
    }
}
