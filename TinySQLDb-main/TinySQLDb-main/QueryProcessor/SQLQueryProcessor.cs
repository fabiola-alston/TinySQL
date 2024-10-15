using Entities;
using QueryProcessor.Exceptions;
using QueryProcessor.Operations;
using StoreDataManager;

namespace QueryProcessor
{
    public class SQLQueryProcessor
    {
        public static OperationStatus Execute(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                throw new UnknownSQLSentenceException("La sentencia SQL no puede estar vacia.");
            }

            //// CREATE DATABASE
            if (sentence.StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase))
            {
                return new CreateDatabase().Execute(sentence);
            }

            // SET DATABASE
            if (sentence.StartsWith("SET DATABASE", StringComparison.OrdinalIgnoreCase))
            {
                return new SetDatabase().Execute(sentence);
            }

            // CREATE TABLE
            if (sentence.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            {
                return new CreateTable().Execute(sentence);
            }

            // CREATE INDEX
            if (sentence.StartsWith("CREATE INDEX", StringComparison.OrdinalIgnoreCase))
            {
                return new CreateIndex().Execute(sentence);
            }

            // SELECT
            if (sentence.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                return new Select().Execute(sentence);
            }

            // INSERT INTO
            if (sentence.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
            {
                return new Insert().Execute(sentence);
            }

            // UPDATE
            //if (sentence.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            //{
            //    return new Update().Execute();
            //}

            // DELETE FROM
            if (sentence.StartsWith("DELETE FROM", StringComparison.OrdinalIgnoreCase))
            {
                return new Delete().Execute(sentence);
            }

            // Si la sentencia no es reconocida
            throw new UnknownSQLSentenceException($"La sentencia SQL no es reconocida: {sentence}");
        }
    }
}
