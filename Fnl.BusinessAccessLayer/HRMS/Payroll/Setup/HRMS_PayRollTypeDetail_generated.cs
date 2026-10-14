using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
    public partial class HRMS_PayRollTypeDetail
    {
		#region Fields

		private int f_CompanyID;
		private int f_PayrollTypeID;
		private short f_PayrollTypeDetailID;
		private short f_EmployeeStatusCode;
		private bool f_Status;
		private Guid f_CreatedBy;
		private DateTime f_CreateDate;
		private string f_CreatedByValue;
		private Nullable<Guid> f_UpdatedBy;
		private Nullable<DateTime> f_UpdateDate;
		private string f_UpdatedByValue;
		private string f_GLcodecombination;
		private Nullable<int> f_ExpenseTypeID;

		//	Not database fields
		private bool f_IsDataLoaded;

		#endregion

		#region Properties

		public int CompanyID
		{
			get{ return f_CompanyID; }
			set{ f_CompanyID = value; }
		}

		public int PayrollTypeID
		{
			get{ return f_PayrollTypeID; }
			set{ f_PayrollTypeID = value; }
		}

		public short PayrollTypeDetailID
		{
			get{ return f_PayrollTypeDetailID; }
			set{ f_PayrollTypeDetailID = value; }
		}

		public short EmployeeStatusCode
		{
			get{ return f_EmployeeStatusCode; }
			set{ f_EmployeeStatusCode = value; }
		}

		public bool Status
		{
			get{ return f_Status; }
			set{ f_Status = value; }
		}

		public Guid CreatedBy
		{
			get{ return f_CreatedBy; }
			set{ f_CreatedBy = value; }
		}

		public DateTime CreateDate
		{
			get{ return f_CreateDate; }
			set{ f_CreateDate = value; }
		}

		public string CreatedByValue
		{
			get{ return f_CreatedByValue; }
			set{ f_CreatedByValue = value; }
		}

		public Nullable<Guid> UpdatedBy
		{
			get{ return f_UpdatedBy; }
			set{ f_UpdatedBy = value; }
		}

		public Nullable<DateTime> UpdateDate
		{
			get{ return f_UpdateDate; }
			set{ f_UpdateDate = value; }
		}

		public string UpdatedByValue
		{
			get{ return f_UpdatedByValue; }
			set{ f_UpdatedByValue = value; }
		}

		public string GLcodecombination
		{
			get{ return f_GLcodecombination; }
			set{ f_GLcodecombination = value; }
		}

		public Nullable<int> ExpenseTypeID
		{
			get{ return f_ExpenseTypeID; }
			set{ f_ExpenseTypeID = value; }
		}

		public bool IsDataLoaded
		{
			get{ return f_IsDataLoaded; }
		}

		#endregion

		#region Constant Strings

		const string Query_SelectAll = @" SELECT * FROM [HRMS_PayRollTypeDetail] WHERE [Status] = 1 AND ([PayrollTypeID] = @PayrollTypeID OR @PayrollTypeID is null) ";

		const string Query_SelectAll_WithPaging = @" SELECT [CompanyID], [PayrollTypeID], [PayrollTypeDetailID], [EmployeeStatusCode], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue], [GLcodecombination], [ExpenseTypeID], ROW_NUMBER() Over (Order by PayrollTypeDetailID, PayrollTypeID) as 'Row' FROM [HRMS_PayRollTypeDetail] "; 

		const string Query_SelectAll_WithoutPaging = @" SELECT [CompanyID], [PayrollTypeID], [PayrollTypeDetailID], [EmployeeStatusCode], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue], [GLcodecombination], [ExpenseTypeID] FROM [HRMS_PayRollTypeDetail] "; 

		const string Query_Get_MaxId = @" SELECT IsNull(max(PayrollTypeDetailID), 0) + 1 as PayrollTypeDetailID FROM HRMS_PayRollTypeDetail WHERE [PayrollTypeID] = @PayrollTypeID ";

		const string Query_GetData_byKey = @" SELECT * FROM [HRMS_PayRollTypeDetail] WHERE [PayrollTypeDetailID] = @PayrollTypeDetailID and [PayrollTypeID] = @PayrollTypeID ";

		const string Query_Insert = @" INSERT INTO [HRMS_PayRollTypeDetail] ([CompanyID], [PayrollTypeID], [PayrollTypeDetailID], [EmployeeStatusCode], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [GLcodecombination], [ExpenseTypeID])
		VALUES(@CompanyID, @PayrollTypeID, @PayrollTypeDetailID, @EmployeeStatusCode, @Status, @CreatedBy, @CreateDate, @CreatedByValue, @GLcodecombination, @ExpenseTypeID) ";

		const string Query_Update_By_PrimaryKey = @" UPDATE [HRMS_PayRollTypeDetail] SET [CompanyID] = @CompanyID, [EmployeeStatusCode] = @EmployeeStatusCode, [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue, [GLcodecombination] = @GLcodecombination, [ExpenseTypeID] = @ExpenseTypeID
		WHERE [PayrollTypeDetailID] = @PayrollTypeDetailID and [PayrollTypeID] = @PayrollTypeID ";

		const string Query_Delete_ByKey = @" Update [HRMS_PayRollTypeDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [PayrollTypeDetailID] = @PayrollTypeDetailID and [PayrollTypeID] = @PayrollTypeID ";

		const string Query_Select_Field = @" SELECT {0} [{1}] FROM [HRMS_PayRollTypeDetail] WHERE [{2}] like '%' + @Value + '%'";

		const string Paging = @"WITH ResultSet AS({0})
			SELECT * FROM ResultSet WHERE Row between (@PageIndex - 1) * @PageSize + 1 and @PageIndex * @PageSize;
			SET @TotalRows = (SELECT COUNT(*) FROM({1}) as Query)";

		#endregion

		#region Constructors

		public HRMS_PayRollTypeDetail()
		{ }

		public HRMS_PayRollTypeDetail(short payrollTypeDetailID, int payrollTypeID)
		{
			Load(payrollTypeDetailID, payrollTypeID);
		}

		public HRMS_PayRollTypeDetail(short payrollTypeDetailID, int payrollTypeID, SqlConnection objConnection)
		{
			Load(payrollTypeDetailID, payrollTypeID, objConnection);
		}

		public HRMS_PayRollTypeDetail(short payrollTypeDetailID, int payrollTypeID, SqlConnection objConnection, SqlTransaction objTransaction)
		{
			Load(payrollTypeDetailID, payrollTypeID, objConnection, objTransaction);
		}

		#endregion

		#region Loading Methods

		private void Load(short payrollTypeDetailID, int payrollTypeID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
		{
			DataRow drRecord = GetDataByKey(payrollTypeDetailID, payrollTypeID, objConnection, objTransaction);

			if (drRecord != null)
			{
				this.f_IsDataLoaded = true;
				this.f_CompanyID = Convert.ToInt32(drRecord["CompanyID"]);
				this.f_PayrollTypeID = Convert.ToInt32(drRecord["PayrollTypeID"]);
				this.f_PayrollTypeDetailID = Convert.ToInt16(drRecord["PayrollTypeDetailID"]);
				this.f_EmployeeStatusCode = Convert.ToInt16(drRecord["EmployeeStatusCode"]);
				this.f_Status = Convert.ToBoolean(drRecord["Status"]);
				this.f_CreatedBy = Guid.Parse(drRecord["CreatedBy"].ToString());
				this.f_CreateDate = Convert.ToDateTime(drRecord["CreateDate"]);
				this.f_CreatedByValue = Convert.ToString(drRecord["CreatedByValue"]);

				if (drRecord["UpdatedBy"] != DBNull.Value)
					this.f_UpdatedBy = Guid.Parse(drRecord["UpdatedBy"].ToString());


				if (drRecord["UpdateDate"] != DBNull.Value)
					this.f_UpdateDate = Convert.ToDateTime(drRecord["UpdateDate"]);


				if (drRecord["UpdatedByValue"] != DBNull.Value)
					this.f_UpdatedByValue = Convert.ToString(drRecord["UpdatedByValue"]);


				if (drRecord["GLcodecombination"] != DBNull.Value)
					this.f_GLcodecombination = Convert.ToString(drRecord["GLcodecombination"]);


				if (drRecord["ExpenseTypeID"] != DBNull.Value)
					this.f_ExpenseTypeID = Convert.ToInt32(drRecord["ExpenseTypeID"]);

			}
		}

		#endregion

		#region Class Methods

		public static DataTable GetAllActiveData(Nullable<int> payrollTypeID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
		{
			DataTable dt = null;

			try
			{
				SqlParameter[] sqlparams = new SqlParameter[1];
				sqlparams[0] = new SqlParameter("@PayrollTypeID", payrollTypeID);

				DataSet ds = null;

				if(objTransaction != null)
					ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll, sqlparams);
				else if(objConnection != null)
					ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll, sqlparams);
				else
					ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll, sqlparams);

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

		public static DataRow GetDataByKey(short payrollTypeDetailID, int payrollTypeID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
		{
			DataRow dr = null;

			try
			{
				SqlParameter[] sqlparams = new SqlParameter[2];
				sqlparams[0] = new SqlParameter("@PayrollTypeDetailID", payrollTypeDetailID);
				sqlparams[1] = new SqlParameter("@PayrollTypeID", payrollTypeID);

				DataSet ds = null;

				if(objTransaction != null)
					ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKey, sqlparams);
				else if(objConnection != null)
					ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKey, sqlparams);
				else
					ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey, sqlparams);

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

		public static int GetNewPayrollTypeDetailID(int payrollTypeID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
		{
			int ID = 0;

			try
			{
				SqlParameter[] sqlparams = new SqlParameter[1];
				sqlparams[0] = new SqlParameter("@PayrollTypeID", payrollTypeID);

				if(objTransaction != null)
					ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxId, sqlparams));
				else if(objConnection != null)
					ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxId, sqlparams));
				else
					ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxId, sqlparams));
			}
			catch (Exception ex)
			{
				SqlHelper.LogException(ex);
				throw ex;
			}

			return ID;
		}

		public static DataTable GetAllData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, 
			Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
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

					if(objTransaction != null)
						dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
					else
						dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

					ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
				}
				else
				{
					if(objTransaction != null)
						dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
					else
						dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
				}
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
				SqlParameter[] sqlparams = new SqlParameter[10];
				sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
				sqlparams[1] = new SqlParameter("@PayrollTypeID", f_PayrollTypeID);
				sqlparams[2] = new SqlParameter("@PayrollTypeDetailID", f_PayrollTypeDetailID);
				sqlparams[3] = new SqlParameter("@EmployeeStatusCode", f_EmployeeStatusCode);
				sqlparams[4] = new SqlParameter("@Status", f_Status);
				sqlparams[5] = new SqlParameter("@CreatedBy", f_CreatedBy);
				sqlparams[6] = new SqlParameter("@CreateDate", f_CreateDate);
				sqlparams[7] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

				if(!string.IsNullOrEmpty(f_GLcodecombination))
					sqlparams[8] = new SqlParameter("@GLcodecombination", f_GLcodecombination);
				else
					sqlparams[8] = new SqlParameter("@GLcodecombination", DBNull.Value);

				if(f_ExpenseTypeID.HasValue)
					sqlparams[9] = new SqlParameter("@ExpenseTypeID", f_ExpenseTypeID.Value);
				else
					sqlparams[9] = new SqlParameter("@ExpenseTypeID", DBNull.Value);

				if(objTransaction == null)
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
				SqlParameter[] sqlparams = new SqlParameter[10];
				sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
				sqlparams[1] = new SqlParameter("@PayrollTypeID", f_PayrollTypeID);
				sqlparams[2] = new SqlParameter("@PayrollTypeDetailID", f_PayrollTypeDetailID);
				sqlparams[3] = new SqlParameter("@EmployeeStatusCode", f_EmployeeStatusCode);
				sqlparams[4] = new SqlParameter("@Status", f_Status);

				if(f_UpdatedBy.HasValue)
					sqlparams[5] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
				else
					sqlparams[5] = new SqlParameter("@UpdatedBy", DBNull.Value);

				if(f_UpdateDate.HasValue)
					sqlparams[6] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
				else
					sqlparams[6] = new SqlParameter("@UpdateDate", DBNull.Value);

				if(!string.IsNullOrEmpty(f_UpdatedByValue))
					sqlparams[7] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
				else
					sqlparams[7] = new SqlParameter("@UpdatedByValue", DBNull.Value);

				if(!string.IsNullOrEmpty(f_GLcodecombination))
					sqlparams[8] = new SqlParameter("@GLcodecombination", f_GLcodecombination);
				else
					sqlparams[8] = new SqlParameter("@GLcodecombination", DBNull.Value);

				if(f_ExpenseTypeID.HasValue)
					sqlparams[9] = new SqlParameter("@ExpenseTypeID", f_ExpenseTypeID.Value);
				else
					sqlparams[9] = new SqlParameter("@ExpenseTypeID", DBNull.Value);

				if(objTransaction == null)
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

		public static bool DeleteDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, short payrollTypeDetailID, int payrollTypeID)
		{
			bool result = false;

			try
			{
				SqlParameter[] sqlparams = new SqlParameter[6];
				sqlparams[0] = new SqlParameter("@PayrollTypeDetailID", payrollTypeDetailID);
				sqlparams[1] = new SqlParameter("@PayrollTypeID", payrollTypeID);
				sqlparams[2] = new SqlParameter("@Status", false);
				sqlparams[3] = new SqlParameter("@UpdatedBy", updatedBy);
				sqlparams[4] = new SqlParameter("@UpdateDate", updateDate);
				sqlparams[5] = new SqlParameter("@UpdatedByValue", updatedByValue);

				if(objTransaction == null)
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

		#endregion
	}
}
