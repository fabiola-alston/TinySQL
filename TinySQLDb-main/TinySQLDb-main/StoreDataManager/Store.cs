using Entities;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace StoreDataManager
{
    public sealed class Store
    {
        private static Store? instance = null;
        private static readonly object _lock = new object();
               
        public static Store GetInstance()
        {
            lock(_lock)
            {
                if (instance == null) 
                {
                    instance = new Store();
                }
                return instance;
            }
        }

        private const string DatabaseBasePath = @"C:\TinySql\";
        private const string DataPath = $@"{DatabaseBasePath}\Data";
        private const string SystemCatalogPath = $@"{DataPath}\SystemCatalog";
        private const string SystemDatabasesFile = $@"{SystemCatalogPath}\SystemDatabases.table";
        private const string SystemTablesFile = $@"{SystemCatalogPath}\SystemTables.table";

        public Store()
        {
            this.InitializeSystemCatalog();
            
        }

        private void InitializeSystemCatalog()
        {
            Directory.CreateDirectory(SystemCatalogPath);
        }

        public OperationStatus CreateDatabase()
        {
            return OperationStatus.Success;
        }


        public OperationStatus SetDatabase()
        {
            return OperationStatus.Success;
        }

        public OperationStatus CreateTable()
        {
            return OperationStatus.Success;
        }

        public OperationStatus CreateIndex()
        {
            return OperationStatus.Success;
        }


        public OperationStatus Select()
        {
            return OperationStatus.Success;
        }

        public OperationStatus Insert()
        {
            return OperationStatus.Success;
        }

        public OperationStatus Update()
        {
            return OperationStatus.Success;
        }
        public OperationStatus Delete()
        {
            return OperationStatus.Success;
        }
    }
}
