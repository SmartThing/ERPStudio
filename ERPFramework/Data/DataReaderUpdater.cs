﻿using ERPFramework.Forms;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace ERPFramework.Data
{
    public interface IDataReaderUpdater
    {
        bool Find();
        bool Update();
        int Count { get; }

        T GetValue<T>(IColumn columnname);
        T GetValue<T>(IColumn columnname, int i);
        T GetValue<T>(string columnname, int i);
        bool Delete(int t = 0, bool forceUpdate = false);

        DataRow GetRecord();
        DataRow GetRecord(int i);

        void SetValue<T>(IColumn columnname, int row, T value);
        void SetValue<T>(IColumn columnname, T value);
        void SetValue<T>(string columnname, int row, T value);
    }

    #region DataReaderUpdater

    public abstract class DataReaderUpdater<TTable> : IDataReaderUpdater
    {
        protected SqlABConnection sqlCN;
        protected SqlABDataAdapter sqlDA;
        protected SqlABCommand sqlCM;
        protected DataSet dataSet;
        protected string myTable = string.Empty;
        private string myCode = string.Empty;
        private bool firstUpdate = true;

        #region Public Variables

        public SqlABConnection Connection
        {
            get { return sqlCN; }
            set { sqlCN = value; }
        }


        public DataSet DataSet { get { return dataSet; } }
        public string Table
        {
            get { return myTable; }
            set { myTable = value; }
        }

        public string Code
        {
            get { return myCode; }
            set { myCode = value; }
        }

        public int Count
        {
            get
            {
                if (dataSet == null) return -1;

                return dataSet.Tables[myTable].Rows.Count;
            }
        }

        private IDocumentBase documentBase;

        public DataTable DataTable { get { return dataSet.Tables[myTable]; } }

        #endregion

        protected DataReaderUpdater()
            : this(GlobalInfo.DBaseInfo.dbManager.DB_Connection, null)
        {
        }

        [Obsolete("Use version with transaction")]
        protected DataReaderUpdater(bool updater)
            : this(GlobalInfo.DBaseInfo.dbManager.DB_Connection, null)
        {
        }

        [Obsolete("Use version with transaction")]
        protected DataReaderUpdater(SqlABConnection conn, bool updater)
            : this(conn, null)
        {
        }

        protected DataReaderUpdater(IDocumentBase documentBase)
            : this(GlobalInfo.DBaseInfo.dbManager.DB_Connection, documentBase)
        {
        }

        protected DataReaderUpdater(SqlABConnection conn, IDocumentBase documentBase)
        {
            System.Diagnostics.Debug.Assert(conn != null, "Connection is null");
            myTable = typeof(TTable).GetField("Name").GetValue(null).ToString();
            sqlCN = documentBase != null && documentBase.Connection != null
                ? documentBase.Connection
                : conn;
            this.documentBase = documentBase;
        }

        public virtual bool Find()
        {
            if (dataSet == null)
            {
                dataSet = new DataSet();
                CreateConnection();
            }

            dataSet.Clear();
            SetParameters();
            AddTransaction();
            try
            {
                sqlDA.Fill(dataSet, myTable);
            }
            catch (SqlException e)
            {
                Trace.Write(e.Message);
                Debug.Assert(false);
            }

            return dataSet.Tables[0].Rows.Count > 0;
        }

        private void CreateConnection()
        {
            sqlCM = new SqlABCommand(sqlCN);
            AddParameters();
            sqlCM.CommandText = CreateQuery();
            sqlDA = new SqlABDataAdapter(sqlCM);
        }

        public SqlABParameter AddParameters(string parameterName, IColumn column)
        {
            return sqlCM.Parameters.Add(new SqlABParameter(parameterName, column));
        }

        public SqlABParameter AddParameters(string parameterName, Type colType, int colLen)
        {
            return sqlCM.Parameters.Add(new SqlABParameter(parameterName, colType, colLen));
        }

        protected virtual void AddParameters() { }

        protected virtual void SetParameters() { }

        protected abstract string CreateQuery();

        public virtual DataRow AddRecord()
        {
            var dr = dataSet.Tables[myTable].NewRow();
            foreach (MemberInfo mi in typeof(TTable).GetMembers())
                if (mi.MemberType == MemberTypes.Field)
                {
                    if (((FieldInfo)mi).FieldType.GetInterface(nameof(IColumn)) == typeof(IColumn))
                    {
                        var ob = ((FieldInfo)mi).GetValue((mi));
                        System.Diagnostics.Debug.WriteLine(ob is IColumn);
                        var col = (IColumn)((FieldInfo)mi).GetValue((mi));
                        if (!col.IsVirtual)
                        {
                            dr[col.Name] = col.DefaultValue;
                        }
                    }
                }

            dataSet.Tables[myTable].Rows.Add(dr);
            return dr;
        }

        public bool Update()
        {
            try
            {
                var ds2 = dataSet.GetChanges();
                if (ds2 != null)
                {
                    if (ds2.HasErrors) MessageBox.Show("Errori");
                    if (ds2.HasChanges())
                    {
                        AddTransaction();
                        CreateUpdateCommand();

                        sqlDA.Update(dataSet, myTable);
                    }
                }
            }
            catch (SqlException e)
            {
                Trace.Write(e.Message);
                Debug.Assert(false);
                return false;
            }

            return true;
        }

        public bool Delete(int t = 0, bool forceUpdate = false)
        {
            try
            {
                dataSet.Tables[myTable].Rows[t].Delete();
                if (forceUpdate)
                {
                    AddTransaction();
                    CreateUpdateCommand();

                    sqlDA.Update(dataSet, myTable);
                }
            }
            catch (Exception e)
            {
                Trace.Write(e.Message);
                Debug.Assert(false);
                return false;
            }

            return true;
        }

        public DataRow GetRecord()
        {
            return dataSet.Tables[myTable].Rows.Count > 0
                ? dataSet.Tables[myTable].Rows[0]
                : null;
        }

        public DataRow GetRecord(int i)
        {
            return i < dataSet.Tables[myTable].Rows.Count
                ? dataSet.Tables[myTable].Rows[i]
                : null;
        }

        public T GetValue<T>(IColumn columnname)
        {
            return GetValue<T>(columnname.Name, 0);
        }

        public T GetValue<T>(IColumn columnname, int i)
        {
            return GetValue<T>(columnname.Name, i);
        }

        public T GetValue<T>(string columnname, int i)
        {
            return i < dataSet.Tables[myTable].Rows.Count ? typeof(T).BaseType == typeof(Enum)
                    ? dataSet.Tables[myTable].Rows[i][columnname] == System.DBNull.Value
                        ? (T)Enum.ToObject(typeof(T), default(T))
                        : (T)Enum.ToObject(typeof(T), dataSet.Tables[myTable].Rows[i][columnname])
                    : dataSet.Tables[myTable].Rows[i][columnname] == System.DBNull.Value
                        ? (
                            typeof(T) == typeof(string)
                                ? (T)Convert.ChangeType("", typeof(T))
                                : (T)Convert.ChangeType(default(T), typeof(T))
                           )
                        : (T)Convert.ChangeType(dataSet.Tables[myTable].Rows[i][columnname], typeof(T)) : default(T);
        }

        public void SetValue<T>(IColumn columnname, int row, T value)
        {
            SetValue<T>(columnname.Name, row, value);
        }

        public void SetValue<T>(IColumn columnname, T value)
        {
            SetValue<T>(columnname.Name, 0, value);
        }

        public void SetValue<T>(string columnname, int row, T value)
        {
            Debug.Assert(dataSet.Tables[myTable].Rows.Count > row, "Row number exceeed number of rows");
            if (dataSet.Tables[myTable].Rows.Count > 0)
            {
                if (typeof(T).BaseType == typeof(Enum))
                    dataSet.Tables[myTable].Rows[row][columnname] = (int)Convert.ChangeType(value, typeof(int));
                else
                    dataSet.Tables[myTable].Rows[row][columnname] = value;
            }
        }

        private void AddTransaction()
        {
            if (documentBase != null && documentBase.Transaction != null)
            {
                sqlDA.UpdateCommand.Transaction = documentBase.Transaction;
                sqlDA.InsertCommand.Transaction = documentBase.Transaction;
                sqlDA.DeleteCommand.Transaction = documentBase.Transaction;
            }
        }

        protected virtual void CreateUpdateCommand()
        {
            firstUpdate = false;

            var cBuilder = new SqlABCommandBuilder(sqlDA)
            {
                QuotePrefix = "[",
                QuoteSuffix = "]",
                ConflictOption = ConflictOption.CompareRowVersion
            };
            sqlDA.UpdateCommand = cBuilder.GetUpdateCommand();
            sqlDA.InsertCommand = cBuilder.GetInsertCommand();
            sqlDA.DeleteCommand = cBuilder.GetDeleteCommand();


        }
    }

    #endregion
}