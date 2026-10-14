using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;


namespace FnI.BusinessAccessLayer.Setup
{
    public class SET_CompanyPeriod : BaseObject
    {
        #region Fields

        private int f_CompanyID;
        private int f_PeriodID;
        private int f_ChartOfAccountID;

        private bool f_Status;

        private Guid f_CreatedBy;
        private Nullable<Guid> f_UpdatedBy;

        private DateTime f_CreateDate;
        private Nullable<DateTime> f_UpdateDate;

        private string f_CreatedByValue;
        private string f_UpdatedByValue;

        #endregion Fields

        #region Properties

        public int CompanyID
        {
            get { return this.f_CompanyID; }
            set { this.f_CompanyID = value; }
        }

        public int PeriodID
        {
            get { return this.f_PeriodID; }
            set { this.f_PeriodID = value; }
        }

        public int ChartOfAccountID
        {
            get { return this.f_ChartOfAccountID; }
            set { this.f_ChartOfAccountID = value; }
        }

        public Guid CreatedBy
        {
            get { return this.f_CreatedBy; }
            set { this.f_CreatedBy = value; }
        }

        public Nullable<Guid> UpdatedBy
        {
            get { return this.f_UpdatedBy; }
            set { this.f_UpdatedBy = value; }
        }

        public bool Status
        {
            get { return this.f_Status; }
            set { this.f_Status = value; }
        }

        public DateTime CreateDate
        {
            get { return this.f_CreateDate; }
            set { this.f_CreateDate = value; }
        }

        public Nullable<DateTime> UpdateDate
        {
            get { return this.f_UpdateDate; }
            set { this.f_UpdateDate = value; }
        }

        public string CreatedByValue
        {
            get { return this.f_CreatedByValue; }
            set { this.f_CreatedByValue = value; }
        }

        public string UpdatedByValue
        {
            get { return this.f_UpdatedByValue; }
            set { this.f_UpdatedByValue = value; }
        }

        #endregion Properties

        #region AutomaticPropertiesForSearching

        public int? MenuID_S { get; set; }

        public Guid? RoleId_S { get; set; }
        public Guid? CreatedBy_S { get; set; }
        public Guid? UpdatedBy_S { get; set; }

        public bool Status_S { get; set; }

        public DateTime? CreateDate_S { get; set; }
        public DateTime? UpdateDate_S { get; set; }

        public string CreatedByValue_S { get; set; }
        public string UpdatedByValue_S { get; set; }

        #endregion AutomaticPropertiesForSearching

        #region ConstantStrings

        const string Query_SelectAll = @" SELECT * FROM SET_CompanyPeriod";

        const string Query_Select_By_PrimaryKey = @" SELECT * FROM Set_CompanyPeriod WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [Status] = @Status";

        const string Query_SelectAll_for_Welcome = @"SELECT 
        Row_Number() OVER(Order by p.PeriodID) SerialNo, 
        p.PeriodSetName, 
        pt.PeriodTypeName, 
        p.PeriodYear, 
        p.PeriodNumber, 
        p.StartDate, 
        p.EndDate, 
        cch.Narration COADescription, 
        cch.Template, 
        cp.CompanyID, 
        cp.PeriodID, 
        cp.ChartOfAccountID,
        ou.BaseCurrencyID,
        0 as FiscalID
        FROM SET_CompanyPeriod cp 
        INNER JOIN SET_Period p on p.PeriodID = cp.PeriodID
        INNER JOIN SET_PeriodType pt on p.PeriodTypeID = pt.PeriodTypeID 
        INNER JOIN UsersInPeriod uip on uip.CompanyID = cp.CompanyID and uip.PeriodID = p.PeriodID and uip.UserId = @UserID 
        INNER JOIN GL_CodeCombinationHeader cch on cch.ChartOfAccountID = cp.ChartOfAccountID and cch.CompanyID = cp.CompanyID
        inner join SET_Company c on c.CompanyID = cp.CompanyID
        inner join SET_OperatingUnit ou on ou.OperatingUnitId =c.OperatingUnitId
        WHERE cp.Status = 1 and cp.CompanyID = @CompanyID";


        const string Query_SelectAll_for_WelcomeFiscal = @" select Row_Number() OVER(Order by p.PeriodID) SerialNo, 
        p.PeriodSetName, 
        p.PeriodTypeName, 
        right(datepart(year,p.StartDate),2) + '-'+ right(datepart(year,p.EndDate),2)  PeriodYear, 
        right(datepart(year,p.StartDate),2) + ''+ right(datepart(year,p.EndDate),2) PeriodNumber, 
        p.StartDate, 
        p.EndDate, 
        cch.Narration COADescription, 
        cch.Template, 
        cp.CompanyID, 
        cp.PeriodID, 
        p.FiscalID,
        cp.ChartOfAccountID,
        ou.BaseCurrencyID
        FROM SET_CompanyPeriod cp 
        INNER JOIN SET_PeriodCustom p on p.PeriodID = cp.PeriodID and p.status=1
        --INNER JOIN SET_PeriodType pt on p.PeriodTypeID = pt.PeriodTypeID 
        INNER JOIN UsersInPeriod uip on uip.CompanyID = cp.CompanyID and uip.FiscalID = p.FiscalID and uip.UserId = @UserID 
        inner JOIN GL_CodeCombinationHeader cch on cch.ChartOfAccountID = cp.ChartOfAccountID and cch.CompanyID = cp.CompanyID
        inner join SET_Company c on c.CompanyID = cp.CompanyID
        inner join SET_OperatingUnit ou on ou.OperatingUnitId =c.OperatingUnitId
        WHERE cp.Status = 1 and cp.CompanyID = @CompanyID
        order by p.PeriodTypeName
";
//        const string Query_SelectAll_for_Welcome = @"SELECT 
//                Row_Number() OVER(Order by p.PeriodID) SerialNo, 
//                p.PeriodSetName, 
//                pt.PeriodTypeName, 
//                p.PeriodYear, 
//                p.PeriodNumber, 
//--               p.StartDate, 
//--                p.EndDate, 
//
//                ISNULL(set_periodcustom.StartDate,p.StartDate) StartDate,
//                ISNULL(set_periodcustom.EndDate,p.EndDate) EndDate, 
//
//                cch.Narration COADescription, 
//                cch.Template, 
//                cp.CompanyID, 
//                cp.PeriodID, 
//                cp.ChartOfAccountID,
//                ou.BaseCurrencyID
//                FROM SET_CompanyPeriod cp 
//                INNER JOIN SET_Period p on p.PeriodID = cp.PeriodID
//                INNER JOIN SET_PeriodType pt on p.PeriodTypeID = pt.PeriodTypeID 
//                INNER JOIN UsersInPeriod uip on uip.CompanyID = cp.CompanyID and uip.PeriodID = p.PeriodID and uip.UserId = @UserID 
//                INNER JOIN GL_CodeCombinationHeader cch on cch.ChartOfAccountID = cp.ChartOfAccountID and cch.CompanyID = cp.CompanyID
//                inner join SET_Company c on c.CompanyID = cp.CompanyID
//                inner join SET_OperatingUnit ou on ou.OperatingUnitId =c.OperatingUnitId
//
//                left join set_periodcustom on set_periodcustom.periodid = p.periodid and set_periodcustom.status =1 and set_periodcustom.isactive=1
//
//                WHERE cp.Status = 1 and cp.CompanyID = @CompanyID";

        const string Query_Select_From_Period = @"Select Row_Number() OVER(Order by compPer.PeriodID) SerialNo, compPer.* From 
        (
            SELECT p.PeriodSetName, pt.PeriodTypeName, p.PeriodYear, p.StartDate, p.EndDate, cp.ChartOfAccountID, cp.CompanyID, cp.PeriodID, cp.Status, 
                cch.Narration Narration
            FROM SET_CompanyPeriod cp 
	            INNER JOIN SET_Period p on p.PeriodID = cp.PeriodID
	            INNER JOIN SET_PeriodType pt on p.PeriodTypeID = pt.PeriodTypeID
                INNER JOIN GL_CodeCombinationHeader cch on cch.ChartOfAccountID = cp.ChartOfAccountID and cch.CompanyID = cp.CompanyID
            WHERE cp.Status = 1 and cp.CompanyID = @CompanyID AND (cp.PeriodID = @PeriodID OR @PeriodID is null)
        
            Union 
        
            SELECT p.PeriodSetName, pt.PeriodTypeName, p.PeriodYear, p.StartDate, p.EndDate, -1 ChartOfAccountID, @CompanyID CompanyID, p.PeriodID, -1 Status, 
                '' Narration
            FROM SET_Period p 
	            INNER JOIN SET_PeriodType pt on p.PeriodTypeID = pt.PeriodTypeID
            WHERE p.Status = 1 and not exists (Select * from SET_CompanyPeriod cp Where cp.PeriodID = p.PeriodID and cp.CompanyID = @CompanyID 
                AND (cp.PeriodID = @PeriodID OR @PeriodID is null) and cp.Status = 1)
        ) as compPer";

        const string Query_Insert = @"INSERT INTO SET_CompanyPeriod(CompanyID, PeriodID, ChartOfAccountID, Status, CreatedBy, CreateDate, CreatedByValue) 
            Values (@CompanyID, @PeriodID, @ChartOfAccountID, @Status, @CreatedBy, @CreateDate, @CreatedByValue)";

        const string Query_Update_By_PrimaryKey = @" UPDATE SET_CompanyPeriod SET [ChartOfAccountID] = @ChartOfAccountID, [Status] = @Status, [UpdatedBy] = @UpdatedBy,         [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID";

        const string Query_Delete_By_PrimaryKey = @" UPDATE SET_CompanyPeriod SET [Status] = 0, [UpdatedBy] = @UpdatedBy, [UpdateDate] = GetDate(), [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID";

        const string Query_Select_Field = @" Select {0} [{1}] from SET_CompanyPeriod where [{2}] like '%' + @Value + '%'";

        const string Query_Select_ChartOfAccount_By_UserID = @"
SELECT Distinct SET_CompanyPeriod.ChartOfAccountID, GL_CodeCombinationHeader.Narration 
FROM SET_CompanyPeriod 
	INNER JOIN UsersInCompanies ON UsersInCompanies.CompanyID = SET_CompanyPeriod.CompanyID AND UsersInCompanies.UserId = @UserID 
	INNER JOIN UsersInPeriod ON UsersInPeriod.CompanyID = SET_CompanyPeriod.CompanyID AND UsersInPeriod.PeriodID = SET_CompanyPeriod.PeriodID 
        AND UsersInPeriod.UserId = @UserID 
	INNER JOIN GL_CodeCombinationHeader ON 
--GL_CodeCombinationHeader.CompanyID = SET_CompanyPeriod.CompanyID AND 
        GL_CodeCombinationHeader.ChartOfAccountID = SET_CompanyPeriod.ChartOfAccountID AND GL_CodeCombinationHeader.EnabledFlag = 1 
        AND GL_CodeCombinationHeader.Status = 1 
WHERE SET_CompanyPeriod.Status = 1 ";

        #endregion ConstantStrings

        #region Constructors

        public SET_CompanyPeriod()
            : base()
        { }

        public SET_CompanyPeriod(int companyID, int periodID)
        {
            DataTable dt = GetDataByKey(companyID, periodID);

            if (dt != null && dt.Rows.Count > 0)
                LoadThisObject(dt.Rows[0]);
        }

        #endregion Constructors

        #region Load Methods

        private void LoadThisObject(DataRow drThis)
        {
            this.f_ChartOfAccountID = Convert.ToInt32(drThis["ChartOfAccountID"]);
            this.f_CompanyID = Convert.ToInt32(drThis["CompanyID"]);
            this.f_CreateDate = Convert.ToDateTime(drThis["CreateDate"]);
            this.f_CreatedBy = Guid.Parse(drThis["CreatedBy"].ToString());
            this.f_CreatedByValue = Convert.ToString(drThis["CreatedByValue"]);
            this.f_PeriodID = Convert.ToInt32(drThis["PeriodID"]);
            this.f_Status = Convert.ToBoolean(drThis["Status"]);

            if (drThis["UpdateDate"] != DBNull.Value)
                this.f_UpdateDate = Convert.ToDateTime(drThis["UpdateDate"]);

            if (drThis["UpdatedBy"] != DBNull.Value)
                this.f_UpdatedBy = Guid.Parse(drThis["UpdatedBy"].ToString());

            this.f_UpdatedByValue = Convert.ToString(drThis["UpdatedByValue"]);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Used for All Data with and without Paging
        /// </summary>
        public static DataTable GetAllData(ref int ReturnTotalRows, int? PageIndex = null, int? PageSize = null)
        {
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    SqlParameter[] sqlparams = new SqlParameter[3];
                    sqlparams[0] = new SqlParameter("@PageIndex", PageIndex);
                    sqlparams[1] = new SqlParameter("@PageSize", PageSize);
                    sqlparams[2] = new SqlParameter("@TotalRows", ReturnTotalRows);
                    sqlparams[2].Direction = ParameterDirection.Output;
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, ApplyPagingQueryWithCount(Query_SelectAll), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparams[2].Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataFromPeriod(int companyID, Nullable<int> periodID = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                if (periodID.HasValue)
                    sqlparams[1] = new SqlParameter("@PeriodID", periodID.Value);
                else
                    sqlparams[1] = new SqlParameter("@PeriodID", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_From_Period, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataForWelcome(int companyID, Guid userID, bool isFiscalSystem=false) 
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@UserID", userID);

                string query = string.Empty;

                if (isFiscalSystem)
                    query = Query_SelectAll_for_WelcomeFiscal;
                else
                    query = Query_SelectAll_for_Welcome;


                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByKey(int companyID, int periodID, bool status = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Status", status);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetField(string FieldName, string Value, int? Top = null, bool? IsDateType = null)
        {
            DataTable dt = null;

            try
            {
                string str = string.Format(Query_Select_Field, (Top == null ? "" : "Top " + Top.ToString()), FieldName, (IsDateType == true ? BaseObject.ConvertDateSqlFormat(FieldName) : FieldName));
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@Value", Value);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, str, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        /// <summary>
        /// Used for All Data Sorted with and without Paging
        /// </summary>
        public static DataTable GetAllData(ref int ReturnTotalRows, string OrderBy, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int? PageIndex = null, int? PageSize = null)
        {
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    SqlParameter[] sqlparams = new SqlParameter[3];
                    sqlparams[0] = new SqlParameter("@PageIndex", PageIndex);
                    sqlparams[1] = new SqlParameter("@PageSize", PageSize);
                    sqlparams[2] = new SqlParameter("@TotalRows", ReturnTotalRows);
                    sqlparams[2].Direction = ParameterDirection.Output;
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, ApplyPagingQueryWithCount(Query_SelectAll + " Order By [" + OrderBy + "] " + sortdirection.ToString()), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparams[2].Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll + " Order By [" + OrderBy + "] " + sortdirection.ToString()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        /// <summary>
        /// Used for All Data Sorted with and without Paging
        /// </summary>
        public static DataTable GetAllData(SET_CompanyPeriod Obj, ref int ReturnTotalRows, string OrderBy, Enumaration.QueryType querytype = Enumaration.QueryType.Equals, int? PageIndex = null, int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            string strWhere = BaseObject.GetWhere(Obj, querytype);
            List<SqlParameter> sqlparamsList = BaseObject.GetSqlParameters(Obj);
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, ApplyPagingQueryWithCount(Query_SelectAll + " " + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll + " " + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool Insert(SqlTransaction objTrans)
        {
            bool result = false;
            bool commitTransaction = false;

            if (objTrans == null)
            {
                objTrans = SqlHelper.GetTransaction("Insert_CompanyPeriod");
                commitTransaction = true;
            }

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@ChartOfAccountID", f_ChartOfAccountID);
                sqlparams[3] = new SqlParameter("@Status", f_Status);
                sqlparams[4] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[5] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[6] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

                if (SqlHelper.ExecuteNonQuery(objTrans, CommandType.Text, Query_Insert, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (commitTransaction)
                {
                    if (result)
                        objTrans.Commit();
                    else
                        objTrans.Rollback();
                }
            }

            return result;
        }

        public bool UpdateDataByKey(SqlTransaction objTrans)
        {
            bool result = false;
            bool commitTransaction = false;

            if (objTrans == null)
            {
                objTrans = SqlHelper.GetTransaction("Insert_CompanyPeriod");
                commitTransaction = true;
            }

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@UpdatedBy", UpdatedBy);
                sqlparams[1] = new SqlParameter("@UpdateDate", UpdateDate);
                sqlparams[2] = new SqlParameter("@UpdatedByValue", UpdatedByValue);
                sqlparams[3] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[4] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[5] = new SqlParameter("@ChartOfAccountID", f_ChartOfAccountID);
                sqlparams[6] = new SqlParameter("@Status", f_Status);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (commitTransaction)
                {
                    if (result)
                        objTrans.Commit();
                    else
                        objTrans.Rollback();
                }
            }

            return result;
        }

        public static bool DeleteDataByKey(int companyID, int periodID, Guid updatedBy, string updatedByValue)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[3] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetChartOfAccountsByUserID(Guid userID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", userID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_ChartOfAccount_By_UserID, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        #endregion Methods
    }
}

