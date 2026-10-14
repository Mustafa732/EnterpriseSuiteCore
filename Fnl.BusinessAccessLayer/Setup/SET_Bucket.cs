using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_Bucket
    {
        #region Queries

        const string Query_SelectAllForBucketQuery = @"
 SELECT * FROM [SET_Bucket] 
   WHERE [Status] = 1 AND [IsActive] = 1  
         AND ([CompanyID] = @CompanyID OR @CompanyID is null) 
		And [SET_Bucket].ToNoOfDays  between  @FromNoOfDays and @FromNoOfDays
";



        #endregion

        #region Method

        public static DataTable Query_SelectAllForBucket(Nullable<int> companyID, Nullable<short> FromNoOfDays, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                //if (ToNoOfDays.HasValue)
                //    sqlparams[2] = new SqlParameter("@ToNoOfDays", ToNoOfDays.Value);
                //else
                //    sqlparams[2] = new SqlParameter("@ToNoOfDays", DBNull.Value);

                if (FromNoOfDays.HasValue)
                    sqlparams[2] = new SqlParameter("@FromNoOfDays", FromNoOfDays.Value);
                else
                    sqlparams[2] = new SqlParameter("@FromNoOfDays", DBNull.Value);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllForBucketQuery, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllForBucketQuery, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForBucketQuery, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static bool DeleteDataByCompanyID(int companyID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);


                const string Query_DeleteDataByCompanyID = @" delete from SET_Bucket where CompanyID = @CompanyID";


                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDataByCompanyID, sqlparams) > 0)
                    result = true;



            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        public static bool InsertData(int companyID, int FromNo,int ToNo, string Description)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);


                string Query_DeleteDataByCompanyID = " insert into SET_Bucket values (isnull((select max(BucketID) from SET_Bucket),'0')+ 1 ,1," + companyID + ",'" + Description + "'," + FromNo + "," + ToNo + ",1,1,'A452FD2F-E6B4-4D5F-BEE9-DF5F23B99A59',getdate(),'Admin',null,null,null) ";

                if(ToNo == 0)
                    Query_DeleteDataByCompanyID = " insert into SET_Bucket values (isnull((select max(BucketID) from SET_Bucket),'0')+ 1 ,1," + companyID + ",'" + Description + "'," + FromNo + ",null,1,1,'A452FD2F-E6B4-4D5F-BEE9-DF5F23B99A59',getdate(),'Admin',null,null,null) ";


                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDataByCompanyID, sqlparams) > 0)
                    result = true;



            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        #endregion

    }
}
