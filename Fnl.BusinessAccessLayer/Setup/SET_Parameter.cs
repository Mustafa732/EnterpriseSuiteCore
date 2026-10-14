using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_Parameter
    {
        #region Constant Strings

        const string Query_SelectByFieldNameDescription = @"SELECT [SerialNo]
        ,[FieldNameDescription]
        ,[FieldName]
        ,[FieldValue1]
        ,[FieldValue2]
        ,[FieldType]
        ,[IsVisible]
        FROM [SET_Parameter] where [FieldNameDescription]=@FieldNameDescription and IsVisible=1
        order by [SerialNo]";
        
        #endregion

        #region Class Methods

        public static List<SET_Parameter> GetAllDataList()
        {
            DataTable dt = GetAllActiveData();

            if (dt != null && dt.Rows.Count > 0)
            {
                List<SET_Parameter> objList = new List<SET_Parameter>(dt.Rows.Count);

                foreach (DataRow drParameter in dt.Rows)
                {
                    SET_Parameter objParameter = new SET_Parameter();

                    objParameter.f_FieldName = Convert.ToString(drParameter["FieldName"]);
                    objParameter.f_FieldNameDescription = Convert.ToString(drParameter["FieldNameDescription"]);
                    objParameter.f_FieldType = Convert.ToString(drParameter["FieldType"]);
                    objParameter.f_FieldValue1 = Convert.ToString(drParameter["FieldValue1"]);
                    objParameter.f_FieldValue2 = Convert.ToString(drParameter["FieldValue2"]);
                    objParameter.f_IsVisible = Convert.ToBoolean(drParameter["IsVisible"]);
                    objParameter.f_SerialNo = Convert.ToInt16(drParameter["SerialNo"]);

                    objList.Add(objParameter);
                }

                return objList;
            }
            else
                return null;
        }

        public static DataTable GetDataByFieldNameDescription(string FieldNameDescription,SqlTransaction obTransaction=null)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();

            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@FieldNameDescription", FieldNameDescription));

            SqlParameter[] sqlparams = sqlparamsList.ToArray();

            try
            {
                if (obTransaction != null)
                    dt = SqlHelper.ExecuteDataset(obTransaction,CommandType.Text, Query_SelectByFieldNameDescription, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectByFieldNameDescription, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        #endregion
    }
}