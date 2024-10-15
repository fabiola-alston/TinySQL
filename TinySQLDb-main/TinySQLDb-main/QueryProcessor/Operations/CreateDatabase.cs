using Entities;
using StoreDataManager;

namespace QueryProcessor.Operations
{
    internal class CreateDatabase
    {
        internal OperationStatus Execute(string sentence)
        {
            return Store.GetInstance().CreateDatabase(sentence);
        }
    }
}
