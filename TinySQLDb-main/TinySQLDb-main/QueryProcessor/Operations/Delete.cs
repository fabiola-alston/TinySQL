using Entities;
using StoreDataManager;

namespace QueryProcessor.Operations
{
    internal class Delete
    {
        internal OperationStatus Execute(string sentence)
        {
            return Store.GetInstance().Delete(sentence);
        }
    }
}
