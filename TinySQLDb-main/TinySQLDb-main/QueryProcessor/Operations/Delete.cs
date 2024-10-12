using Entities;
using StoreDataManager;

namespace QueryProcessor.Operations
{
    internal class Delete
    {
        internal OperationStatus Execute()
        {
            return Store.GetInstance().Delete();
        }
    }
}
