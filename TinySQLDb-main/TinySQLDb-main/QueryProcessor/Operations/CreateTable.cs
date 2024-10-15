using Entities;
using StoreDataManager;

namespace QueryProcessor.Operations
{
    internal class CreateTable
    {
        internal OperationStatus Execute(string sentence)
        {
            return Store.GetInstance().CreateTable(sentence);
        }
    }
}
