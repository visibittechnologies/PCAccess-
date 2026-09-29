namespace PCAccess.DAL
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Text;
    using System.Xml;

    public class MySqlHelper
    {
        #region Declaration
        private static string _conString = System.Configuration.ConfigurationManager.ConnectionStrings["constr"] != null
            ? System.Configuration.ConfigurationManager.ConnectionStrings["constr"].ToString()
            : "Data Source=38.247.144.251,55798;Initial Catalog=PCAccess_DB;User ID=PCAccess_DB;Password=Amitendra@123#;MultipleActiveResultSets=True";

        public static string ConnectionString
        {
            get { return _conString; }
            set { _conString = value; }
        }
        #endregion
        #region ExecuteNonQuery
        public static int ExecuteNonQuery(string query)
        {
            int retval;

            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);

            try
            {
                if (query.ToUpper().StartsWith("INSERT") | query.ToUpper().StartsWith("UPDATE") | query.ToUpper().StartsWith("DELETE"))
                //if (query.StartsWith("INSERT") | query.StartsWith("insert") | query.StartsWith("UPDATE") | query.StartsWith("update") | query.StartsWith("DELETE") | query.StartsWith("delete"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }

                cnn.Open();
                retval = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cnn.State == ConnectionState.Open)
                {
                    cnn.Dispose();
                }
            }
            return retval;
        }

        public static int ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            int retval;

            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);
            try
            {
                if (query.ToUpper().StartsWith("INSERT") | query.ToUpper().StartsWith("UPDATE") | query.ToUpper().StartsWith("DELETE"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                for (int i = 0; i <= parameters.Length - 1; i++)
                {
                    cmd.Parameters.Add(parameters[i]);
                }

                cnn.Open();
                retval = cmd.ExecuteNonQuery();

                cnn.Close();

                if (cmd.Parameters.Count > 0)
                {
                    cmd.Parameters.Clear();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cnn.State == ConnectionState.Open)
                {
                    cnn.Dispose();
                }
            }

            return retval;
        }
        public static string ExecuteNonQueryWithOutParameter(string query, string outParaName, params SqlParameter[] parameters)
        {
            string retval;

            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);
            try
            {
                if (query.ToUpper().StartsWith("INSERT") | query.ToUpper().StartsWith("UPDATE") | query.ToUpper().StartsWith("DELETE"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                for (int i = 0; i <= parameters.Length - 1; i++)
                {
                    cmd.Parameters.Add(parameters[i]);
                }
                cmd.Parameters["@" + outParaName].Direction = ParameterDirection.Output;
                cmd.Parameters["@" + outParaName].Size = 100;
                cnn.Open();
                cmd.ExecuteNonQuery();
                cnn.Close();
                retval = cmd.Parameters["@" + outParaName].Value.ToString();

                if (cmd.Parameters.Count > 0)
                {
                    cmd.Parameters.Clear();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cnn.State == ConnectionState.Open)
                {
                    cnn.Dispose();
                }
            }

            return retval;
        }
        #endregion
        #region ExecuteScalers
        public static object ExecuteScalar(string query)
        {
            object retval;

            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);

            try
            {
                if (query.ToUpper().StartsWith("SELECT"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                cnn.Open();
                retval = cmd.ExecuteScalar();
                cnn.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cnn.State == ConnectionState.Open)
                {
                    cnn.Dispose();
                }
            }
            return retval;
        }

        public static object ExecuteScalar(string query, params SqlParameter[] parameters)
        {
            object retval;

            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);
            try
            {
                if (query.ToUpper().StartsWith("SELECT"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                for (int i = 0; i <= parameters.Length - 1; i++)
                {
                    cmd.Parameters.Add(parameters[i]);
                }
                cnn.Open();
                retval = cmd.ExecuteScalar();
                cnn.Close();

                if (cmd.Parameters.Count > 0)
                {
                    cmd.Parameters.Clear();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cnn.State == ConnectionState.Open)
                {
                    cnn.Dispose();
                }
            }
            return retval;
        }
        public static DataTable ExecuteScalarReturnDatatable(string query, params SqlParameter[] parameters)
        {
            var dt = new DataTable();
            dt.Columns.Add(new DataColumn("Response", typeof(string)));
            string retval = string.Empty;

            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);
            try
            {
                if (query.ToUpper().StartsWith("SELECT"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                for (int i = 0; i <= parameters.Length - 1; i++)
                {
                    cmd.Parameters.Add(parameters[i]);
                }
                cnn.Open();
                retval = cmd.ExecuteScalar().ToString();
                cnn.Close();

                if (cmd.Parameters.Count > 0)
                {
                    cmd.Parameters.Clear();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cnn.State == ConnectionState.Open)
                {
                    cnn.Dispose();
                }
                var dr = dt.NewRow();
                dr["Response"] = retval;
                dt.Rows.Add(dr);
                dt.AcceptChanges();
            }
            return dt;
        }

        #endregion
        #region ExecuteReaders
        public static SqlDataReader ExecuteReader(string query)
        {
            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);
            try
            {
                if (query.ToUpper().StartsWith("SELECT"))
                {
                    cmd.CommandType = CommandType.Text;
                    cnn.Open();
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cnn.Open();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public static SqlDataReader ExecuteReader(string query, params SqlParameter[] parameters)
        {
            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);

            try
            {
                if (query.ToUpper().StartsWith("SELECT"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                for (int i = 0; i <= parameters.Length - 1; i++)
                {
                    cmd.Parameters.Add(parameters[i]);
                }
                cnn.Open();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }
        public static StringBuilder ExecuteReaderReturnJSON(string query, params SqlParameter[] parameters)
        {
            var jsonResult = new StringBuilder();
            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);
            try
            {
                if (!query.ToUpper().Contains("DELETE") && !query.ToUpper().Contains("UPDATE"))
                {
                    if (query.ToUpper().StartsWith("SELECT"))
                    {
                        cmd.CommandType = CommandType.Text;
                    }
                    else
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                    }
                    for (int i = 0; i <= parameters.Length - 1; i++)
                    {
                        cmd.Parameters.Add(parameters[i]);
                    }
                    cnn.Open();
                    var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    if (!reader.HasRows)
                    {
                        jsonResult.Append("[]");
                    }
                    else
                    {
                        while (reader.Read())
                        {
                            jsonResult.Append(reader.GetValue(0).ToString());
                        }
                    }
                }
            }
            catch
            {
                throw;
            }

            return jsonResult;
        }
        #endregion
        #region Dataset
        public static DataSet ExecuteDataSet(string query)
        {
            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);
            DataSet ds;
            SqlDataAdapter da;
            try
            {
                if (query.ToUpper().StartsWith("SELECT"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                da = new SqlDataAdapter();
                da.SelectCommand = cmd;
                ds = new DataSet();
                da.Fill(ds);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            { cnn.Dispose(); }
            return ds;
        }

        public static DataSet ExecuteDataSet(string query, params SqlParameter[] parameters)
        {
            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);
            SqlDataAdapter da;
            DataSet ds;

            try
            {
                if (query.ToUpper().StartsWith("SELECT"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                for (int i = 0; i <= parameters.Length - 1; i++)
                {
                    cmd.Parameters.Add(parameters[i]);
                }
                da = new SqlDataAdapter();
                da.SelectCommand = cmd;
                ds = new DataSet();
                da.Fill(ds);

                if (cmd.Parameters.Count > 0)
                {
                    cmd.Parameters.Clear();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            { cnn.Dispose(); }
            return ds;
        }

        #endregion
        #region DataTable
        public static DataTable ExecuteDataTable(string query)
        {
            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);
            SqlDataReader dr;
            DataTable dt;

            try
            {
                if (query != null && query.Trim().ToUpper().StartsWith("SELECT"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                cnn.Open();
                dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                dt = new DataTable();
                dt.Load(dr);

                if (cmd.Parameters.Count > 0)
                {
                    cmd.Parameters.Clear();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cnn.State == ConnectionState.Open)
                {
                    cnn.Dispose();
                }
            }
            return dt;
        }

        public static DataTable ExecuteDataTable(string query, params SqlParameter[] parameters)
        {
            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);
            SqlDataReader dr;
            DataTable dt;

            try
            {
                if (query != null && query.Trim().ToUpper().StartsWith("SELECT"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                for (int i = 0; i <= parameters.Length - 1; i++)
                {
                    cmd.Parameters.Add(parameters[i]);
                }
                cnn.Open();
                dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                dt = new DataTable();
                dt.Load(dr);

                if (cmd.Parameters.Count > 0)
                {
                    cmd.Parameters.Clear();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cnn.State == ConnectionState.Open)
                {
                    cnn.Dispose();
                }
            }
            return dt;
        }

        #endregion
        #region XmlDocument
        /// <summary>
        /// fetch all data in xmldocument formate
        /// </summary>
        /// <param name="query">sql query</param>
        /// <param name="rootNodeName">root node name</param>
        /// <returns>XmlDocument</returns>
        public static XmlDocument ExecuteXmlDoc(string query, string rootNodeName)
        {
            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);
            XmlReader xmlReader = null;
            XmlDocument xmldoc = new XmlDocument();
            try
            {
                if (query.ToUpper().StartsWith("SELECT"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                cnn.Open();
                xmlReader = cmd.ExecuteXmlReader();


                System.Xml.XPath.XPathDocument xpathDoc = new System.Xml.XPath.XPathDocument(xmlReader, XmlSpace.Preserve);
                System.Xml.XPath.XPathNavigator xpathDocNav = xpathDoc.CreateNavigator();

                XmlNode root = xmldoc.CreateElement(rootNodeName);
                root.InnerText = rootNodeName;
                root.InnerXml = xpathDocNav.OuterXml;
                xmldoc.AppendChild(root);
            }
            catch (Exception ex)
            {
                throw new ApplicationException
                   ("Cannot Execute SQL Command: " + ex.Message, ex);
            }
            finally
            {
                // dispose of open objects
                if (xmlReader != null) xmlReader.Close();
                if (cnn != null) cnn.Dispose();
            }
            return xmldoc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TableName">TableName</param>
        /// <param name="FieldsNames">FieldsNames</param>
        /// <param name="OrderByFieldsName">OrderBy FieldsName</param>
        /// <param name="WhereCondition">WhereCondition </param>
        /// <returns>XmlDocument</returns>
        public static XmlDocument ExecuteXmlDoc(String TableName, String FieldsNames, String OrderByFieldsName, String WhereCondition)
        {
            SqlConnection cnn = new SqlConnection(_conString);
            XmlReader xmlReader = null;
            XmlDocument xmldoc = new XmlDocument();
            if (!string.IsNullOrEmpty(WhereCondition))
                WhereCondition = " WHERE " + WhereCondition;

            if (!string.IsNullOrEmpty(OrderByFieldsName))
                OrderByFieldsName = " ORDER BY " + OrderByFieldsName;

            string strSql = "SELECT DISTINCT " + FieldsNames + " FROM " + TableName + WhereCondition + OrderByFieldsName + " FOR XML AUTO";

            SqlCommand cmd = new SqlCommand(strSql, cnn);
            try
            {
                cmd.CommandType = CommandType.Text;

                cnn.Open();
                xmlReader = cmd.ExecuteXmlReader();


                System.Xml.XPath.XPathDocument xpathDoc = new System.Xml.XPath.XPathDocument(xmlReader, XmlSpace.Preserve);
                System.Xml.XPath.XPathNavigator xpathDocNav = xpathDoc.CreateNavigator();

                XmlNode root = xmldoc.CreateElement("TB");
                root.InnerText = "TB";
                root.InnerXml = xpathDocNav.OuterXml;
                xmldoc.AppendChild(root);
            }
            catch (Exception ex)
            {
                throw new ApplicationException
                   ("Cannot Execute SQL Command: " + ex.Message, ex);
            }
            finally
            {
                // dispose of open objects
                if (xmlReader != null) xmlReader.Close();
                if (cnn != null) cnn.Dispose();
            }
            return xmldoc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TableName">TableName</param>
        /// <param name="FieldsNames">FieldsNames</param>
        /// <param name="OrderByFieldsName">OrderBy FieldsName</param>
        /// <param name="WhereCondition">WhereCondition </param>
        /// <returns>XmlDocument</returns>
        public static DataTable ExecuteDataTable(String TableName, String FieldsNames, String OrderByFieldsName, String WhereCondition)
        {
            SqlConnection cnn = new SqlConnection(_conString);
            SqlDataReader dr;
            DataTable DT = new DataTable();
            if (!string.IsNullOrEmpty(WhereCondition))
                WhereCondition = " WHERE " + WhereCondition;

            if (!string.IsNullOrEmpty(OrderByFieldsName))
                OrderByFieldsName = " ORDER BY " + OrderByFieldsName;

            string strSql = "SELECT DISTINCT " + FieldsNames + " FROM " + TableName + WhereCondition + OrderByFieldsName;
            SqlCommand cmd = new SqlCommand(strSql, cnn);
            try
            {
                cmd.CommandType = CommandType.Text;

                cnn.Open();
                dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                DT.Load(dr);
            }
            catch (Exception ex)
            {
                throw new ApplicationException
                   ("Cannot Execute SQL Command: " + ex.Message, ex);
            }
            finally
            {
                // dispose of open objects
                //if (xmlReader != null) xmlReader.Close();
                if (cnn != null) cnn.Dispose();
            }
            return DT;
        }

        /// <summary>
        /// fetch all data in xmldocument formate
        /// </summary>
        /// <param name="query">sql query</param>
        /// <param name="rootNodeName">root node name</param>
        /// <param name="parameters">SqlParameters</param>
        /// <returns>XmlDocument</returns>
        public static XmlDocument ExecuteXmlDoc(string query, string rootNodeName, params SqlParameter[] parameters)
        {
            SqlConnection cnn = new SqlConnection(_conString);
            SqlCommand cmd = new SqlCommand(query, cnn);
            XmlReader xmlReader = null;
            XmlDocument xmldoc = new XmlDocument();
            try
            {
                if (query.ToUpper().StartsWith("SELECT"))
                {
                    cmd.CommandType = CommandType.Text;
                }
                else
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                for (int i = 0; i <= parameters.Length - 1; i++)
                {
                    cmd.Parameters.Add(parameters[i]);
                }
                cnn.Open();
                xmlReader = cmd.ExecuteXmlReader();


                System.Xml.XPath.XPathDocument xpathDoc = new System.Xml.XPath.XPathDocument(xmlReader, XmlSpace.Preserve);
                System.Xml.XPath.XPathNavigator xpathDocNav = xpathDoc.CreateNavigator();

                XmlNode root = xmldoc.CreateElement(rootNodeName);
                root.InnerText = rootNodeName;
                root.InnerXml = xpathDocNav.OuterXml;
                xmldoc.AppendChild(root);
            }
            catch (Exception ex)
            {
                throw new ApplicationException
                   ("Cannot Execute SQL Command: " + ex.Message, ex);
            }
            finally
            {
                // dispose of open objects
                if (xmlReader != null) xmlReader.Close();
                if (cnn != null) cnn.Dispose();
            }
            return xmldoc;

        }
        #endregion
    }
}