using System;
using System.IO;
using StoreDataManager;
using Entities;

namespace QueryProcessor.Operations
{
    public class Update
    {
        public OperationStatus Execute(string sentence)
        {
            //	try
            //	{
            //		// Parsear la sentencia SQL usando expresiones regulares
            //		string pattern = @"^\s*UPDATE\s+(\w+)\s+SET\s+(.+)\s+WHERE\s+(.+)\s*;?$";
            //		var match = Regex.Match(sentence, pattern, RegexOptions.IgnoreCase);

            //		if (!match.Success)
            //		{
            //			return OperationStatus.Failure("Sintaxis inválida para UPDATE.");
            //		}

            //		// Extraer la tabla, pares columna-valor, y condición
            //		string tableName = match.Groups[1].Value;
            //		string setPart = match.Groups[2].Value;
            //		string whereCondition = match.Groups[3].Value;

            //		// Validar que la tabla exista
            //		if (!Store.Instance.TableExists("TESTDB", tableName))
            //		{
            //			return OperationStatus.Failure($"La tabla {tableName} no existe.");
            //		}

            //		// Parsear los pares columna-valor
            //		var columnValuePairs = setPart.Split(',');
            //		Dictionary<string, string> updates = new Dictionary<string, string>();
            //		foreach (var pair in columnValuePairs)
            //		{
            //			var columnValue = pair.Trim().Split('=');
            //			string column = columnValue[0].Trim();
            //			string value = columnValue[1].Trim();

            //			updates[column] = value;
            //		}

            //		// Leer y actualizar los registros que coincidan con la condición
            //		bool result = Store.Instance.UpdateRecords("TESTDB", tableName, updates, whereCondition);

            //		if (result)
            //		{
            //			return OperationStatus.Success($"Registros actualizados en la tabla {tableName}.");
            //		}
            //		else
            //		{
            //			return OperationStatus.Failure("No se encontraron registros para actualizar.");
            //		}
            //	}
            //	catch (Exception ex)
            //	{
            //		return OperationStatus.Failure($"Error al actualizar registros: {ex.Message}");
            //	}
            //}

            return Store.GetInstance().Update(sentence);
        }
    }
}


