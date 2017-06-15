using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableDoc
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Please pass the connection string as the 1st argument.");
                return 1;
            }

            var documenter = new Documenter(args[0]);
            Console.WriteLine(documenter.Run());
            return 0;
        }
    }

    class Documenter
    {
        private SqlConnection conn;
        private IList<string> tables = new List<string>();
        private IList<TableDef> tableDefs = new List<TableDef>();

        public Documenter(string connString)
        {
            conn = new SqlConnection(connString);
        }

        public string Run()
        {
            GetTables();
            GetTableDefs();
            return ToHtml();
        }

        private void GetTables()
        {
            var cmd = new SqlCommand("sp_tables", conn);
            conn.Open();
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    if (r["TABLE_OWNER"].ToString() == "dbo")
                    {
                        var name = r["TABLE_NAME"].ToString();

                        if (name.StartsWith("_")) continue;
                        if (name.StartsWith("Projects_")) continue;
                        if (name.StartsWith("sysdiagrams")) continue;

                        tables.Add(name);
                    }
                }
                r.Close();
            }
            conn.Close();
        }

        private void GetTableDefs()
        {
            var cmd = new SqlCommand("sp_columns", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@table_name", System.Data.SqlDbType.VarChar));
            conn.Open();
            foreach (var tableName in tables)
            {
                var tableDef = new TableDef { Name = tableName };
                cmd.Parameters["@table_name"].Value = tableName;

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        tableDef.Columns.Add(new ColumnDef
                        (
                            r["COLUMN_NAME"] as string,
                            r["TYPE_NAME"] as string,
                            (int)r["PRECISION"],
                            (r["SCALE"] as int?).GetValueOrDefault(),
                            Convert.ToBoolean(r["NULLABLE"])
                        ));
                    }
                    r.Close();
                }

                tableDefs.Add(tableDef);
            }
            conn.Close();
        }

        private string ToHtml()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<h1>{conn.Database}</h1>");

            foreach (var tableDef in tableDefs.OrderBy(x => x.Name))
            {
                sb.AppendLine("<table border=1>");
                sb.AppendLine($"<tr><th colspan=3 align=center>{tableDef.Name}</th></tr>");
                sb.AppendLine("<tr><th>Column Name</th><th>Type</th><th>Nullable</th></tr>");

                foreach (var col in tableDef.Columns)
                    sb.AppendLine($"<tr><td>{col.Name}</td><td>{col.Type}</td><td>{(col.IsNullable ? "Yes" : "No")}</td></tr>");

                sb.AppendLine("</table><br>");
            }
            return sb.ToString();
        }
    }

    public class TableDef
    {
        public string Name { get; set; }
        public IList<ColumnDef> Columns { get; set; }

        public TableDef()
        {
            Columns = new List<ColumnDef>();
        }
    }

    public class ColumnDef
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsNullable { get; set; }

        public ColumnDef(string name, string type, int precision, int? scale, bool nullable)
        {
            Name = name;
            IsNullable = nullable;

            switch (type)
            {
                case "ntext":
                    Type = "nvarchar(max)";
                    break;

                case "nvarchar":
                    Type = $"nvarchar({precision})";
                    break;

                case "nchar":
                    Type = $"nchar({precision})";
                    break;

                case "varchar":
                    Type = $"varchar({precision})";
                    break;

                case "char":
                    Type = $"char({precision})";
                    break;

                case "decimal":
                    Type = $"decimal({precision}, {scale})";
                    break;

                default:
                    Type = type;
                    break;
            }
        }
    }
}
