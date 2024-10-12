using Entities;
using StoreDataManager;

namespace QueryProcessor.Operations
{
    internal class SetDatabase
    {
        internal OperationStatus Execute()
        {
            return Store.GetInstance().SetDatabase();
        }
    }
}
