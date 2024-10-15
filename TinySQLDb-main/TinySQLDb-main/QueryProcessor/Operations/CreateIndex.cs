using Entities;
using StoreDataManager;

namespace QueryProcessor.Operations
{
	internal class CreateIndex
	{
		internal OperationStatus Execute(string sentence)
		{
			return Store.GetInstance().CreateIndex(sentence);
		}
	}
}
