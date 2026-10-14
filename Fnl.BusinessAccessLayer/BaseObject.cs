using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Common.Config;
using Common.Utils;

namespace FnI.BusinessAccessLayer
{
    [Serializable]
    public abstract class BaseObject
    {
        public BaseObject()
        {
            PropertyInfo[] properties = this.GetType().GetProperties();

            foreach (PropertyInfo p in properties)
            {
                if(p.Name.EndsWith("_S"))
                p.SetValue(this, null, null);
            }
        }

        protected static string GetWhere(Object ClassObj, Enumaration.QueryType quertype)
        {
            string ClassName = ClassObj.GetType().Name;
            //string ClassName = ((DAL.BusinessLogicLayer.ProductionLocation)(ClassObj)).GetType().Name;
            string str = "";
            StringBuilder strWhere = new StringBuilder();

            PropertyInfo[] properties = ClassObj.GetType().GetProperties();
            foreach (PropertyInfo p in properties)
            {
                if (p.Name.Contains("_S"))
                {
                    Object ClassObjValue = p.GetValue(ClassObj, null);


                    //if (!Constants.CheckObjectForNullContants(ClassObjValue))
                    if (ClassObjValue != null)
                    {
                        if (!string.IsNullOrEmpty(ClassObjValue.ToString()))
                        {
                            //strWhere.Append("[" + p.Name + "] = '" + obj.ToString() + "' and ");
                            string ColumnName = p.Name.Replace("_S", "");
                            switch (quertype)
                            {
                                case Enumaration.QueryType.Equals:
                                    if (p.PropertyType.FullName.Contains("DateTime"))
                                    {
                                        strWhere.Append(ConvertDateSqlFormat(ColumnName, ClassName) + "= @" + ColumnName + " and ");
                                        //strWhere.Append("CONVERT ( Varchar , " + ClassName + ".[" + ColumnName + "] , 105 ) = @" + ColumnName + " and ");
                                    }
                                    else
                                        strWhere.Append(" " + ClassName + ".[" + ColumnName + "] = @" + ColumnName + " and ");
                                    break;

                                case Enumaration.QueryType.Like:
                                    if (p.PropertyType.FullName.Contains("DateTime"))
                                        strWhere.Append(ConvertDateSqlFormat(ColumnName) + " like '%' +  @" + ColumnName + " + '%' and ");
                                    else if(p.PropertyType.FullName.ToLower().Contains("int"))
                                        strWhere.Append(" " + ClassName + ".[" + ColumnName + "] like '%' + Convert(varchar(10), @" + ColumnName + ") + '%' and ");
                                    else
                                        strWhere.Append(" " + ClassName + ".[" + ColumnName + "] like '%' +  @" + ColumnName + " + '%' and ");
                                    break;
                            }
                        }
                    }
                }

            }

            str = strWhere.ToString();
            if (!string.IsNullOrEmpty(str))
            {
                str = "where " + str.Remove(str.LastIndexOf("and"));
            }
            return str;
        }


        public static string ConvertDateSqlFormat(string ColumnName)
        {
            return " CONVERT ( Varchar , [" + ColumnName + "] , 101 ) ";
        }

        public static string ConvertDateSqlFormat(string ColumnName, string ClassName)
        {
            return " CONVERT ( Varchar , " + ClassName + ".[" + ColumnName + "] , 101 ) ";
        }

        protected static List<SqlParameter> GetSqlParameters(Object ClassObj)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            SqlParameter sqlparam = null;

            PropertyInfo[] properties = ClassObj.GetType().GetProperties();
            foreach (PropertyInfo p in properties)
            {
                if (p.Name.Contains("_S"))
                {
                    Object ClassObjValue = p.GetValue(ClassObj, null);

                    //if (!Constants.CheckObjectForNullContants(ClassObjValue))
                    if (ClassObjValue != null)
                    {
                        string ColumnName = p.Name.Replace("_S", "");

                        if (!string.IsNullOrEmpty(ClassObjValue.ToString()))
                        {
                            if (p.PropertyType.FullName.Contains("DateTime"))
                            {
                                DateTime date = DateTime.Parse(ClassObjValue.ToString());
                                sqlparam = new SqlParameter("@" + ColumnName, DateUtil.ConvertToString(date, true));    //  SQL Server pattern
                            }
                            else
                            {
                                sqlparam = new SqlParameter("@" + ColumnName, ClassObjValue.ToString());
                            }
                            sqlparams.Add(sqlparam);
                        }
                    }
                }
            }

            return sqlparams;
        }

        public void InitializeToNullConstants()
        { 
                    PropertyInfo[] properties = this.GetType().GetProperties();
                    foreach (PropertyInfo p in properties)
                    {
                       p.SetValue(this,Constants.GetNullConstant(p.GetValue(this, null)),null);
                    }
        }

        /// <summary>
        /// IF SubQuery Used Outer Query Must Have Order By.
        /// </summary>
        protected static string ApplyPagingQuery(string Query)
        {
            StringBuilder str = new StringBuilder();
            str.Append("set @PageIndex = @PageIndex + 1;\r\n");
            str.Append("WITH ResultSet AS\r\n");
            str.Append("(\r\n");
            string TempQuery = Query.ToLower();
            
            string TempQueryLastFrom = TempQuery.Substring(TempQuery.LastIndexOf("from"));
            int IndexFirstFrom = TempQuery.IndexOf("from");

            if (TempQueryLastFrom.Contains("order by"))
            {
                string OrderByClause = TempQueryLastFrom.Substring(TempQueryLastFrom.IndexOf("order by"));
                Query = Query.Insert(IndexFirstFrom, ", ROW_NUMBER()  Over (" + OrderByClause + ") as 'Row' \r\n");
            }
            else
            { 
                //InCase There is no Order By Clause in the Query
                int AfterSelect =  TempQuery.IndexOf("select") + 6;
                int FirstComma = TempQuery.IndexOf(",");
                string FirstColumn = TempQuery.Substring(AfterSelect, FirstComma - AfterSelect);
                Query = Query.Insert(IndexFirstFrom, ", ROW_NUMBER() Over (Order by " + FirstColumn + ") as 'Row' \r\n");
            }
            str.Append(Query + "\r\n");
            str.Append(")\r\n");
            str.Append("select *\r\n");
            str.Append("from ResultSet\r\n");
            str.Append("WHERE Row between\r\n");
            str.Append("(@PageIndex - 1) * @PageSize + 1 and @PageIndex*@PageSize\r\n");
            return str.ToString();
        }

        /// <summary>
        /// IF SubQuery Used Outer Query Must Have Order By.
        /// Return Count For Paging
        /// </summary>
        protected static string ApplyPagingQueryWithCount(string Query)
        {
            StringBuilder str = new StringBuilder();
            //str.Append("declare @TotalRows int;\r\n");
            //str.Append("set @PageIndex = @PageIndex + 1;\r\n");
            str.Append("WITH ResultSet AS\r\n");
            str.Append("(\r\n");
            string TempQuery = Query.ToLower();

            string frommatch = "\\bfrom\\b";
            //var reg = new Regex(frommatch, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            //var matches = reg.Matches(TempQuery);

            //Field is not calculated here yet           
            MatchCollection m = Regex.Matches(TempQuery, frommatch, RegexOptions.Multiline);
            int IndexOf_LastOrderBy = m[m.Count - 1].Index; 
            //int IndexOf_LastOrderBy = TempQuery.LastIndexOf("from");

            string TempQueryLastFrom = TempQuery.Substring(IndexOf_LastOrderBy);
            //string TempQueryLastFrom = TempQuery.Substring(TempQuery.LastIndexOf("from",StringComparison.Ordinal));

            int IndexFirstFrom = Regex.Match(TempQuery, frommatch, RegexOptions.Multiline).Index;
            //int IndexFirstFrom = TempQuery.IndexOf("from", StringComparison.Ordinal);

            if (TempQueryLastFrom.Contains("order by"))
            {
                //Field Calculation Completed here
                IndexOf_LastOrderBy = IndexOf_LastOrderBy + TempQueryLastFrom.IndexOf("order by");
                string OrderByClause = TempQueryLastFrom.Substring(TempQueryLastFrom.IndexOf("order by"));
                Query = Query.Substring(0, IndexOf_LastOrderBy);
                Query = Query.Insert(IndexFirstFrom, ", ROW_NUMBER()  Over (" + OrderByClause + ") as 'Row' \r\n");
            }
            else
            {
                //InCase There is no Order By Clause in the Query
                int AfterSelect = TempQuery.IndexOf("select") + 6;
                int FirstComma = TempQuery.IndexOf(",");
                string FirstColumn = TempQuery.Substring(AfterSelect, FirstComma - AfterSelect).Replace("distinct","");
                Query = Query.Insert(IndexFirstFrom, ", ROW_NUMBER() Over (Order by " + FirstColumn + ") as 'Row' \r\n");
            }
            str.Append(Query + "\r\n");
            str.Append(")\r\n");
            str.Append("select *\r\n");
            str.Append("from ResultSet\r\n");
            str.Append("WHERE Row between\r\n");
            str.Append("(@PageIndex - 1) * @PageSize + 1 and @PageIndex*@PageSize;\r\n");
            str.Append("set @TotalRows = (select count(*)");
            str.Append("from");
            str.Append("(\r\n");
            str.Append(Query + "\r\n");
            str.Append(") as Query)\r\n");

            return str.ToString();
        }


    }
}
