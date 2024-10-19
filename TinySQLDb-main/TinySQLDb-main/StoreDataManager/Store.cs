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
            if (!File.Exists(SystemTablesFile))
            {
                using (var fs = File.Create(SystemTablesFile)) { }
            }
            if (!File.Exists(SystemColumnsFile))
            {
                using (var fs = File.Create(SystemColumnsFile)) { }
            }
        }

        public OperationStatus CreateDatabase(string sentence)
        {
            string pattern = @"^CREATE\s+DATABASE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*";
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
            string pattern = @"^SET\s+DATABASE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*";
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

                List<ColumnMetadata> columnList = new List<ColumnMetadata>();

                foreach (string columnDef in columnDefinitions)
                {
                    string columnPattern = @"([a-zA-Z_][a-zA-Z0-9_]*)\s+(INTEGER|DOUBLE|VARCHAR\(\d+\)|DATETIME)";
                    Match columnMatch = Regex.Match(columnDef.Trim(), columnPattern);

                    if (columnMatch.Success)
                    {
                        string columnName = columnMatch.Groups[1].Value; // nombre de variable
                        string columnType = columnMatch.Groups[2].Value; // tipo de variable
                        int sizeInBytes = GetColumnSize(columnType);

                        Console.WriteLine($"Columna: {columnName}, Tipo: {columnType}, Tamaño: {sizeInBytes} bytes");

                        // Agregar la columna a la lista de metadatos
                        columnList.Add(new ColumnMetadata
                        {
                            ColumnName = columnName,
                            ColumnType = columnType,
                            SizeInBytes = sizeInBytes
                        });
                    }
                }

                // Registrar la tabla en el catálogo del sistema
                WriteTableToSystemCatalog(tableName, columnList);

            }
            else
            {
                Console.WriteLine("Error al analizar la sentencia.");
            }

            return OperationStatus.Success;
        }

        private int GetColumnSize(string columnType)
        {
            if (columnType.StartsWith("VARCHAR"))
            {
                int varcharLength = int.Parse(Regex.Match(columnType, @"\d+").Value); // tamaño de VARCHAR
                return varcharLength; // El tamaño en bytes es el tamaño del VARCHAR
            }

            return columnType switch
            {
                "INTEGER" => 4,  // 4 bytes para INTEGER
                "DOUBLE" => 8,   // 8 bytes para DOUBLE
                "DATETIME" => 17, // 10 bytes para formato YYYY-MM-DD
                _ => throw new InvalidOperationException("Tipo de columna no soportado")
            };
        }

        private void WriteTableToSystemCatalog(string tableName, List<ColumnMetadata> columns)
        {
            // Escribir la tabla en el archivo binario del sistema
            using (var fs = new FileStream(SystemTablesFile, FileMode.Append))
            {
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(currentDatabase);
                    bw.Write(tableName);
                    bw.Write(columns.Count);

                    foreach (var column in columns)
                    {
                        bw.Write(column.ColumnName);
                        bw.Write(column.ColumnType);
                        bw.Write(column.SizeInBytes);
                    }

                    bw.Write((byte)'\n'); // Agrega un salto de línea (nueva línea) en binario
                }
            }

            Console.WriteLine($"Tabla '{tableName}' registrada con {columns.Count} columnas en la base de datos {currentDatabase}.");
        }

        public OperationStatus DropTable(string sentence)
        {
            string pattern = @"^DROP\s+TABLE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*";
            Match match = Regex.Match(sentence, pattern);

            if (match.Success)
            {
                string tableName = match.Groups[1].Value;
                Console.WriteLine($"Drop table: {tableName}");
                // Verificar si la tabla existe en SystemTablesFile
                List<ColumnMetadata> tableInfo = ReadTableFromSystemCatalog(tableName);
                if (tableInfo == null)
                {
                    Console.WriteLine($"Error: La tabla '{tableName}' no existe en la base de datos {currentDatabase}.");
                    return OperationStatus.Error;
                }

                // Verificar si hay registros de esa tabla en SystemColumnsFile
                if (TableHasColumns(tableName))
                {
                    Console.WriteLine($"Error: La tabla '{tableName}' no se puede eliminar porque no está vacía.");
                    return OperationStatus.Error;
                }

                // Eliminar la tabla de SystemTablesFile
                DeleteTableFromSystemCatalog(tableName);
                Console.WriteLine($"Tabla '{tableName}' eliminada exitosamente de la base de datos {currentDatabase}.");
                return OperationStatus.Success;
            }
            else
            {
                Console.WriteLine("Error: Sintaxis incorrecta para el comando DROP TABLE.");
                return OperationStatus.Error;
            }
        }

        // Método para verificar si una tabla tiene columnas en SystemColumnsFile
        private bool TableHasColumns(string tableName)
        {
            using (var fs = new FileStream(SystemColumnsFile, FileMode.Open))
            {
                using (var br = new BinaryReader(fs))
                {
                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        try
                        {
                            // Leer el nombre de la tabla
                            string tblName = br.ReadString();

                            if (tblName == tableName)
                            {
                                // Si encontramos la tabla, es que tiene registros
                                return true;
                            }

                            // Saltar las columnas asociadas a esa tabla
                            while (br.BaseStream.Position < br.BaseStream.Length)
                            {
                                string columnName = br.ReadString(); // Leer el nombre de la columna
                                string columnValue = br.ReadString(); // Leer el valor de la columna

                                // Si encontramos un salto de línea, significa que hemos llegado al final del registro
                                if (br.BaseStream.Position < br.BaseStream.Length)
                                {
                                    byte nextByte = br.ReadByte();
                                    if (nextByte == (byte)'\n')
                                    {
                                        break; // Fin del registro actual
                                    }
                                    else
                                    {
                                        // Si no es un salto de línea, retroceder el puntero porque no era un salto de línea
                                        br.BaseStream.Seek(-1, SeekOrigin.Current);
                                    }
                                }
                            }
                        }
                        catch (EndOfStreamException)
                        {
                            Console.WriteLine("Error: Se intentó leer más allá del final del archivo SystemColumnsFile.");
                            break; // Sal del ciclo si llegamos al final inesperadamente
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine($"Error de IO al leer SystemColumnsFile: {ex.Message}");
                            break;
                        }
                    }
                }
            }

            // Si no se encontraron registros para la tabla, significa que está vacía
            return false;
        }




        // Método para eliminar la tabla de SystemTablesFile
        private void DeleteTableFromSystemCatalog(string tableName)
        {
            string tempFile = Path.GetTempFileName(); // Crear un archivo temporal para almacenar las tablas restantes

            using (var fs = new FileStream(SystemTablesFile, FileMode.Open))
            {
                using (var br = new BinaryReader(fs))
                {
                    using (var tempFs = new FileStream(tempFile, FileMode.Create))
                    {
                        using (var bw = new BinaryWriter(tempFs))
                        {
                            while (br.BaseStream.Position < br.BaseStream.Length)
                            {
                                string db = br.ReadString();
                                string tbl = br.ReadString();
                                int colCount = br.ReadInt32();

                                // Si esta tabla no es la que queremos eliminar, escribirla en el archivo temporal
                                if (tbl != tableName || db != currentDatabase)
                                {
                                    bw.Write(db);
                                    bw.Write(tbl);
                                    bw.Write(colCount);

                                    // Copiar las columnas de esta tabla al archivo temporal
                                    for (int i = 0; i < colCount; i++)
                                    {
                                        bw.Write(br.ReadString()); // nombre de columna
                                        bw.Write(br.ReadString()); // tipo de columna
                                        bw.Write(br.ReadInt32());  // tamaño de columna
                                    }

                                    // Leer y escribir el salto de línea (fin de registro)
                                    if (br.BaseStream.Position < br.BaseStream.Length)
                                    {
                                        bw.Write(br.ReadByte());
                                    }
                                }
                                else
                                {
                                    // Saltar las columnas de la tabla que queremos eliminar
                                    for (int i = 0; i < colCount; i++)
                                    {
                                        br.ReadString(); // saltar nombre de columna
                                        br.ReadString(); // saltar tipo de columna
                                        br.ReadInt32();  // saltar tamaño de columna
                                    }

                                    // Saltar el salto de línea (fin de registro)
                                    if (br.BaseStream.Position < br.BaseStream.Length)
                                    {
                                        br.ReadByte();
                                    }
                                    Console.WriteLine($"Tabla '{tableName}' eliminada del catálogo del sistema.");
                                }
                            }
                        }
                    }
                }
            }

            // Reemplazar el archivo original por el archivo temporal
            File.Delete(SystemTablesFile);
            File.Move(tempFile, SystemTablesFile);
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
            string pattern = @"^INSERT\s+INTO\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\((.+)\)\s*";
            Match match = Regex.Match(sentence, pattern);

            if (match.Success)
            {
                string tableName = match.Groups[1].Value; // Nombre de la tabla
                string values = match.Groups[2].Value;    // Valores a insertar

                Console.WriteLine($"Insertando en tabla: {tableName} los valores: {values}");

                // Validar que la tabla exista y obtener la estructura de las columnas
                List<ColumnMetadata> columns = ReadTableFromSystemCatalog(tableName);

                if (columns == null)
                {
                    Console.WriteLine($"Error: La tabla '{tableName}' no existe.");
                    return OperationStatus.Error;
                }

                // Separar los valores de la sentencia
                string[] valueList = values.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (valueList.Length != columns.Count)
                {
                    Console.WriteLine($"Error: El número de valores no coincide con el número de columnas.");
                    return OperationStatus.Error;
                }

                // Validar y escribir cada valor
                for (int i = 0; i < columns.Count; i++)
                {
                    string value = valueList[i].Trim().Trim('"'); // Eliminar espacios y comillas
                    ColumnMetadata column = columns[i];

                    // Validar el tipo y tamaño del valor
                    if (!ValidateValue(column, value))
                    {
                        Console.WriteLine($"Error: El valor '{value}' excede el tamaño permitido o no coincide con el tipo.");
                        return OperationStatus.Error;
                    }
                }

                // Si todo es válido, escribir los valores en el archivo de columnas
                WriteValuesToSystemColumns(tableName, columns, valueList);
            }
            else
            {
                Console.WriteLine("Error: Sintaxis incorrecta para el comando INSERT.");
                return OperationStatus.Error;
            }

            return OperationStatus.Success;
        }

        private List<ColumnMetadata> ReadTableFromSystemCatalog(string tableName)
        {
            // Leer el catálogo de tablas y buscar la tabla especificada
            List<ColumnMetadata> columns = new List<ColumnMetadata>();

            using (var fs = new FileStream(SystemTablesFile, FileMode.Open))
            {
                using (var br = new BinaryReader(fs))
                {
                    while (br.BaseStream.Position < br.BaseStream.Length) // Evitar leer más allá del archivo
                    {
                        try
                        {
                            string db = br.ReadString();
                            string tbl = br.ReadString();
                            int colCount = br.ReadInt32();

                            if (tbl == tableName && db == currentDatabase)
                            {
                                for (int i = 0; i < colCount; i++)
                                {
                                    string columnName = br.ReadString();
                                    string columnType = br.ReadString();
                                    int sizeInBytes = br.ReadInt32();
                                    columns.Add(new ColumnMetadata { ColumnName = columnName, ColumnType = columnType, SizeInBytes = sizeInBytes });
                                }
                                // Leer el salto de línea después de las columnas
                                br.ReadByte();
                                return columns;
                            }
                            else
                            {
                                // Saltar el contenido de columnas si no es la tabla
                                for (int i = 0; i < colCount; i++)
                                {
                                    br.ReadString(); // Leer el nombre de la columna
                                    br.ReadString(); // Leer el tipo de la columna
                                    br.ReadInt32();  // Leer el tamaño de la columna
                                }
                                // Leer el salto de línea después de las columnas
                                br.ReadByte();
                            }
                        }
                        catch (EndOfStreamException)
                        {
                            Console.WriteLine("Error: Llegaste al final del archivo antes de tiempo.");
                            break; // Sal del ciclo si llegas al final del archivo inesperadamente.
                        }
                    }
                }
            }
            return null; // La tabla no se encontró
        }


        private bool ValidateValue(ColumnMetadata column, string value)
        {
            switch (column.ColumnType)
            {
                case "INTEGER":
                    if (!int.TryParse(value, out _))
                        return false;
                    break;

                case "DOUBLE":
                    if (!double.TryParse(value, out _))
                        return false;
                    break;

                case string s when s.StartsWith("VARCHAR"):
                    if (value.Length > column.SizeInBytes)
                        return false; // Excede el tamaño permitido
                    break;

                case "DATETIME":
                    if (!DateTime.TryParse(value, out _))
                        return false;
                    break;

                default:
                    return false; // Tipo no soportado
            }
            return true;
        }

        private void WriteValuesToSystemColumns(string tableName, List<ColumnMetadata> columns, string[] values)
        {
            using (var fs = new FileStream(SystemColumnsFile, FileMode.Append))
            {
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(tableName);

                    for (int i = 0; i < columns.Count; i++)
                    {
                        string value = values[i].Trim().Trim('"'); // Eliminar espacios y comillas
                        bw.Write(columns[i].ColumnName);
                        bw.Write(value); // Escribir el valor en el archivo binario
                    }

                    bw.Write((byte)'\n'); // Agregar una nueva línea
                }
            }

            Console.WriteLine($"Valores insertados en la tabla '{tableName}' en la base de datos {currentDatabase}.");
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

    public class ColumnMetadata
    {
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
        public int SizeInBytes { get; set; }
    }
}
