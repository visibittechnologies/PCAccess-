namespace PCAccess.DAL
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Xml;

    public class Parameters
    {
        /// <summary>
        /// To get integer type SqlParameter for command object.
        /// </summary>
        /// <param name="propertyName">String property name</param>
        /// <param name="value">Integer value for parameter </param>
        /// <returns>SqlParameter which contans a valid parametername and value.</returns>
        public static SqlParameter GetIntParameter(string propertyName, int value)
        {
            try
            {
                return GetIntParameter(propertyName, value, false);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static SqlParameter GetIntParameter(string propertyName, int? value)
        {
            try
            {
                if (value == null)
                    return new SqlParameter("@" + propertyName, DBNull.Value);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return new SqlParameter("@" + propertyName, value);
        }

        /// <summary>
        /// To get integer type SqlParameter for command object.
        /// </summary>
        /// <param name="propertyName">String parameter name</param>
        /// <param name="value">Integer value for parameter </param>
        /// <param name="allowDbNull">To indicate that value should allow dbnull or not</param>
        /// <remarks>
        ///     if true then it will assign DBNull.value to SqlParameter.
        /// </remarks>
        /// <returns>SqlParameter which contans a valid parametername and value.</returns>
        public static SqlParameter GetIntParameter(string propertyName, int value, bool allowDbNull)
        {
            try
            {
                if (!allowDbNull)
                    return new SqlParameter("@" + propertyName, value);


                if (value <= 0)
                    return new SqlParameter("@" + propertyName, DBNull.Value);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return new SqlParameter("@" + propertyName, value);
        }

        /// <summary>
        /// To get integer type SqlParameter for command object.
        /// </summary>
        /// <param name="propertyName">String property name</param>
        /// <param name="value">Integer value for parameter </param>
        /// <returns>SqlParameter which contans a valid parametername and value.</returns>
        public static SqlParameter GetDecimalParameter(string propertyName, decimal value)
        {
            try
            {
                return GetDecimalParameter(propertyName, value, false);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// To get integer type SqlParameter for command object.
        /// </summary>
        /// <param name="propertyName">String parameter name</param>
        /// <param name="value">Integer value for parameter </param>
        /// <param name="allowDbNull">To indicate that value should allow dbnull or not</param>
        /// <remarks>
        ///     if true then it will assign DBNull.value to SqlParameter.
        /// </remarks>
        /// <returns>SqlParameter which contans a valid parametername and value.</returns>
        public static SqlParameter GetDecimalParameter(string propertyName, decimal value, bool allowDbNull)
        {
            try
            {
                if (!allowDbNull)
                    return new SqlParameter("@" + propertyName, value);


                if (value <= 0)
                    return new SqlParameter("@" + propertyName, DBNull.Value);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return new SqlParameter("@" + propertyName, value);
        }

        /// <summary>
        /// To get SqlParameter with ParameterDirection ReturnValue
        /// </summary>
        /// <param name="name">string name for parameter</param>
        /// <returns>SqlParameter which contans a valid parametername and value with parameter direction ReturnValue.</returns>
        public static SqlParameter GetReurnParameter(string name)
        {
            SqlParameter objParameter = new SqlParameter();

            try
            {
                objParameter.ParameterName = "@" + name;
                objParameter.Direction = ParameterDirection.ReturnValue;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objParameter;
        }

        /// <summary>
        /// To get DateTime type SqlParameter for command object. 
        /// </summary>
        /// <param name="propertyName">String parameter name</param>
        /// <param name="value">Integer value for parameter </param>
        /// <returns>SqlParameter which contans a valid parametername and value.</returns>
        public static SqlParameter GetDateParameter(string propertyName, DateTime value)
        {
            try
            {
                if (value == DateTime.MinValue)
                    return new SqlParameter("@" + propertyName, DBNull.Value);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return new SqlParameter("@" + propertyName, value);
        }
        public static SqlParameter GetDateParameter(string propertyName, DateTime? value)
        {
            try
            {
                if (value == null)
                    return new SqlParameter("@" + propertyName, DBNull.Value);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return new SqlParameter("@" + propertyName, value);
        }
        /// <summary>
        /// To get DateTime with type SqlParameter for command object. 
        /// </summary>
        /// <param name="propertyName">String parameter name</param>
        /// <param name="value">Integer value for parameter </param>
        /// <returns>SqlParameter which contans a valid parametername and value.</returns>
        public static SqlParameter GetDateTimeParameter(string propertyName, DateTime value)
        {
            try
            {
                return GetDateParameter(propertyName, value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// To get double type SqlParameter for command object.
        /// </summary>
        /// <param name="propertyName">String property name</param>
        /// <param name="value">Integer value for parameter </param>
        /// <returns>SqlParameter which contans a valid parametername and value.</returns>
        public static SqlParameter GetDoubleParameter(string propertyName, double value)
        {
            try
            {
                return new SqlParameter("@" + propertyName, value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static SqlParameter GetFloatParameter(string propertyName, decimal value)
        {
            try
            {
                return new SqlParameter("@" + propertyName, value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static SqlParameter GetFloatPara(string propertyName, float value)
        {
            try
            {
                return new SqlParameter("@" + propertyName, value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// To get string type SqlParameter for command object.
        /// </summary>
        /// <param name="propertyName">String property name</param>
        /// <param name="value">Integer value for parameter </param>
        /// <returns>SqlParameter which contans a valid parametername and value.</returns>
        public static SqlParameter GetStringParameter(string propertyName, string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = "";
                    return new SqlParameter("@" + propertyName, value);
                }
                value = value.Replace("'", "");
                value = value.Replace("%", "");
                value = value.Replace("<", "");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return new SqlParameter("@" + propertyName, value.Trim());
        }

        public static SqlParameter GetStringParameterWithNonFilter(string propertyName, string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = "";
                    return new SqlParameter("@" + propertyName, value);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return new SqlParameter("@" + propertyName, value.Trim());
        }

        public static SqlParameter GetStringParameter_Null(string propertyName, string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return new SqlParameter("@" + propertyName, DBNull.Value);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return new SqlParameter("@" + propertyName, value.Trim());
        }

        /// <summary>
        /// To get boolean type SqlParameter for command object.
        /// </summary>
        /// <param name="propertyName">String property name</param>
        /// <param name="value">Integer value for parameter </param>
        /// <returns>SqlParameter which contans a valid parametername and value.</returns>
        public static SqlParameter GetBooleanParameter(string propertyName, bool value)
        {
            try
            {
                return new SqlParameter("@" + propertyName, value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /* Functions to retrieve values from reader */

        /// <summary>
        /// To get interger value from SqlDataReader object.
        /// </summary>
        /// <param name="readerObject">object value from reader.</param>
        /// <returns>a int value.</returns>
        public static int GetInt(object readerObject)
        {
            try
            {
                if (readerObject != DBNull.Value)
                {
                    return Convert.ToInt32(readerObject);
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// To get double value from SqlDataReader object.
        /// </summary>
        /// <param name="readerObject">object value from reader.</param>
        /// <returns>a double value.</returns>
        public static double GetDouble(object readerObject)
        {
            try
            {
                if (readerObject != DBNull.Value)
                {
                    return Convert.ToDouble(readerObject);
                }
                else
                {
                    return 0D;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// To get string value from SqlDataReader object.
        /// </summary>
        /// <param name="readerObject">object value from reader.</param>
        /// <returns>a string value.</returns>
        public static string GetString(object readerObject)
        {
            try
            {
                return readerObject.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// To get DateTime value from SqlDataReader object.
        /// </summary>
        /// <param name="readerObject">object value from reader.</param>
        /// <returns>a DateTime value.</returns>
        public static DateTime GetDateTime(object readerObject)
        {
            try
            {
                if (readerObject != DBNull.Value)
                {
                    return Convert.ToDateTime(readerObject);
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// To get bool value from SqlDataReader object.
        /// </summary>
        /// <param name="readerObject">object value from reader.</param>
        /// <returns>a bool value.</returns>
        public static bool GetBoolean(object readerObject)
        {
            try
            {
                if (readerObject != DBNull.Value)
                {
                    return Convert.ToBoolean(readerObject);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static SqlParameter GetByteParameter(string propertyName, byte[] value)
        {
            try
            {
                return new SqlParameter("@" + propertyName, value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static SqlParameter GetCharParameter(char propertyName, char value)
        {
            try
            {
                if (string.IsNullOrEmpty(value.ToString()))
                    return new SqlParameter("@" + propertyName, DBNull.Value);

                return new SqlParameter("@" + propertyName, value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static SqlParameter GetCharParameter(string propertyName, char value)
        {
            try
            {
                if (string.IsNullOrEmpty(value.ToString()))
                    return new SqlParameter("@" + propertyName, DBNull.Value);

                return new SqlParameter("@" + propertyName, value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string GetCharacter(object readerObject)
        {
            try
            {
                return readerObject.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// To get long type SqlParameter for command object.
        /// </summary>
        /// <param name="propertyName">String property name</param>
        /// <param name="value">Integer value for parameter </param>
        /// <returns>SqlParameter which contans a valid parametername and value.</returns>
        public static SqlParameter GetLongParameter(string propertyName, long value)
        {
            try
            {
                return new SqlParameter("@" + propertyName, value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static SqlParameter GetLongParameter(string propertyName, long value, bool allowDbNull)
        {
            try
            {
                if (!allowDbNull)
                    return new SqlParameter("@" + propertyName, value);


                if (value <= 0)
                    return new SqlParameter("@" + propertyName, DBNull.Value);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return new SqlParameter("@" + propertyName, value);
        }

        public static SqlParameter GetDateTimeParameter(string propertyName, DateTime? value)
        {
            SqlParameter o = new SqlParameter();//return new SqlParameter("@" + propertyName, value);
            try
            {
                if (value.HasValue)
                    o = new SqlParameter("@" + propertyName, value);
                else
                    o = new SqlParameter("@" + propertyName, DBNull.Value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return o;
        }

        internal static object GetIntParameter(string p, long EnqueryNo)
        {
            throw new NotImplementedException();
        }

        internal static SqlParameter GetString(string p, string flag)
        {
            throw new NotImplementedException();
        }
        public static SqlParameter GetTableParameter(string propertyName, DataTable value)
        {
            try
            {
                var x = new SqlParameter("@" + propertyName, value);
                x.SqlDbType = SqlDbType.Structured;
                return x;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static SqlParameter GetXmlParameter(string propertyName, XmlDocument value)
        {
            try
            {
                return new SqlParameter("@" + propertyName, value.OuterXml);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}