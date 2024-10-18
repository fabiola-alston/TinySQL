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
        private string? currentDatabase; // Variable global para almacenar la base de datos seteada

        public static Store GetInstance()
        {
            lock (_lock)
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
        private const string SystemDatabasesFile = $@"{SystemCatalogPath}\SystemDatabases.bin";
        private const string SystemTablesFile = $@"{SystemCatalogPath}\SystemTables.bin";
        private const string SystemColumnsFile = $@"{SystemCatalogPath}\SystemColumns.bin";
        private const string SystemIndexesFile = $@"{SystemCatalogPath}\SystemIndexes.bin";

        public Store()
        {
            this.InitializeSystemCatalog();
        }

        private void InitializeSystemCatalog()
        {
            Directory.CreateDirectory(SystemCatalogPath);

            // Si el archivo de bases de datos no existe, se crea
            if (!File.Exists(SystemDatabasesFile))
            {
                using (var fs = File.Create(SystemDatabasesFile)) { }
            }
        }

        public OperationStatus CreateDatabase(string sentence)
        {
            string pattern = @"^CREATE\s+DATABASE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*;";
            Match match = Regex.Match(sentence, pattern);

            if (match.Success)
            {
                string databaseName = match.Groups[1].Value;

                // Verificar si la base de datos ya existe
                if (DatabaseExists(databaseName))
                {
                    Console.WriteLine($"La base de datos '{databaseName}' ya existe.");
                    return OperationStatus.Success; // No arrojar error si la base de datos ya existe
                }

                // Crear la base de datos en el archivo binario
                int newId = GetNextDatabaseId();

                // Escribir el nuevo ID y nombre de la base de datos en binario
                using (BinaryWriter writer = new BinaryWriter(File.Open(SystemDatabasesFile, FileMode.Append)))
                {
                    writer.Write(newId); // Escribe el ID en binario
                    writer.Write(databaseName); // Escribe el nombre de la base de datos como string en binario
                    writer.Write((byte)'\n'); // Agrega un salto de línea (nueva línea) en binario
                }

                Console.WriteLine($"Base de datos '{databaseName}' creada con ID {newId}.");
            }
            else
            {
                Console.WriteLine("Error al analizar la sentencia.");
                return OperationStatus.Error;
            }

            return OperationStatus.Success;
        }

        private bool DatabaseExists(string databaseName)
        {
            if (File.Exists(SystemDatabasesFile))
            {
                using (BinaryReader reader = new BinaryReader(File.Open(SystemDatabasesFile, FileMode.Open)))
                {
                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        reader.ReadInt32(); // Leer el ID (sin usarlo aquí)
                        string dbName = reader.ReadString();
                        reader.ReadByte(); // Leer el byte del salto de línea

                        if (dbName.Equals(databaseName)) // Comparación literal sin ignorar mayúsculas/minúsculas
                        {
                            return true; // La base de datos ya existe
                        }
                    }
                }
            }
            return false; // La base de datos no existe
        }



        private int GetNextDatabaseId()
        {
            int nextId = 1;

            if (File.Exists(SystemDatabasesFile))
            {
                using (BinaryReader reader = new BinaryReader(File.Open(SystemDatabasesFile, FileMode.Open)))
                {
                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        int id = reader.ReadInt32();
                        string dbName = reader.ReadString();
                        reader.ReadByte(); // Leer el byte del salto de línea
                        nextId = id + 1; // El ID será el último leído más 1
                    }
                }
            }

            return nextId;
        }


        public OperationStatus SetDatabase(string sentence)
        {
            string pattern = @"^SET\s+DATABASE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*;";
            Match match = Regex.Match(sentence, pattern);
            if (match.Success)
            {
                string databaseName = match.Groups[1].Value;

                // Verificar si la base de datos existe
                if (!DatabaseExists(databaseName))
                {
                    Console.WriteLine($"La base de datos '{databaseName}' no existe.");
                    return OperationStatus.Error; // Error si la base de datos no existe
                }

                // Establecer la base de datos actual
                currentDatabase = databaseName;
                Console.WriteLine($"Base de datos '{currentDatabase}' seteada correctamente.");
                return OperationStatus.Success;
            }
            else
            {
                Console.WriteLine("Error al analizar la sentencia.");
                return OperationStatus.Error;
            }
        }
        
        public OperationStatus CreateTable(string sentence)
        {
            string pattern = @"^CREATE\s+TABLE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(\s*([a-zA-Z_][a-zA-Z0-9_]*\s+(?:INTEGER|DOUBLE|VARCHAR\(\d+\)|DATETIME)\s*(?:,\s*[a-zA-Z_][a-zA-Z0-9_]*\s+(?:INTEGER|DOUBLE|VARCHAR\(\d+\)|DATETIME)\s*)*)\)\s*;?\s*$";

            Match match = Regex.Match(sentence, pattern, RegexOptions.Singleline);

            if (match.Success)
            {
                string tableName = match.Groups[1].Value; // Nombre de la tabla
                string columns = match.Groups[2].Value;   // Definición de las columnas

                Console.WriteLine($"Tabla: {tableName}");

                // Separar las columnas por coma
                string[] columnDefinitions = columns.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string columnDef in columnDefinitions)
                {
                    string columnPattern = @"([a-zA-Z_][a-zA-Z0-9_]*)\s+(INTEGER|DOUBLE|VARCHAR\(\d+\)|DATETIME)";
                    Match columnMatch = Regex.Match(columnDef.Trim(), columnPattern);

                    if (columnMatch.Success)
                    {
                        string columnName = columnMatch.Groups[1].Value; // nombre de variable
                        string columnType = columnMatch.Groups[2].Value; // tipo de variable

                        Console.WriteLine($"Columna: {columnName}, Tipo: {columnType}");

                        switch (columnType)
                        {
                            case "INTEGER":
                                // aqui va lo que pasa cuando es INTEGER
                                break;

                            case "DOUBLE":
                                // aqui va lo que pasa cuando es DOUBLE
                                break;

                            case string s when s.StartsWith("VARCHAR"):
                                int varcharLength = int.Parse(Regex.Match(s, @"\d+").Value); // tamano del VARCHAR !!
                                Console.WriteLine("VARCHAR SIZE: " + varcharLength);
                                // aqui va lo que pasa cuando es VARCHAR(x) 
                                break;

                            case "DATETIME":
                                // aqui va lo que pasa cuando es DATETIME
                                break;
                        }
                    }
                }

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
