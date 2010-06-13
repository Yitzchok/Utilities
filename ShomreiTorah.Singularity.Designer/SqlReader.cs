using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShomreiTorah.Common;

namespace ShomreiTorah.Singularity.Designer {
	static class SqlReader {
		const string TablesSql = @"
SELECT 
	Tables.TABLE_NAME		TableName,
	Tables.TABLE_SCHEMA		SchemaName,
	PrimaryKeys.PrimaryKeyName
FROM	INFORMATION_SCHEMA.Tables Tables
		LEFT JOIN (
			SELECT
				Tables.TABLE_NAME		TableName,
				Tables.TABLE_SCHEMA		SchemaName,
				KeyColumns.COLUMN_NAME	PrimaryKeyName
			FROM	INFORMATION_SCHEMA.Tables Tables
					JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS Constraints	ON Tables.TABLE_NAME = Constraints.TABLE_NAME
					JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KeyColumns		ON Constraints.CONSTRAINT_NAME = KeyColumns.CONSTRAINT_NAME
			WHERE	CONSTRAINT_TYPE = 'PRIMARY KEY'
		) PrimaryKeys ON (Tables.TABLE_NAME = PrimaryKeys.TableName AND Tables.TABLE_SCHEMA = PrimaryKeys.SchemaName)";

		const string ColumnsSql = @"
SELECT 
	Columns.TABLE_NAME	TableName,
	Columns.COLUMN_NAME	ColumnName,
	Columns.DATA_TYPE	DataType,
	CASE Columns.IS_NULLABLE WHEN 'YES' THEN 1 ELSE 0 END	AllowNulls,

	ISNULL(UniqueColumns.Included, 0)	IsUnique,
	ForeignKeyColumns.ParentTable		ForeignTable
FROM	INFORMATION_SCHEMA.COLUMNS Columns
		LEFT OUTER JOIN (
			SELECT
 				iColumns.TABLE_NAME	TableName,
				iColumns.COLUMN_NAME	ColumnName,
				1 AS Included
			FROM	INFORMATION_SCHEMA.COLUMNS iColumns
					JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KeyColumns		ON iColumns.COLUMN_NAME = KeyColumns.COLUMN_NAME
					JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS Constraints	ON KeyColumns.CONSTRAINT_NAME = Constraints.CONSTRAINT_NAME
			WHERE Constraints.CONSTRAINT_TYPE = 'UNIQUE'
		) UniqueColumns ON (UniqueColumns.TableName = Columns.TABLE_NAME AND UniqueColumns.ColumnName = Columns.COLUMN_NAME)

		LEFT OUTER JOIN (
			SELECT
				ChildColumns.TABLE_NAME 		ChildTable,
				ChildColumns.COLUMN_NAME		ChildColumn,
				ParentConstraints.TABLE_NAME 	ParentTable
			FROM	INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS Refs
					JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ChildColumns		ON Refs.CONSTRAINT_NAME = ChildColumns.CONSTRAINT_NAME
					JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS ParentConstraints	ON Refs.UNIQUE_CONSTRAINT_NAME = ParentConstraints.CONSTRAINT_NAME
		) ForeignKeyColumns ON (ForeignKeyColumns.ChildTable = Columns.TABLE_NAME AND ForeignKeyColumns.ChildColumn = Columns.COLUMN_NAME);";

		public static IEnumerable<SchemaModel> ReadSchemas(DBConnector database) {
			if (database == null) throw new ArgumentNullException("database");

			List<SchemaModel> tables = new List<SchemaModel>();
			List<Action> postColumnActions = new List<Action>();

			Func<string, SchemaModel> Table = name => String.IsNullOrEmpty(name) ? null : tables.SingleOrDefault(t => t.SqlName == name);

			using (var connection = database.OpenConnection()) {
				#region Read Tables
				using (var reader = database.ExecuteReader(TablesSql)) {
					while (reader.Read()) {
						var table = new SchemaModel {
							Name = (string)reader["TableName"],
							SqlName = (string)reader["TableName"],
							SqlSchemaName = reader["SchemaName"] as string
						};

						if (Table(table.SqlName) != null) continue;
						string keyName = reader["PrimaryKeyName"] as string;
						if (!String.IsNullOrEmpty(keyName)) {
							postColumnActions.Add(
								() => table.PrimaryKey = table.Columns.Single(c => c.SqlName == keyName)
							);
						}

						tables.Add(table);
					}
				}
				#endregion

				using (var reader = database.ExecuteReader(ColumnsSql)) {
					while (reader.Read()) {
						var table = Table((string)reader["TableName"]);

						table.Columns.Add(new ColumnModel(table) {
							Name = (string)reader["ColumnName"],
							SqlName = (string)reader["ColumnName"],
							DataType = SqlTypes[(string)reader["DataType"]],

							AllowNulls = 1 == (int)reader["AllowNulls"],
							IsUnique = 1 == (int)reader["IsUnique"],

							ForeignSchema = Table(reader["ForeignTable"] as string)
						});
					}
				}
			}

			postColumnActions.ForEach(a => a());

			return tables;
		}

		static readonly Dictionary<string, Type> SqlTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase) {
		    { "UNIQUEIDENTIFIER", typeof(Guid)		},

		    { "VARBINARY",		typeof(byte[])		},
		    { "BINARY",			typeof(byte[])		},
		    { "IMAGE",			typeof(byte[])		},

		    { "NVARCHAR",		typeof(String)		},
		    { "VARCHAR",		typeof(String)		},
		    { "NTEXT",			typeof(String)		},
		    { "NCHAR",			typeof(String)		},
		    { "CHAR",			typeof(String)		},
		    { "TEXT",			typeof(String)		},
			  
		    { "DATETIME",		typeof(DateTime)	},
			  
		    { "TINYINT",		typeof(Byte)		},
		    { "SMALLINT",		typeof(Int16)		},
		    { "INTEGER",		typeof(Int32)		},
		    { "BIGINT",			typeof(Int64)		},
		    { "INT",			typeof(Int32)		},
			  
		    { "DECIMAL",		typeof(Decimal)		},
		    { "MONEY",			typeof(Decimal)		},
		    { "FLOAT",			typeof(Double)		},
		    { "REAL",			typeof(Single)		},
		    { "BIT",			typeof(Boolean)		},
		};
	}
}