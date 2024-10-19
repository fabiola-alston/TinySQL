using Entities;
using StoreDataManager;

namespace QueryProcessor.Operations
{
    internal class DropTable
    {
        internal OperationStatus Execute(string sentence)
        {
            return Store.GetInstance().DropTable(sentence);
        }
    }
}
