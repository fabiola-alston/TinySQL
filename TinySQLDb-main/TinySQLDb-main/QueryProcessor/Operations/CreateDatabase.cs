using Entities;
using StoreDataManager;

namespace QueryProcessor.Operations
{
    internal class CreateDatabase
    {
        internal OperationStatus Execute()
        {
            return Store.GetInstance().CreateDatabase();
        }
    }
}
