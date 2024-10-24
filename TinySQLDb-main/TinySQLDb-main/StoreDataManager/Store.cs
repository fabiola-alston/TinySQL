using System.Diagnostics;
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
        private readonly ConsoleHelper consoleHelper; // Instancia de ConsoleHelper

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
            consoleHelper = new ConsoleHelper();  // Instanciar ConsoleHelper una vez
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
            // Iniciar el temporizador
            var stopwatch = Stopwatch.StartNew();
            consoleHelper.PrintStartTimer("Iniciando creación de la base de datos...");

            string pattern = @"^CREATE\s+DATABASE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*";
            Match match = Regex.Match(sentence, pattern);

            if (match.Success)
            {
                string databaseName = match.Groups[1].Value;

                // Imprimir la información capturada con PrintInfo
                consoleHelper.PrintInfo($"Base de datos que se quiere crear: {databaseName}");

                // Verificar si la base de datos ya existe
                if (DatabaseExists(databaseName))
                {
                    consoleHelper.PrintError($"La base de datos '{databaseName}' ya existe.");
                    stopwatch.Stop();
                    consoleHelper.PrintStopTimer($"Operación finalizada en {stopwatch.ElapsedMilliseconds} ms.");
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

                consoleHelper.PrintSuccess($"Base de datos '{databaseName}' creada con ID {newId}.");
            }
            else
            {
                consoleHelper.PrintError("Error al analizar la sentencia.");
                stopwatch.Stop();
                consoleHelper.PrintStopTimer($"Operación finalizada en {stopwatch.ElapsedMilliseconds} ms.");
                return OperationStatus.Error;
            }

            stopwatch.Stop();
            consoleHelper.PrintStopTimer($"Operación finalizada en {stopwatch.ElapsedMilliseconds} ms.");
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
            // Iniciar el temporizador
            var stopwatch = Stopwatch.StartNew();
            consoleHelper.PrintStartTimer("Iniciando la operación de seteo de la base de datos...");

            string pattern = @"^SET\s+DATABASE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*";
            Match match = Regex.Match(sentence, pattern);
            if (match.Success)
            {
                string databaseName = match.Groups[1].Value;

                // Verificar si la base de datos existe
                if (!DatabaseExists(databaseName))
                {
                    consoleHelper.PrintError($"La base de datos '{databaseName}' no existe.");

                    // Detener el temporizador y mostrar el tiempo final
                    stopwatch.Stop();
                    consoleHelper.PrintStopTimer($"Operación finalizada en {stopwatch.ElapsedMilliseconds} ms.");

                    return OperationStatus.Error; // Error si la base de datos no existe
                }

                // Establecer la base de datos actual
                currentDatabase = databaseName;
                consoleHelper.PrintSuccess($"Base de datos '{currentDatabase}' seteada correctamente.");

                // Detener el temporizador y mostrar el tiempo final
                stopwatch.Stop();
                consoleHelper.PrintStopTimer($"Operación finalizada en {stopwatch.ElapsedMilliseconds} ms.");

                return OperationStatus.Success;
            }
            else
            {
                consoleHelper.PrintError("Error al analizar la sentencia.");

                // Detener el temporizador y mostrar el tiempo final
                stopwatch.Stop();
                consoleHelper.PrintStopTimer($"Operación finalizada en {stopwatch.ElapsedMilliseconds} ms.");

                return OperationStatus.Error;
            }
        }

        public OperationStatus CreateTable(string sentence)
        {
            // Iniciar el temporizador
            var stopwatch = Stopwatch.StartNew();
            consoleHelper.PrintStartTimer("Iniciando la creación de la tabla...");

            string pattern = @"^CREATE\s+TABLE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(\s*([a-zA-Z_][a-zA-Z0-9_]*\s+(?:INTEGER|DOUBLE|VARCHAR\(\d+\)|DATETIME)\s*(?:,\s*[a-zA-Z_][a-zA-Z0-9_]*\s+(?:INTEGER|DOUBLE|VARCHAR\(\d+\)|DATETIME)\s*)*)\)\s*;?\s*$";

            Match match = Regex.Match(sentence, pattern, RegexOptions.Singleline);

            if (match.Success)
            {
                string tableName = match.Groups[1].Value; // Nombre de la tabla
                string columns = match.Groups[2].Value;   // Definición de las columnas

                consoleHelper.PrintInfo($"Tabla: {tableName}");

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

                        consoleHelper.PrintInfo($"Columna: {columnName}, Tipo: {columnType}, Tamaño: {sizeInBytes} bytes");

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

                // Detener el temporizador y mostrar el tiempo final
                stopwatch.Stop();
                consoleHelper.PrintStopTimer($"Creación de la tabla completada en {stopwatch.ElapsedMilliseconds} ms.");
            }
            else
            {
                consoleHelper.PrintError("Error al analizar la sentencia CREATE TABLE.");

                // Detener el temporizador y mostrar el tiempo final
                stopwatch.Stop();
                consoleHelper.PrintStopTimer($"Operación finalizada en {stopwatch.ElapsedMilliseconds} ms.");
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

            consoleHelper.PrintSuccess($"Tabla '{tableName}' registrada con {columns.Count} columnas en la base de datos {currentDatabase}.");
        }

        public OperationStatus DropTable(string sentence)
        {
            // Iniciar el temporizador
            var stopwatch = Stopwatch.StartNew();
            consoleHelper.PrintStartTimer("Iniciando eliminación de la tabla...");

            string pattern = @"^DROP\s+TABLE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*";
            Match match = Regex.Match(sentence, pattern);

            if (match.Success)
            {
                string tableName = match.Groups[1].Value;
                consoleHelper.PrintInfo($"Drop table: {tableName}");

                // Verificar si la tabla existe en SystemTablesFile
                List<ColumnMetadata> tableInfo = ReadTableFromSystemCatalog(tableName);
                if (tableInfo == null)
                {
                    consoleHelper.PrintError($"Error: La tabla '{tableName}' no existe en la base de datos {currentDatabase}.");

                    // Detener el temporizador y mostrar el tiempo final
                    stopwatch.Stop();
                    consoleHelper.PrintStopTimer($"Operación finalizada en {stopwatch.ElapsedMilliseconds} ms.");

                    return OperationStatus.Error;
                }

                // Verificar si hay registros de esa tabla en SystemColumnsFile
                if (TableHasColumns(tableName))
                {
                    consoleHelper.PrintError($"Error: La tabla '{tableName}' no se puede eliminar porque no está vacía.");

                    // Detener el temporizador y mostrar el tiempo final
                    stopwatch.Stop();
                    consoleHelper.PrintStopTimer($"Operación finalizada en {stopwatch.ElapsedMilliseconds} ms.");

                    return OperationStatus.Error;
                }

                // Eliminar la tabla de SystemTablesFile
                DeleteTableFromSystemCatalog(tableName);
                consoleHelper.PrintSuccess($"Tabla '{tableName}' eliminada exitosamente de la base de datos {currentDatabase}.");

                // Detener el temporizador y mostrar el tiempo final
                stopwatch.Stop();
                consoleHelper.PrintStopTimer($"Eliminación de la tabla completada en {stopwatch.ElapsedMilliseconds} ms.");

                return OperationStatus.Success;
            }
            else
            {
                consoleHelper.PrintError("Error: Sintaxis incorrecta para el comando DROP TABLE.");

                // Detener el temporizador y mostrar el tiempo final
                stopwatch.Stop();
                consoleHelper.PrintStopTimer($"Operación finalizada en {stopwatch.ElapsedMilliseconds} ms.");

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
                            consoleHelper.PrintError("Error: Se intentó leer más allá del final del archivo SystemColumnsFile.");
                            break; // Sal del ciclo si llegamos al final inesperadamente
                        }
                        catch (IOException ex)
                        {
                            consoleHelper.PrintError($"Error de IO al leer SystemColumnsFile: {ex.Message}");
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
                                    consoleHelper.PrintSuccess($"Tabla '{tableName}' eliminada del catálogo del sistema.");
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
            consoleHelper.PrintError("No ha sido implementado el proceso de esta sentencia");
            return OperationStatus.Error;
        }

        public OperationStatus Select(string sentence)
        {
            var selectPattern = @"SELECT\s+(?<columns>[\*\w,\s]+)\s+FROM\s+(?<table>\w+)(\s+WHERE\s+(?<where>[\w\s><=]+))?(\s+ORDER\s+BY\s+(?<orderBy>\w+)\s+(?<orderDirection>asc|desc))?";
            var match = Regex.Match(sentence, selectPattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                // Extraer las variables de la sentencia SQL
                var columns = match.Groups["columns"].Value;
                var tableName = match.Groups["table"].Value;
                var whereClause = match.Groups["where"].Value;
                var orderBy = match.Groups["orderBy"].Value;
                var orderDirection = match.Groups["orderDirection"].Value;

                Console.WriteLine($"Columns: {columns}");
                Console.WriteLine($"Table: {tableName}");
                if (!string.IsNullOrEmpty(whereClause))
                {
                    Console.WriteLine($"Where clause: {whereClause}");
                }
                if (!string.IsNullOrEmpty(orderBy))
                {
                    Console.WriteLine($"Order by: {orderBy} {orderDirection}");
                }
                return OperationStatus.Success;
            }
            else
            {
                consoleHelper.PrintError("Error en la sintaxis de la sentencia SELECT.");
                return OperationStatus.Error;
            }
        }


        // algoritmo quicksort, no hubo tiempo de implementarlo :(
        public class QuickSortAlgorithm
        {
            public static void QuickSort(int[] array, int low, int high)
            {
                if (low < high)
                {
                    int partitionIndex = Partition(array, low, high);

                    QuickSort(array, low, partitionIndex - 1); 
                    QuickSort(array, partitionIndex + 1, high);
                }
            }

            // Método de partición
            private static int Partition(int[] array, int low, int high)
            {
                int pivot = array[high];
                int i = (low - 1); 

                for (int j = low; j < high; j++)
                {
                   
                    if (array[j] <= pivot)
                    {
                        i++;
        
                        Swap(array, i, j);
                    }
                }

                Swap(array, i + 1, high);

                return i + 1; // Índice de partición
            }

            private static void Swap(int[] array, int a, int b)
            {
                int temp = array[a];
                array[a] = array[b];
                array[b] = temp;
            }

            public static void PrintArray(int[] array)
            {
                foreach (int t in array)
                {
                    Console.Write(t + " ");
                }
                Console.WriteLine();
            }

            public static void Main(string[] args)
            {
                int[] data = { 8, 7, 6, 1, 0, 9, 2 };
                Console.WriteLine("Array original:");
                PrintArray(data);

                QuickSort(data, 0, data.Length - 1);

                Console.WriteLine("Array ordenado:");
                PrintArray(data);
            }
        }

        public OperationStatus Insert(string sentence)
        {
            // Iniciar el temporizador
            var stopwatch = Stopwatch.StartNew();
            consoleHelper.PrintStartTimer("Iniciando inserción...");

            string pattern = @"^INSERT\s+INTO\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\((.+)\)\s*";
            Match match = Regex.Match(sentence, pattern);

            if (match.Success)
            {
                string tableName = match.Groups[1].Value; // Nombre de la tabla
                string values = match.Groups[2].Value;    // Valores a insertar

                consoleHelper.PrintInfo($"Insertando en tabla: {tableName} los valores: {values}");

                // Validar que la tabla exista y obtener la estructura de las columnas
                List<ColumnMetadata> columns = ReadTableFromSystemCatalog(tableName);

                if (columns == null)
                {
                    consoleHelper.PrintError($"Error: La tabla '{tableName}' no existe.");

                    // Detener el temporizador y mostrar el tiempo final
                    stopwatch.Stop();
                    consoleHelper.PrintStopTimer($"Inserción fallida en {stopwatch.ElapsedMilliseconds} ms.");

                    return OperationStatus.Error;
                }

                // Separar los valores de la sentencia
                string[] valueList = values.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (valueList.Length != columns.Count)
                {
                    consoleHelper.PrintError($"Error: El número de valores no coincide con el número de columnas.");

                    // Detener el temporizador y mostrar el tiempo final
                    stopwatch.Stop();
                    consoleHelper.PrintStopTimer($"Inserción fallida en {stopwatch.ElapsedMilliseconds} ms.");

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
                        consoleHelper.PrintError($"Error: El valor '{value}' excede el tamaño permitido o no coincide con el tipo.");

                        // Detener el temporizador y mostrar el tiempo final
                        stopwatch.Stop();
                        consoleHelper.PrintStopTimer($"Inserción fallida en {stopwatch.ElapsedMilliseconds} ms.");

                        return OperationStatus.Error;
                    }
                }

                // Si todo es válido, escribir los valores en el archivo de columnas
                WriteValuesToSystemColumns(tableName, columns, valueList);
            }
            else
            {
                consoleHelper.PrintError("Error: Sintaxis incorrecta para el comando INSERT INTO.");

                // Detener el temporizador y mostrar el tiempo final
                stopwatch.Stop();
                consoleHelper.PrintStopTimer($"Inserción fallida en {stopwatch.ElapsedMilliseconds} ms.");

                return OperationStatus.Error;
            }

            // Detener el temporizador y mostrar el tiempo final
            stopwatch.Stop();
            consoleHelper.PrintStopTimer($"Inserción completada en {stopwatch.ElapsedMilliseconds} ms.");

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
                            consoleHelper.PrintError("Error: Llegaste al final del archivo antes de tiempo.");
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

            consoleHelper.PrintSuccess($"Valores insertados en la tabla '{tableName}' en la base de datos {currentDatabase}.");
        }

        public OperationStatus Update(string sentence)
        {
            // Iniciar el temporizador
            var stopwatch = Stopwatch.StartNew();
            consoleHelper.PrintStartTimer("Iniciando actualización...");

            // Expresión regular para analizar la sentencia UPDATE
            string pattern = @"^UPDATE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+SET\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*\""?([a-zA-Z0-9_.:\-\s]+)\""?\s+WHERE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*\""?([a-zA-Z0-9_.:\-\s]+)\""?\s*;?$";
            Match match = Regex.Match(sentence, pattern);

            if (match.Success)
            {
                // Extraer la información de la sentencia
                string tableName = match.Groups[1].Value;  // Nombre de la tabla
                string columnToUpdate = match.Groups[2].Value;  // Columna a actualizar
                string newValue = match.Groups[3].Value;  // Nuevo valor
                string whereColumn = match.Groups[4].Value;  // Columna de la condición WHERE
                string whereValue = match.Groups[5].Value;  // Valor de la condición WHERE

                // Imprimir la información capturada
                consoleHelper.PrintInfo($"Tabla: {tableName}");
                consoleHelper.PrintInfo($"Columna a actualizar: {columnToUpdate}");
                consoleHelper.PrintInfo($"Nuevo valor: {newValue}");
                consoleHelper.PrintInfo($"Columna de condición WHERE: {whereColumn}");
                consoleHelper.PrintInfo($"Valor de la condición WHERE: {whereValue}");

                // Verificar si la tabla existe en la base de datos actual
                List<ColumnMetadata> columns = ReadTableFromSystemCatalog(tableName);
                if (columns == null)
                {
                    consoleHelper.PrintError($"Error: La tabla '{tableName}' no existe en la base de datos {currentDatabase}.");

                    // Detener el temporizador
                    stopwatch.Stop();
                    consoleHelper.PrintStopTimer($"Actualización fallida en {stopwatch.ElapsedMilliseconds} ms.");

                    return OperationStatus.Error;
                }

                // Verificar que las columnas SET y WHERE existan en la tabla
                bool columnToUpdateExists = columns.Any(c => c.ColumnName == columnToUpdate);
                bool whereColumnExists = columns.Any(c => c.ColumnName == whereColumn);

                if (!columnToUpdateExists)
                {
                    consoleHelper.PrintError($"Error: La columna '{columnToUpdate}' no existe en la tabla '{tableName}'.");

                    // Detener el temporizador
                    stopwatch.Stop();
                    consoleHelper.PrintStopTimer($"Actualización fallida en {stopwatch.ElapsedMilliseconds} ms.");

                    return OperationStatus.Error;
                }

                if (!whereColumnExists)
                {
                    consoleHelper.PrintError($"Error: La columna '{whereColumn}' no existe en la tabla '{tableName}'.");

                    // Detener el temporizador
                    stopwatch.Stop();
                    consoleHelper.PrintStopTimer($"Actualización fallida en {stopwatch.ElapsedMilliseconds} ms.");

                    return OperationStatus.Error;
                }

                // Actualizar los valores en el archivo SystemColumnsFile
                var result = UpdateRowsInSystemColumns(tableName, columnToUpdate, newValue, whereColumn, whereValue, columns);

                // Detener el temporizador y mostrar el tiempo final
                stopwatch.Stop();
                consoleHelper.PrintStopTimer($"Actualización completada en {stopwatch.ElapsedMilliseconds} ms.");

                return result;
            }
            else
            {
                consoleHelper.PrintError("Error: Sintaxis incorrecta para el comando UPDATE.");

                // Detener el temporizador
                stopwatch.Stop();
                consoleHelper.PrintStopTimer($"Actualización fallida en {stopwatch.ElapsedMilliseconds} ms.");

                return OperationStatus.Error;
            }
        }

        private OperationStatus UpdateRowsInSystemColumns(string tableName, string columnToUpdate, string newValue, string whereColumn, string whereValue, List<ColumnMetadata> columns)
        {
            string tempFile = Path.GetTempFileName();  // Crear un archivo temporal
            bool foundMatchingRows = false;

            using (var fs = new FileStream(SystemColumnsFile, FileMode.Open))
            {
                using (var br = new BinaryReader(fs))
                {
                    using (var tempFs = new FileStream(tempFile, FileMode.Create))
                    {
                        using (var bw = new BinaryWriter(tempFs))
                        {
                            while (br.BaseStream.Position < br.BaseStream.Length)
                            {
                                string tblName = br.ReadString();

                                if (tblName == tableName)
                                {
                                    // Leer la fila completa de la tabla
                                    bool matchFound = false;
                                    List<string> currentRowValues = new List<string>();

                                    // Leer los valores de las columnas
                                    for (int i = 0; i < columns.Count; i++)
                                    {
                                        string columnName = br.ReadString();
                                        string columnValue = br.ReadString();
                                        currentRowValues.Add(columnValue);

                                        // Verificar si la columna WHERE coincide
                                        if (columnName == whereColumn && columnValue == whereValue)
                                        {
                                            matchFound = true;
                                            foundMatchingRows = true;
                                        }
                                    }

                                    br.ReadByte();  // Leer el salto de línea

                                    // Si coincide la condición WHERE, actualizar el valor
                                    if (matchFound)
                                    {
                                        consoleHelper.PrintInfo($"Actualizando fila donde '{whereColumn}' = '{whereValue}' en la tabla '{tableName}'.");

                                        bw.Write(tblName);

                                        // Escribir las columnas actualizadas
                                        for (int i = 0; i < columns.Count; i++)
                                        {
                                            string columnName = columns[i].ColumnName;
                                            bw.Write(columnName);

                                            // Actualizar solo la columna correspondiente
                                            if (columnName == columnToUpdate)
                                            {
                                                bw.Write(newValue);  // Escribir el nuevo valor
                                            }
                                            else
                                            {
                                                bw.Write(currentRowValues[i]);  // Escribir el valor existente
                                            }
                                        }

                                        bw.Write((byte)'\n');  // Agregar salto de línea
                                    }
                                    else
                                    {
                                        // Si no coincide la condición, copiar la fila tal cual al archivo temporal
                                        bw.Write(tblName);

                                        for (int i = 0; i < columns.Count; i++)
                                        {
                                            bw.Write(columns[i].ColumnName);
                                            bw.Write(currentRowValues[i]);
                                        }

                                        bw.Write((byte)'\n');  // Agregar salto de línea
                                    }
                                }
                                else
                                {
                                    // Copiar las otras tablas sin cambios
                                    bw.Write(tblName);

                                    for (int i = 0; i < columns.Count; i++)
                                    {
                                        bw.Write(br.ReadString());  // Copiar el nombre de la columna
                                        bw.Write(br.ReadString());  // Copiar el valor de la columna
                                    }

                                    bw.Write(br.ReadByte());  // Copiar el salto de línea
                                }
                            }
                        }
                    }
                }
            }

            // Reemplazar el archivo original con el archivo temporal
            File.Delete(SystemColumnsFile);
            File.Move(tempFile, SystemColumnsFile);

            if (foundMatchingRows)
            {
                consoleHelper.PrintSuccess($"Filas actualizadas correctamente en la tabla '{tableName}' donde '{whereColumn}' = '{whereValue}'.");
                return OperationStatus.Success;
            }
            else
            {
                consoleHelper.PrintError($"No se encontraron filas que coincidan con la condición WHERE '{whereColumn}' = '{whereValue}' en la tabla '{tableName}'.");
                return OperationStatus.Error;
            }
        }

        public OperationStatus Delete(string sentence)
        {
            // Iniciar el temporizador
            var stopwatch = Stopwatch.StartNew();
            consoleHelper.PrintStartTimer("Iniciando eliminación...");

            // Expresión regular para analizar la sentencia DELETE, con y sin WHERE
            string patternWithWhere = @"^DELETE\s+FROM\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+WHERE\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*\""?([a-zA-Z0-9_.:\-\s]+)\""?\s*;?$";
            string patternWithoutWhere = @"^DELETE\s+FROM\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*;?$";

            Match matchWithWhere = Regex.Match(sentence, patternWithWhere);
            Match matchWithoutWhere = Regex.Match(sentence, patternWithoutWhere);

            string tableName, variable = null, value = null;

            if (matchWithWhere.Success)
            {
                // DELETE con WHERE
                tableName = matchWithWhere.Groups[1].Value;
                variable = matchWithWhere.Groups[2].Value;
                value = matchWithWhere.Groups[3].Value;

                consoleHelper.PrintInfo($"Tabla: {tableName}");
                consoleHelper.PrintInfo($"Variable: {variable}");
                consoleHelper.PrintInfo($"Valor: {value}");
            }
            else if (matchWithoutWhere.Success)
            {
                // DELETE sin WHERE
                tableName = matchWithoutWhere.Groups[1].Value;
                consoleHelper.PrintInfo($"Tabla: {tableName}");
            }
            else
            {
                consoleHelper.PrintError("Error: Sintaxis incorrecta para el comando DELETE.");
                return OperationStatus.Error;
            }

            // Verificar si la tabla existe en la base de datos actual
            List<ColumnMetadata> columns = ReadTableFromSystemCatalog(tableName);
            if (columns == null)
            {
                consoleHelper.PrintError($"Error: La tabla '{tableName}' no existe en la base de datos {currentDatabase}.");
                return OperationStatus.Error;
            }

            // Eliminar las filas que coincidan con la condición WHERE, o todas si no hay WHERE
            var result = DeleteRowsFromSystemColumns(tableName, variable, value, columns);

            // Detener el temporizador y mostrar el tiempo final
            stopwatch.Stop();
            consoleHelper.PrintStopTimer($"Eliminación completada en {stopwatch.ElapsedMilliseconds} ms.");

            return result;
        }

        private OperationStatus DeleteRowsFromSystemColumns(string tableName, string? variable, string? value, List<ColumnMetadata> columns)
        {
            string tempFile = Path.GetTempFileName(); // Crear un archivo temporal para almacenar los datos restantes
            bool foundMatchingRows = false;

            // Lista para almacenar todas las filas leídas antes de hacer cambios
            List<(string tblName, List<string> rowValues)> allRows = new List<(string, List<string>)>();

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
                            List<string> currentRowValues = new List<string>();

                            for (int i = 0; i < columns.Count; i++)
                            {
                                string columnName = br.ReadString();
                                string columnValue = br.ReadString();
                                currentRowValues.Add(columnValue);
                            }

                            br.ReadByte(); // Leer el salto de línea

                            // Guardar todas las filas para procesar después
                            allRows.Add((tblName, currentRowValues));
                        }
                        catch (EndOfStreamException)
                        {
                            consoleHelper.PrintError("Error: Se intentó leer más allá del final del archivo SystemColumnsFile.");
                            break;
                        }
                    }
                }
            }

            using (var tempFs = new FileStream(tempFile, FileMode.Create))
            {
                using (var bw = new BinaryWriter(tempFs))
                {
                    foreach (var (tblName, currentRowValues) in allRows)
                    {
                        if (tblName != tableName)
                        {
                            // Si no es la tabla que estamos buscando, copiar la fila completa al archivo temporal
                            bw.Write(tblName);
                            for (int i = 0; i < columns.Count; i++)
                            {
                                bw.Write(columns[i].ColumnName);
                                bw.Write(currentRowValues[i]);
                            }
                            bw.Write((byte)'\n'); // Salto de línea
                        }
                        else if (variable == null || currentRowValues[columns.FindIndex(c => c.ColumnName == variable)] == value)
                        {
                            foundMatchingRows = true;
                            consoleHelper.PrintInfo($"Eliminando fila en la tabla '{tableName}'.");

                            // No escribir la fila en el archivo temporal (esto la "elimina")
                        }
                        else
                        {
                            // Si no se encuentra un match, copiar la fila completa al archivo temporal
                            bw.Write(tblName);
                            for (int i = 0; i < columns.Count; i++)
                            {
                                bw.Write(columns[i].ColumnName);
                                bw.Write(currentRowValues[i]);
                            }
                            bw.Write((byte)'\n'); // Salto de línea
                        }
                    }
                }
            }

            // Reemplazar el archivo original con el archivo temporal
            File.Delete(SystemColumnsFile);
            File.Move(tempFile, SystemColumnsFile);

            if (foundMatchingRows || variable == null)
            {
                consoleHelper.PrintSuccess($"Filas eliminadas correctamente de la tabla '{tableName}'.");
                return OperationStatus.Success;
            }
            else
            {
                consoleHelper.PrintError($"No se encontraron filas que coincidan con la condición WHERE {variable} = {value} en la tabla '{tableName}'.");
                return OperationStatus.Error;
            }
        }
    }

    public class ColumnMetadata
    {
        public string ColumnName { get; set; } = string.Empty; // Inicializar con valor no nulo
        public string ColumnType { get; set; } = string.Empty; // Inicializar con valor no nulo
        public int SizeInBytes { get; set; }
    }
}
