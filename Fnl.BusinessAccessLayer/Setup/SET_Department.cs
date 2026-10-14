using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
   public partial class SET_Department

    {

        const string Query_SelectAllFromDepartment = @" SELECT * FROM [SET_Department] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) order by  DepartmentName ";
        
       const string Query_SelectAllCostCenterDepID = @"
SELECT SET_CostCenter.CostCenterID,SET_CostCenter.CostCenterName
FROM SET_DepartmentsinCC
inner join SET_CostCenter on SET_CostCenter.CostCenterID = SET_DepartmentsinCC.FK_CostCenterID
and SET_CostCenter.Status = 1 and SET_CostCenter.IsActive = 1
left join SET_Department on SET_Department.DepartmentID = SET_DepartmentsinCC.FK_DepartmentID
and SET_Department.Status = 1 and SET_Department.IsActive = 1
{0}
";
       const string Query_SelectAllCostCenterByCostCID = @"SELECT SET_CostCenter.CostCenterID,SET_CostCenter.CostCenterName
FROM SET_DepartmentsinCC
inner join SET_CostCenter on SET_CostCenter.CostCenterID = SET_DepartmentsinCC.FK_CostCenterID
and SET_CostCenter.Status = 1 and SET_CostCenter.IsActive = 1
left join SET_Department on SET_Department.DepartmentID = SET_DepartmentsinCC.FK_DepartmentID
and SET_Department.Status = 1 and SET_Department.IsActive = 1
{0}";

       const string Query_Get_Data_For_DepartmentReport = @" Select 
		ROW_NUMBER() over (order by DepartmentName) as SerialNumber,
		Set_Department.DepartmentID,
		Set_Department.DepartmentName,
		Case when Set_Department.IsActive='1' then 'True' else 'False'end as IsActive,
		Set_Department.CreateDate,
		Set_Department.CreatedBy,
		Set_Department.CreatedByValue,
		Set_Department.UpdateDate,
		Set_Department.UpdatedBy,
		Set_Department.UpdatedByValue
		
 from Set_Department
	Where status=1 
";

       const string Query_SelectProductionData = @" SELECT * FROM [SET_Department] where [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null)  and DepartmentName like '%Production%' ";

       const string Query_SelectDefaultCostCenter = @" SELECT * FROM [SET_CostCenter] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null) AND CostCenterName = 'N/A' ";
       const string Query_SelectDefaultAllCostCenter = @" SELECT * FROM [SET_CostCenter] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null)";

#region Class Methods
        public static DataTable GetAllActiveDataWithSort(Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllFromDepartment, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetProductionData(Nullable<bool> isActive = true, SqlConnection obj_SQL_Connection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                if (obj_SQL_Connection != null)
                    dt = SqlHelper.ExecuteDataset(obj_SQL_Connection, CommandType.Text, Query_SelectProductionData, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectProductionData, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllCostCenterDepID(Nullable<int> DepartmentID = 0)
        {
            DataTable dt = null;
            string Filter = "";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (DepartmentID != 0)
                    sqlparams[0] = new SqlParameter("@DepartmentID", DepartmentID);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);


                if (DepartmentID != 0)
                    Filter = " where SET_Department.DepartmentID = " + DepartmentID;

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllCostCenterDepID,Filter) , sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetAllCostCenterByCostCID(Nullable<int> CostCenterID)
        {
            DataTable dt = null;
            string Filter = "";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (CostCenterID != 0)
                    sqlparams[0] = new SqlParameter("@CostCenterID", CostCenterID);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);


                if (CostCenterID != 0)
                    Filter = " where SET_CostCenter.CostCenterID = " + CostCenterID;

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllCostCenterByCostCID, Filter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable DepartmentReport(DateTime? DateFrom, DateTime? DateTo, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[2];

                //sqlParms[0] = new SqlParameter("@CompanyID", CompanyID);
                //sqlParms[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlParms[0] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[1] = new SqlParameter("@ToDate", DateTo);

                if (DateFrom != null && DateTo != null)
                {
                    AddtionalParameters = AddtionalParameters + " AND [Set_Department].CreateDate between @FromDate AND @ToDate";
                    AddtionalParameters = AddtionalParameters + " ORDER BY SET_Department.DepartmentName";
                }

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Get_Data_For_DepartmentReport + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_Get_Data_For_DepartmentReport + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetCostCenterData(Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectDefaultCostCenter, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllCostCenterData(Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectDefaultAllCostCenter, sqlparams).Tables[0];
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
