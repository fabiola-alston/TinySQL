using Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

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
        private const string SystemColumnsFile = $@"{SystemCatalogPath}\SystemColumns.table";
        private const string SystemIndexesFile = $@"{SystemCatalogPath}\SystemIndexes.table";

        public Store()
        {
            this.InitializeSystemCatalog();
            
        }

        private void InitializeSystemCatalog()
        {
            Directory.CreateDirectory(SystemCatalogPath);
        }

        public OperationStatus CreateDatabase(string sentence)
        {
            string pattern = @"^CREATE\s+DATABASE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*";
            Match match = Regex.Match(sentence, pattern);

            if (match.Success)
            {
                string databaseName = match.Groups[1].Value;
                Console.WriteLine(databaseName);
            }
            else
            {
                Console.WriteLine("Error");
            }

            return OperationStatus.Success;
        }


        public OperationStatus SetDatabase(string sentence)
        {
            string pattern = @"^SET\s+DATABASE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*";
            Match match = Regex.Match(sentence, pattern);
            if (match.Success)
            {
                string databaseName = match.Groups[1].Value;
                Console.WriteLine(databaseName);
            }
            else
            {
                Console.WriteLine("Error");
            }

            return OperationStatus.Success;
        }

        public OperationStatus CreateTable(string sentence)
        {
            string pattern = @"^CREATE\s+TABLE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(\s*([a-zA-Z_][a-zA-Z0-9_]*\s+(?:INTEGER|DOUBLE|VARCHAR\(\d+\)|DATETIME)\s*(?:,\s*[a-zA-Z_][a-zA-Z0-9_]*\s+(?:INTEGER|DOUBLE|VARCHAR\(\d+\)|DATETIME)\s*)*)\)\s*;?\s*$";

            Match match = Regex.Match(sentence, pattern, RegexOptions.Singleline);

            if (match.Success)
            {
                string tableName = match.Groups[1].Value;
                Console.WriteLine(tableName);

                string columnNames = match.Groups[2].Value;
                Console.WriteLine(columnNames);

            }
            else
            {
                Console.WriteLine("Error");
            }

            return OperationStatus.Success;
        }

        public OperationStatus CreateIndex(string sentence)
        {
            return OperationStatus.Success;
        }


        public OperationStatus Select(string sentence)
        {
            return OperationStatus.Success;
        }

        public OperationStatus Insert(string sentence)
        {
            return OperationStatus.Success;
        }

        public OperationStatus Update(string sentence)
        {
            return OperationStatus.Success;
        }
        public OperationStatus Delete(string sentence)
        {
            return OperationStatus.Success;
        }
    }
}
