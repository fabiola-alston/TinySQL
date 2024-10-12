using Entities;
using StoreDataManager;

namespace QueryProcessor.Operations
{
	internal class CreateIndex
	{
		internal OperationStatus Execute()
		{
			return Store.GetInstance().CreateIndex();
		}
	}
}
