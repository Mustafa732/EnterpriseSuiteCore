using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_DepartmentsInCC
    {
        #region Fields

        private SET_CostCenter f_FK_CostCenterID_CostCenter;
        private SET_Department f_FK_DepartmentID_Department;

        private long f_DepartmentInCCID;
        private int f_FK_DepartmentID;
        private int f_FK_CostCenterID;
        private bool f_Status;
        private bool f_IsActive;
        private Guid f_CreatedBy;
        private DateTime f_CreateDate;
        private string f_CreatedByValue;
        private Nullable<Guid> f_UpdatedBy;
        private Nullable<DateTime> f_UpdateDate;
        private string f_UpdatedByValue;

        //	Not database fields
        private bool f_IsDataLoaded;

        #endregion

        #region Properties

        public SET_CostCenter FK_CostCenterID_CostCenter
        {
            get
            {
                if (this.f_FK_CostCenterID_CostCenter == null)
                    this.f_FK_CostCenterID_CostCenter = new SET_CostCenter(this.f_FK_CostCenterID);

                return this.f_FK_CostCenterID_CostCenter;
            }
        }

        public SET_Department FK_DepartmentID_Department
        {
            get
            {
                if (this.f_FK_DepartmentID_Department == null)
                    this.f_FK_DepartmentID_Department = new SET_Department(this.f_FK_DepartmentID);

                return this.f_FK_DepartmentID_Department;
            }
        }

        public long DepartmentInCCID
        {
            get { return f_DepartmentInCCID; }
            set { f_DepartmentInCCID = value; }
        }

        public int FK_DepartmentID
        {
            get { return f_FK_DepartmentID; }
            set { f_FK_DepartmentID = value; }
        }

        public int FK_CostCenterID
        {
            get { return f_FK_CostCenterID; }
            set { f_FK_CostCenterID = value; }
        }

        public bool Status
        {
            get { return f_Status; }
            set { f_Status = value; }
        }

        public bool IsActive
        {
            get { return f_IsActive; }
            set { f_IsActive = value; }
        }

        public Guid CreatedBy
        {
            get { return f_CreatedBy; }
            set { f_CreatedBy = value; }
        }

        public DateTime CreateDate
        {
            get { return f_CreateDate; }
            set { f_CreateDate = value; }
        }

        public string CreatedByValue
        {
            get { return f_CreatedByValue; }
            set { f_CreatedByValue = value; }
        }

        public Nullable<Guid> UpdatedBy
        {
            get { return f_UpdatedBy; }
            set { f_UpdatedBy = value; }
        }

        public Nullable<DateTime> UpdateDate
        {
            get { return f_UpdateDate; }
            set { f_UpdateDate = value; }
        }

        public string UpdatedByValue
        {
            get { return f_UpdatedByValue; }
            set { f_UpdatedByValue = value; }
        }

        public bool IsDataLoaded
        {
            get { return f_IsDataLoaded; }
        }

        #endregion

        #region Constant Strings

        const string Query_SelectAll = @" SELECT * FROM [SET_DepartmentsInCC] WHERE [Status] = 1 ";

        const string Query_SelectAll_WithPaging = @" SELECT [DepartmentInCCID], [FK_DepartmentID], [FK_CostCenterID], [Status], [IsActive], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue], ROW_NUMBER() Over (Order by DepartmentInCCID) as 'Row' FROM [SET_DepartmentsInCC] ";

        const string Query_Get_MaxId = @" SELECT IsNull(max(DepartmentInCCID), 0) + 1 as DepartmentInCCID FROM SET_DepartmentsInCC";

        const string Query_GetData_byKey = @" SELECT * FROM [SET_DepartmentsInCC] WHERE [DepartmentInCCID] = @DepartmentInCCID ";

        const string Query_Insert = @" INSERT INTO [SET_DepartmentsInCC] ([DepartmentInCCID], [FK_DepartmentID], [FK_CostCenterID], [Status], [IsActive], [CreatedBy], [CreateDate], [CreatedByValue])
		VALUES(@DepartmentInCCID, @FK_DepartmentID, @FK_CostCenterID, @Status, @IsActive, @CreatedBy, @CreateDate, @CreatedByValue) ";

        const string Query_Update_By_PrimaryKey = @" UPDATE [SET_DepartmentsInCC] SET [FK_DepartmentID] = @FK_DepartmentID, [FK_CostCenterID] = @FK_CostCenterID, [Status] = @Status, [IsActive] = @IsActive, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue
		WHERE [DepartmentInCCID] = @DepartmentInCCID ";

        const string Query_Delete_ByKey = @" Update [SET_DepartmentsInCC] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [DepartmentInCCID] = @DepartmentInCCID ";

        const string Query_Select_Field = @" SELECT {0} [{1}] FROM [SET_DepartmentsInCC] WHERE [{2}] like '%' + @Value + '%'";

        const string Paging = @"WITH ResultSet AS({0})
			SELECT * FROM ResultSet WHERE Row between (@PageIndex - 1) * @PageSize + 1 and @PageIndex * @PageSize;
			SET @TotalRows = (SELECT COUNT(*) FROM({1}) as Query)";

        const string Query_SelectAll_WithPaging_New = @"SELECT 
        [SET_DepartmentsInCC].[DepartmentInCCID], 
        [SET_DepartmentsInCC].[FK_DepartmentID], 
        [SET_Department].[DepartmentID],
        [SET_Department].[DepartmentName],
        [SET_DepartmentsInCC].[FK_CostCenterID], 
        [SET_DepartmentsInCC].[Status], 
        [SET_DepartmentsInCC].[IsActive], 
        [SET_DepartmentsInCC].[CreatedBy], 
        [SET_DepartmentsInCC].[CreateDate], 
        [SET_DepartmentsInCC].[CreatedByValue], 
        [SET_DepartmentsInCC].[UpdatedBy], 
        [SET_DepartmentsInCC].[UpdateDate], 
        [SET_DepartmentsInCC].[UpdatedByValue],
        '' Action, 
        ROW_NUMBER() Over (Order by DepartmentInCCID) as 'Row' 
        FROM [SET_DepartmentsInCC]
        INNER JOIN SET_Department ON SET_Department.DepartmentID = [SET_DepartmentsInCC].FK_DepartmentID";

        const string Query_DeleteAllPermanent = @"Delete FROM SET_DepartmentsInCC where FK_CostCenterID = @FK_CostCenterID";

        const string Query_GetCostCenterOnDepartments = @"SELECT
        DISTINCT        
        FK_DepartmentID,
        DepartmentID,
        DepartmentName,
        FK_CostCenterID,
        CostCenterID,
        CostCenterName
        FROM SET_DepartmentsInCC
        INNER JOIN SET_Department ON SET_Department.DepartmentID = SET_DepartmentsInCC.FK_DepartmentID
        INNER JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = SET_DepartmentsInCC.FK_CostCenterID
        WHERE FK_DepartmentID = @FK_DepartmentID
        AND SET_CostCenter.Status = 1
        ORDER BY FK_CostCenterID ASC";

        #endregion

        #region Constructors

        public SET_DepartmentsInCC()
        { }

        public SET_DepartmentsInCC(long departmentInCCID)
        {
            Load(departmentInCCID);
        }

        #endregion

        #region Loading Methods

        private void Load(long departmentInCCID)
        {
            DataRow drRecord = GetDataByKey(departmentInCCID);

            if (drRecord != null)
            {
                this.f_IsDataLoaded = true;
                this.f_DepartmentInCCID = Convert.ToInt64(drRecord["DepartmentInCCID"]);
                this.f_FK_DepartmentID = Convert.ToInt32(drRecord["FK_DepartmentID"]);
                this.f_FK_CostCenterID = Convert.ToInt32(drRecord["FK_CostCenterID"]);
                this.f_Status = Convert.ToBoolean(drRecord["Status"]);
                this.f_IsActive = Convert.ToBoolean(drRecord["IsActive"]);
                this.f_CreatedBy = Guid.Parse(drRecord["CreatedBy"].ToString());
                this.f_CreateDate = Convert.ToDateTime(drRecord["CreateDate"]);
                this.f_CreatedByValue = Convert.ToString(drRecord["CreatedByValue"]);

                if (drRecord["UpdatedBy"] != DBNull.Value)
                    this.f_UpdatedBy = Guid.Parse(drRecord["UpdatedBy"].ToString());


                if (drRecord["UpdateDate"] != DBNull.Value)
                    this.f_UpdateDate = Convert.ToDateTime(drRecord["UpdateDate"]);


                if (drRecord["UpdatedByValue"] != DBNull.Value)
                    this.f_UpdatedByValue = Convert.ToString(drRecord["UpdatedByValue"]);

            }
        }

        #endregion

        #region Class Methods

        public static DataTable GetAllActiveData()
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetDataByKey(long departmentInCCID)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@DepartmentInCCID", departmentInCCID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static int GetNewDepartmentInCCID(SqlTransaction objTransaction = null)
        {
            int ID = 0;

            try
            {
                if (objTransaction == null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxId));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxId));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetAllData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAlllData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPaging_New + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging_New + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithPaging_New + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool Insert(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@DepartmentInCCID", f_DepartmentInCCID);
                sqlparams[1] = new SqlParameter("@FK_DepartmentID", f_FK_DepartmentID);
                sqlparams[2] = new SqlParameter("@FK_CostCenterID", f_FK_CostCenterID);
                sqlparams[3] = new SqlParameter("@Status", f_Status);
                sqlparams[4] = new SqlParameter("@IsActive", f_IsActive);
                sqlparams[5] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[6] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[7] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Insert, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Insert, sqlparams) > 0)
                        result = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public bool UpdateDataByKey(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@DepartmentInCCID", f_DepartmentInCCID);
                sqlparams[1] = new SqlParameter("@FK_DepartmentID", f_FK_DepartmentID);
                sqlparams[2] = new SqlParameter("@FK_CostCenterID", f_FK_CostCenterID);
                sqlparams[3] = new SqlParameter("@Status", f_Status);
                sqlparams[4] = new SqlParameter("@IsActive", f_IsActive);

                if (f_UpdatedBy.HasValue)
                    sqlparams[5] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[5] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[6] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[6] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[7] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[7] = new SqlParameter("@UpdatedByValue", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool DeleteDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, long departmentInCCID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DepartmentInCCID", departmentInCCID);
                sqlparams[1] = new SqlParameter("@Status", false);
                sqlparams[2] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[3] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[4] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                        result = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool DeleteAllPermanent(SqlTransaction objTransaction, int costcenterID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FK_CostCenterID", costcenterID);


                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteAllPermanent, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteAllPermanent, sqlparams) > 0)
                        result = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;

        }

        public static DataTable GetCostCenterOnBehalfOfDepartments(int departmentID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@FK_DepartmentID", departmentID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCostCenterOnDepartments, sqlparams).Tables[0];
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
