using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace SomeDB.Storage
{
    public class SqlSeverStorage : IStorage
    {
        private readonly ISerializer _serializer;
        private readonly int _bufferSize;
        private readonly SqlConnectionStringBuilder _connInfo;

        public const string DataTableCreationScript = @"CREATE TABLE [dbo].[Data] (
[Type] [nvarchar](200) NOT NULL
,[DocId] [nvarchar](200) NOT NULL
,[Value] [nvarchar](max) NOT NULL
,CONSTRAINT [PK_Data] PRIMARY KEY CLUSTERED (
	[Type] ASC
	,[DocId] ASC
	)
)";

        public SqlSeverStorage(string connString, ISerializer serializer = null, int bufferSize = 2000)
        {
            if (connString == null) throw new ArgumentNullException("connString");
            _connInfo = new SqlConnectionStringBuilder(connString);
            _serializer = serializer?? new JsonSerializer();
            _bufferSize = bufferSize > 0 ? bufferSize : 2000;
        }

        public void CreateDataTable()
        {
            ExecuteSql(DataTableCreationScript);
        }

        private void ExecuteSql(SqlConnection conn,string sql, params SqlParameter[] parameters)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
            }
        }  

        private void ExecuteSql(string sql, params SqlParameter[] parameters)
        {
            using (var conn = OpenConnection())
                ExecuteSql(conn, sql, parameters);
        }

        private SqlConnection OpenConnection()
        {
            var conn = new SqlConnection(_connInfo.ConnectionString);
            conn.Open();
            return conn;
        }

        public IEnumerable<IDocument> Store(IEnumerable<IDocument> documents)
        {
            const string typeCol = "Type";
            const string docIdCol = "DocId";
            const string valueCol = "Value";

            var table = new DataTable();
            table.Columns.Add(typeCol, typeof(string));
            table.Columns.Add(docIdCol, typeof(string));
            table.Columns.Add(valueCol, typeof(string));

            using (var conn = OpenConnection())
            {
                // create staging table
                ExecuteSql(conn, "create table #data ([Type] nvarchar(200) NOT NULL, " +
                                 "[DocId] nvarchar(200) NOT NULL, " +
                                 "[Value] [nvarchar](max) NOT NULL)");
                
                

                var bcp = new SqlBulkCopy(conn)
                {
                    DestinationTableName = "#data",
                };

                foreach (var buffer in documents.Buffer(_bufferSize).Select(x => x.ToArray()))
                {
                    // insert into staging table
                    table.Clear();
                    foreach (var doc in buffer)
                    {
                        var row = table.NewRow();
                        row[typeCol] = doc.GetType().FullName;
                        row[docIdCol] = doc.Id;
                        row[valueCol] = _serializer.Serialize(doc);
                        table.Rows.Add(row);
                    }
                    bcp.WriteToServer(table);

                    // merge into data table
                    ExecuteSql(conn, "MERGE INTO data as d " +
                                     "USING #data as s " +
                                     "ON d.[Type] = s.[Type] AND d.DocId = s.DocId " +
                                     "WHEN MATCHED THEN UPDATE SET d.Value = s.Value " +
                                     "WHEN NOT MATCHED THEN INSERT ([Type], DocId, Value) " +
                                     "      VALUES (s.Type, s.DocId, s.Value);");

                    foreach (var doc in buffer)
                        yield return doc;
                }

                // drop staging table
                ExecuteSql(conn, "drop table #data");
            }
        }

        public IDocument Retrieve(Type type, string id)
        {
            var results = QuerySql("SELECT [Value] FROM Data " +
                                  "WHERE [Type] = @type AND [DocId] = @id",
                new SqlParameter("@type", type.FullName),
                new SqlParameter("@id", id)).SingleOrDefault();

            if (results == null)
                return null;

            var doc = (IDocument) _serializer.Deserialize((string) results[0], type);
            return doc;
        }

        private IEnumerable<object[]> QuerySql(string sql, params SqlParameter[] parameters)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters);

                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        var values = new object[reader.FieldCount];
                        reader.GetValues(values);
                        yield return values;
                    }
            }
        }

        public IEnumerable<IDocument> RetrieveAll(Type type)
        {
            var values = QuerySql("SELECT [Value] FROM Data WHERE [Type] = @type",
                new SqlParameter("@type", type.FullName));

            var docs = values
                .Select(arr => (string)arr[0])
                .Select(value => _serializer.Deserialize(value, type))
                .Cast<IDocument>();

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var doc in docs)
                yield return doc;
        }

        public IEnumerable<IDocument> RetrieveAll()
        {
            var docTypes = AppDomain.CurrentDomain.GetTypes()
                .Where(x => typeof(IDocument).IsAssignableFrom(x))
                .ToLookup(x => x.FullName, StringComparer.OrdinalIgnoreCase); 
            
            var results = QuerySql("SELECT [Type], [Value] FROM Data");

            return from result in results
                let typeName = (string) result[0]
                let value = (string) result[1]
                let type = docTypes[typeName].Single()
                select (IDocument) _serializer.Deserialize(value, type);
        }

        public void Remove(Type type, string id)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (id == null) throw new ArgumentNullException("id");

            ExecuteSql("DELETE FROM Data WHERE Type = @type AND DocId = @id",
                new SqlParameter("@type", type.FullName),
                new SqlParameter("@id", id));
        }
    }
}

