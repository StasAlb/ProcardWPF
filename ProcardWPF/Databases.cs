using System;
using System.Data;
using System.Data.Odbc;
using System.Collections;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HugeLib;

namespace ProcardWPF
{
    public class Database
    {
        private DBTypes dbType;

        private OdbcConnection odbc;
        private OleDbConnection oleConn;
        public DBTypes DbType
        {
            get{ return dbType; }
            set{ dbType = value; }
        }
        private string path;
        public string Path
        {
            get { return path; }
            set { path = value; }
        }
        private string table;
        public string Table
        {
            get { return table; }
            set { table = value; }
        }
        private string login;
        public string Login
        {
            get { return login; }
            set { login = value; }
        }
        private string pwd;
        public string Pwd
        {
            get { return pwd; }
            set { pwd = value; }
        }
        private string filter;
        public string Filter
        {
            get { return filter; }
            set { filter = value; }
        }
        public ArrayList Columns;
        public Database()
        {
            dbType = DBTypes.None;
            path = String.Empty;
            table = String.Empty;
            filter = String.Empty;
            Columns = new ArrayList();
        }
        public Database(DBTypes tp)
        {
            dbType = tp;
            path = String.Empty;
            table = String.Empty;
            Columns = new ArrayList();
        }
        public Database(DBTypes tp, string dbPath, string dbLogin, string dbPwd)
        {
            dbType = tp;
            path = dbPath;
            login = dbLogin;
            pwd = dbPwd;
            Columns = new ArrayList();
        }
        public override string ToString()
        {
            return String.Format("{0}: {1}", path, table);
        }
        public bool SetConnection()
        {
            switch (dbType)
            {
                case DBTypes.ODBC:
                    odbc = new OdbcConnection(String.Format(@"DSN={0};UID={1};Pwd={2}", path, login, pwd));
                    // ради поддержки большинства odbc драйверов в свойствах проекта на вкладке Build выставлена галка prefer 32-bit
                    try {
                        odbc.Open();
                    }
                    catch (Exception ex)
                    {
                        Params.WriteLogString("ODBC open error: {0}", ex.Message);
                        break;
                    }
                    Columns.Clear();
                    using (DataTable dt = odbc.GetSchema("Columns"))
                    {
                        foreach (DataRow dr in dt.Rows)
                            if (dr["TABLE_NAME"].ToString() == table)
                                Columns.Add(dr["COLUMN_NAME"].ToString());
                    }
                    break;
                case DBTypes.OleText:
                    oleConn = new OleDbConnection($"Provider = Microsoft.Jet.OLEDB.4.0; Data Source = {path}; Extended Properties = \"text; FMT = Fixed\"");
                    try {
                        oleConn.Open();
                    }
                    catch (Exception ex)
                    {
                        Params.WriteLogString($"OleDbText open error: {ex.Message}");
                        break;
                    }
                    Columns.Clear();
                    using (DataTable dt = oleConn.GetSchema("Columns"))
                    {
                        foreach (DataRow dr in dt.Rows)
                            if (dr["TABLE_NAME"].ToString() == table)
                                Columns.Add(dr["COLUMN_NAME"].ToString());
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }
        public List<string> GetTables()
        {
            List<string> res = new List<string>();
            DataTable dt = null;
            switch (dbType)
            {
                case DBTypes.ODBC:
                    if (odbc == null || odbc.State != System.Data.ConnectionState.Open)
                        break;
                    dt = odbc.GetSchema("TABLES");
                    if (dt != null)
                        foreach (DataRow dr in dt.Rows)
                            res.Add(dr[2].ToString());
                    dt = odbc.GetSchema("VIEWS");
                    if (dt != null)
                        foreach (DataRow dr in dt.Rows)
                            res.Add(dr[2].ToString());
                    break;
                case DBTypes.OleText:
                    if (oleConn?.State != ConnectionState.Open)
                        break;
                    dt = oleConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new Object[] {null, null, null, "TABLE"});
                    if (dt != null)
                        foreach (DataRow dr in dt.Rows)
                            res.Add(dr[2].ToString());
                    break;
            }
            return res;
        }
        public bool IsText()
        {
            return (odbc != null && odbc.DataSource == "TEXT");
        }
        public string Directory()
        {
            if (odbc == null)
                return "";
            return odbc.Database;
        }
        public DataTable GetData()
        {
            DataSet ds = new DataSet();
            switch (dbType)
            {
                case DBTypes.ODBC:
                    using (OdbcCommand comm = odbc.CreateCommand())
                    {
                        comm.CommandText = $"select * from [{table}] {filter}";
                        OdbcDataAdapter ad = new OdbcDataAdapter(comm);
                        try
                        {
                            ad.Fill(ds);
                        }
                        catch (Exception ex)
                        {
                            LogClass.WriteToLog($"Db load error: {ex.Message}");
                        }
                    }
                    break;
                case DBTypes.OleText:
                    using (OleDbCommand comm = oleConn.CreateCommand())
                    {
                        comm.CommandText = $"select * from [{table}] {filter}";
                        OleDbDataAdapter ad = new OleDbDataAdapter(comm);
                        try
                        {
                            ad.Fill(ds);
                        }
                        catch (Exception ex)
                        {
                            LogClass.WriteToLog($"Db load error: {ex.Message}");
                        }

                    }
                    break;
            }
            return (ds.Tables.Count > 0) ? ds.Tables[0] : null;
        }
        public DataTable GetData(int maxCount)
        {
            DataSet ds = new DataSet();
            switch (dbType)
            {
                case DBTypes.ODBC:
                    try
                    {
                        OdbcCommand comm = odbc.CreateCommand();
                        comm.CommandText = String.Format("select top {1} * from [{0}] {2}", table, maxCount, filter);
                        OdbcDataAdapter ad = new OdbcDataAdapter(comm);
                        ad.Fill(ds);
                    }
                    catch
                    {}
                    break;
                case DBTypes.OleText:
                    using (OleDbCommand comm = oleConn.CreateCommand())
                    {
                        comm.CommandText = $"select top {maxCount} * from [{table}] {filter}";
                        OleDbDataAdapter ad = new OleDbDataAdapter(comm);
                        ad.Fill(ds);
                    }
                    break;

            }
            return ds.Tables?[0];
        }
        public ArrayList GetFieldsList()
        {
            DataTable dt = null;
            ArrayList res = new ArrayList();
            switch (dbType)
            {
                case DBTypes.ODBC:
                    dt = odbc.GetSchema("Columns");
                    break;
                case DBTypes.OleText:
                    dt = oleConn?.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, new Object[] {null, null, table, null});
                    break;
            }
            if (dt != null)
                foreach (DataRow dr in dt.Rows)
                    if (dr["TABLE_NAME"].ToString() == table)
                        res.Add(dr["COLUMN_NAME"].ToString());
            res.Sort();
            return res;
        }
    }
}
