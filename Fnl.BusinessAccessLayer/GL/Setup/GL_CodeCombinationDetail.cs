using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.GL.Setup
{
    public partial class GL_CodeCombinationDetail
    {
        #region constantstrings

        const string Query_SelectAll_Import = @" SELECT * FROM [GL_CodeCombinationDetail] WHERE [Status] = 1 and IsActive = 1 ";

        const string Query_Update_IsTaxAccount = @" UPDATE [GL_CodeCombinationDetail] SET [IsTaxAccount] = @CheckValue 
where [CodeCombinationID]=@CodeCombinationID and [ChartOfAccountID]=@ChartOfAccountID --and [CompanyID]=@CompanyID ";

        const string Query_SelectAll_WithPaging_WithOtherTables = @" SELECT ccd.[ChartOfAccountID], ccd.[CodeCombinationID], ccd.[AccountTypeID], ccd.[Description], 
                ccd.[DetailBudgetingAllowedFlag], ccd.[DetailPostingAllowedFlag], ccd.[SummaryFlag], ccd.[CreatedBy], ccd.[CreateDate], ccd.[UpdatedBy], 
                ccd.[UpdateDate], [CreatedByValue], ccd.[UpdatedByValue], ccd.[SerialNo], ccd.[CompanyID], ccd.[Status], ccd.[HeadAccountCode], c.CompanyName, 
                ROW_NUMBER() Over (Order by ccd.ChartOfAccountID, ccd.CodeCombinationID, ccd.CompanyID) as 'Row' 
            FROM [GL_CodeCombinationDetail] ccd 
                INNER JOIN [Set_Company] c on c.CompanyID = ccd.CompanyID ";

        const string Query_SelectAll_ByCompanyAndChartOfAccID = @" SELECT 
convert(varchar(Max), 
ROW_NUMBER() OVER (ORDER BY GL_CodeCombinationDetail.ChartOfAccountID, 
GL_CodeCombinationDetail.CodeCombinationID, 
GL_CodeCombinationDetail.[Description])) + '=' + GL_CodeCombinationDetail.[Description] AS ID, 
GL_CodeCombinationDetail.ChartOfAccountID, 
GL_CodeCombinationDetail.CodeCombinationID, 
GL_CodeCombinationDetail.[Description], 
SET_AccountType.AccountTypeName, 

(SELECT ccd.Description 
 FROM GL_CodeCombinationDetail ccd
 WHERE ccd.CodeCombinationID = GL_CodeCombinationDetail.HeadAccountCode and ccd.[ChartOfAccountID] = GL_CodeCombinationDetail.ChartOfAccountID 
--AND ccd.[CompanyID] = GL_CodeCombinationDetail.CompanyID 
AND ccd.[Status] = 1 
AND (ccd.[IsActive] = @IsActive or @IsActive is null)) AS HeadDescription,

GL_CodeCombinationDetail.HeadAccountCode, 
GL_CodeCombinationDetail.AccountTypeID, 
GL_CodeCombinationDetail.DetailBudgetingAllowedFlag, 
GL_CodeCombinationDetail.DetailPostingAllowedFlag, 
GL_CodeCombinationDetail.SerialNo, 
GL_CodeCombinationDetail.SummaryFlag 
FROM GL_CodeCombinationDetail
INNER JOIN SET_AccountType ON SET_AccountType.AccountTypeID = GL_CodeCombinationDetail.AccountTypeID 
inner join UsersInAccountCodes on  UsersInAccountCodes.CodeCombinationID = GL_CodeCombinationDetail.CodeCombinationID
and UsersInAccountCodes.AccountTypeID = GL_CodeCombinationDetail.AccountTypeID
and UsersInAccountCodes.UserID =  @UserID
WHERE GL_CodeCombinationDetail.[ChartOfAccountID] = @ChartOfAccountID 
--AND GL_CodeCombinationDetail.[CompanyID] = @CompanyID 
AND GL_CodeCombinationDetail.[Status] = 1 
AND (GL_CodeCombinationDetail.[IsActive] = @IsActive or @IsActive is null) 
AND (GL_CodeCombinationDetail.[DetailPostingAllowedFlag] = @DetailPostingAllowedFlag or @DetailPostingAllowedFlag is null) 

{0}
ORDER BY GL_CodeCombinationDetail.CodeCombinationID ";

        const string Query_Select_HeadCodes = @" Select Distinct ccd.ChartOfAccountID, ccd.CodeCombinationID, ccd.[Description], act.AccountTypeName 
            from GL_CodeCombinationDetail ccd 
	            Inner Join
	            (
		            SELECT HeadAccountCode FROM GL_CodeCombinationDetail 
		            WHERE ChartOfAccountID = @ChartOfAccountID And CompanyID = @CompanyID And [Status] = 1 And ([IsActive] = @IsActive or @IsActive is null) 
                        and (SummaryFlag = @SummaryFlag or @SummaryFlag is null) 
                        and (DetailPostingAllowedFlag = @DetailPostingAllowedFlag or @DetailPostingAllowedFlag is null)
	            ) InnerCCDetail on InnerCCDetail.HeadAccountCode = ccd.CodeCombinationID
	            Inner Join SET_AccountType act on act.AccountTypeID = ccd.AccountTypeID 
            Order By ccd.CodeCombinationID ";

        const string Query_MaxSerialNo = @"SELECT ISNULL(MAX(SerialNo),0)+1 FROM GL_CodeCombinationDetail 
        WHERE 
--GL_CodeCombinationDetail.[CompanyID] = @CompanyID AND 
        GL_CodeCombinationDetail.[ChartOfAccountID] = @ChartOfAccountID";

        const string Query_Select_By_Accounttype = @" SELECT ISNULL(MAX(AccountTypeID), 0) AS AccountTypeID FROM GL_CodeCombinationDetail 
            WHERE 
--GL_CodeCombinationDetail.[CompanyID] = @CompanyID AND 
GL_CodeCombinationDetail.[ChartOfAccountID] = @ChartOfAccountID 
                AND GL_CodeCombinationDetail.[CodeCombinationID] = @CodeCombinationID ";

        const string Query_IsExpenseGLCode = @"
IF EXISTS (
select AccountTypeName, * from GL_CodeCombinationDetail
inner join SET_AccountType on SET_AccountType.AccountTypeID = GL_CodeCombinationDetail.AccountTypeID
where 
	SET_AccountType.Status = 1
	--and GL_CodeCombinationDetail.CompanyID = @CompanyID
	and SET_AccountType.AccountTypeName like 'Expense%'
	and GL_CodeCombinationDetail.CodeCombinationID = @CodeCombinationID
	and GL_CodeCombinationDetail.ChartOfAccountID = @ChartOfAccountID
	) 
select 1 
else 
select 0
";

        const string Query_Select_By_PrimaryKey = @" SELECT CompanyID, ChartOfAccountID, CodeCombinationID,TaxCode, [Description], AccountTypeID, AccountTypeName, 
                DetailBudgetingAllowedFlag, DetailPostingAllowedFlag, SummaryFlag, Template, [Status], IsActive , SaleTaxFlag,WithHoldingTaxFlag,OSCode,ControlAccount {0}
,SaleWithHoldingTaxFlag,BankChargesAccount,BankProfitAccount
            FROM 
            (
                SELECT GL_CodeCombinationDetail.[CompanyID], GL_CodeCombinationDetail.[ChartOfAccountID], GL_CodeCombinationDetail.[CodeCombinationID], 
                    GL_CodeCombinationDetail.[Description], GL_CodeCombinationDetail.[DetailBudgetingAllowedFlag],
                    GL_CodeCombinationDetail.TaxCode, 
                    GL_CodeCombinationDetail.[DetailPostingAllowedFlag], GL_CodeCombinationDetail.[SummaryFlag], 
                    GL_CodeCombinationDetail.[Status], GL_CodeCombinationDetail.[AccountTypeID], SET_AccountType.[AccountTypeName], GL_CodeCombinationHeader.[Template], GL_CodeCombinationDetail.[isActive]
                    ,isnull(SaleTaxFlag,0) as SaleTaxFlag,isnull(WithHoldingTaxFlag,0) as WithHoldingTaxFlag,GL_CodeCombinationDetail.OSCode,isnull(ControlAccount,0) as ControlAccount {1}
,isnull(SaleWithHoldingTaxFlag,0) as SaleWithHoldingTaxFlag,isnull(BankChargesAccount,0) as BankChargesAccount,isnull(BankProfitAccount,0) as BankProfitAccount
                    FROM GL_CodeCombinationHeader 
                    INNER JOIN GL_CodeCombinationDetail ON 
--GL_CodeCombinationHeader.[CompanyID] = GL_CodeCombinationDetail.[CompanyID] AND 
GL_CodeCombinationHeader.[ChartOfAccountID] = GL_CodeCombinationDetail.[ChartOfAccountID] 
                    INNER JOIN SET_AccountType ON GL_CodeCombinationDetail.[AccountTypeID] = SET_AccountType.[AccountTypeID] 
            ) COA 
            WHERE COA.Status = 1 AND (COA.IsActive = @IsActive OR @IsActive = -1) AND COA.ChartOfAccountID = @ChartOfAccountID 
                AND (COA.CodeCombinationID = @CodeCombinationID or @CodeCombinationID is null)
--AND COA.CompanyID = @CompanyID ";

        const string Query_GetByCompany = @"select * from GL_CodeCombinationDetail where status = 1 and CompanyID = @CompanyID";

        const string Query_GetCOAReport = @"
";

        const string Query_GetAll = @"SELECT COA.CompanyID, COA.ChartOfAccountID, COA.CodeCombinationID, COA.[Description], COA.AccountTypeID, COA.AccountTypeName, COA.[DetailBudgetingAllowedFlag], COA.[DetailPostingAllowedFlag],
        COA.[SummaryFlag], COA.[Template], COA.[Status],HeadDescription, ROW_NUMBER() OVER (ORDER BY  
        COA.[CodeCombinationID]) AS 'Row' FROM (
        SELECT GL_CodeCombinationDetail.[CompanyID], GL_CodeCombinationDetail.[ChartOfAccountID],GL_CodeCombinationDetail.[CodeCombinationID] ,GL_CodeCombinationDetail.[Description], 
        GL_CodeCombinationDetail.[DetailBudgetingAllowedFlag],GL_CodeCombinationDetail.[DetailPostingAllowedFlag],GL_CodeCombinationDetail.[SummaryFlag] ,GL_CodeCombinationDetail.[Status] ,GL_CodeCombinationDetail.[AccountTypeID],SET_AccountType.[AccountTypeName] ,
        GL_CodeCombinationHeader.[Template], GL_CodeCombinationDetail.[IsActive] 
        ,(SELECT ccd.Description 
 FROM GL_CodeCombinationDetail ccd
 WHERE ccd.CodeCombinationID = GL_CodeCombinationDetail.HeadAccountCode and ccd.[ChartOfAccountID] = GL_CodeCombinationDetail.ChartOfAccountID 
--AND ccd.[CompanyID] = GL_CodeCombinationDetail.CompanyID 
AND ccd.[Status] = 1 
AND (ccd.[IsActive] = Null or Null is null)) AS HeadDescription
        FROM GL_CodeCombinationHeader INNER JOIN
        GL_CodeCombinationDetail ON (GL_CodeCombinationHeader.[CompanyID] = GL_CodeCombinationDetail.[CompanyID] AND
									 GL_CodeCombinationHeader.[ChartOfAccountID] = GL_CodeCombinationDetail.[ChartOfAccountID]) INNER JOIN
        SET_AccountType ON (GL_CodeCombinationDetail.[AccountTypeID] = SET_AccountType.[AccountTypeID])) COA";

        const string Query_Update_AccountType = @"UPDATE GL_CodeCombinationDetail SET      
        [AccountTypeID]=@AccountTypeID
		WHERE  CompanyID = @CompanyID AND ChartOfAccountID = @ChartOfAccountID AND 
        SUBSTRING(CodeCombinationID, 1,@LengthofFirstLevel) = @CodeCombinationID";

        const string Query_TopExpensive = @"Select Top 5 GL_CodeCombinationDetail.Description , Set_AccountType.AccountTypeName
From GL_CodeCombinationDetail
Inner Join Set_AccountType On (GL_CodeCombinationDetail.AccountTypeID = SET_AccountType.AccountTypeID )
Where Set_AccountType.AccountTypeName = 'Expenses' AND Set_AccountType.Status = 1 AND GL_CodeCombinationDetail.Status = 1
--And GL_CodeCombinationDetail.CompanyID = @CompanyID
";

        const string Query_GetDescriptionList = @"
select [Description] from GL_CodeCombinationDetail where [Description] like @Description + '%'
";

        #endregion

        #region Methods

        public static DataTable GetTopExpensive(Nullable<int> companyID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_TopExpensive, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_GetAll + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_GetAll + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAll + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public int GetMaxSerialNo(int mCompanyID, int mChartOfAccountID)
        {
            int MaxSerialNo = 0;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", mCompanyID);
                sqlparams[1] = new SqlParameter("@ChartOfAccountID", mChartOfAccountID);
                MaxSerialNo = int.Parse(SqlHelper.ExecuteDataset(CommandType.Text, Query_MaxSerialNo, sqlparams).Tables[0].Rows[0][0].ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return MaxSerialNo;
        }

        public int GetAccountType(int CompanyID, int ChartOfAccountID, string CodeCombinationID)
        {
            int AccountTypeID = 0;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@ChartOfAccountID", ChartOfAccountID);
                sqlparams[2] = new SqlParameter("@CodeCombinationID", CodeCombinationID);
                AccountTypeID = int.Parse(SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_Accounttype, sqlparams).Tables[0].Rows[0][0].ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return AccountTypeID;
        }

        public static bool IsExpenseGLCode(int CompanyID, int ChartOfAccountID, string CodeCombinationID)
        {
            bool status = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@ChartOfAccountID", ChartOfAccountID);
                sqlparams[2] = new SqlParameter("@CodeCombinationID", CodeCombinationID);

                status = Convert.ToBoolean(SqlHelper.ExecuteScalar(CommandType.Text, Query_IsExpenseGLCode, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return status;
        }

        public static DataTable GetAccountCode(int CompanyID, int ChartOfAccountID, string CodeCombinationID, int StartLen, int EndLen, int isActive, string FormType)
        {
            DataTable dtCOADetail = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@ChartOfAccountID", ChartOfAccountID);
                sqlparams[2] = new SqlParameter("@CodeCombinationID", CodeCombinationID);
                sqlparams[3] = new SqlParameter("@StartLen", StartLen);
                sqlparams[4] = new SqlParameter("@EndLen", EndLen);
                sqlparams[5] = new SqlParameter("@IsActive", isActive);
                sqlparams[6] = new SqlParameter("@FormType", FormType);
                dtCOADetail = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_Select_By_AccountCode, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtCOADetail;
        }

        public static bool UpdateAccountType(int CompanyID,int ChartOfAccountID,string CodeCombinationID, bool CheckValue)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@ChartOfAccountID", ChartOfAccountID);
                sqlparams[2] = new SqlParameter("@CodeCombinationID", CodeCombinationID);
                sqlparams[3] = new SqlParameter("@CheckValue", CheckValue);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_IsTaxAccount, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in update class code combination detail " + ex.Message);
                throw ex;
            }
            return result;
        }

        public bool UpdateAccountType(SqlTransaction trans, int nLengthofFirstLevel)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@ChartOfAccountID", ChartOfAccountID);
                sqlparams[2] = new SqlParameter("@CodeCombinationID", CodeCombinationID);
                sqlparams[3] = new SqlParameter("@AccountTypeID", AccountTypeID);
                sqlparams[4] = new SqlParameter("@LengthofFirstLevel", nLengthofFirstLevel);
                if (SqlHelper.ExecuteNonQuery(trans, CommandType.Text, Query_Update_AccountType, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error in update class code combination detail " + ex.Message);
                throw ex;
            }
            return result;
        }

        public static DataTable GetAllData(int CompanyId, int ChartOfAccountId, string filter, bool? DetailPostingAllowedFlag = true, bool? isActive = true, bool? SummaryFlag = false,string UserID = "")
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyId);
                sqlparams[1] = new SqlParameter("@ChartOfAccountId", ChartOfAccountId);
                sqlparams[2] = new SqlParameter("@IsActive", isActive);
                sqlparams[3] = new SqlParameter("@SummaryFlag", SummaryFlag);
                sqlparams[4] = new SqlParameter("@DetailPostingAllowedFlag", DetailPostingAllowedFlag);
                sqlparams[5] = new SqlParameter("@UserID", UserID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAll_ByCompanyAndChartOfAccID, filter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_HeadCodes(int companyID, int chartOfAccountID, bool? isActive = true, bool? summaryFlag = false,
            bool? detailPostingAllowedFlag = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ChartOfAccountId", chartOfAccountID);
                sqlparams[2] = new SqlParameter("@IsActive", isActive);
                sqlparams[3] = new SqlParameter("@SummaryFlag", summaryFlag);
                sqlparams[4] = new SqlParameter("@DetailPostingAllowedFlag", detailPostingAllowedFlag);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_HeadCodes, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByKey(string codecombid, int chartofaccountid, int companyid, int isActive,bool? IsPaymentTax = false)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@ChartOfAccountID", chartofaccountid);
                sqlparams[1] = new SqlParameter("@CodeCombinationID", codecombid);
                sqlparams[2] = new SqlParameter("@CompanyID", companyid);
                sqlparams[3] = new SqlParameter("@IsActive", isActive);

                string strFirlds1="";
                string strFirlds2="";

                if (IsPaymentTax == true)
                {
                    strFirlds1 = " , IsTaxAccount ";
                    strFirlds2 = " , [GL_CodeCombinationDetail].IsTaxAccount ";
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Select_By_PrimaryKey,strFirlds1,strFirlds2), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Query_GetDataAllByCompany(int companyid, string coaCodeFrom, string coaCodeTo,
            Nullable<int> accountType, Nullable<int> accountSummary, Nullable<int> accountActive)
        {
            DataTable dt = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);

                if (!string.IsNullOrEmpty(coaCodeFrom) && !string.IsNullOrEmpty(coaCodeTo))
                {
                    additionalFilter = " And GL_CodeCombinationDetail.CodeCombinationID Between @CombinationCodeFrom And @CombinationCodeTo ";
                    sqlparams[1] = new SqlParameter("@CombinationCodeFrom", coaCodeFrom);
                    sqlparams[2] = new SqlParameter("@CombinationCodeTo", coaCodeTo);
                }

                if (!string.IsNullOrEmpty(coaCodeFrom))
                {
                    additionalFilter = " And GL_CodeCombinationDetail.CodeCombinationID >= @CombinationCodeFrom ";
                    sqlparams[1] = new SqlParameter("@CombinationCodeFrom", coaCodeFrom);
                    sqlparams[2] = new SqlParameter("@CombinationCodeTo", DBNull.Value);
                }

                if (!string.IsNullOrEmpty(coaCodeTo))
                {
                    additionalFilter = " And GL_CodeCombinationDetail.CodeCombinationID <= @CombinationCodeTo ";
                    sqlparams[1] = new SqlParameter("@CombinationCodeFrom", DBNull.Value);
                    sqlparams[2] = new SqlParameter("@CombinationCodeTo", coaCodeTo);
                }

                additionalFilter = additionalFilter + " AND GL_CodeCombinationDetail.Status = 1";

                if(accountType.HasValue)
                    sqlparams[3] = new SqlParameter("@AccountType", accountType.Value);
                else
                    sqlparams[3] = new SqlParameter("@AccountType", DBNull.Value);

                if(accountSummary.HasValue)
                    sqlparams[4] = new SqlParameter("@AccountSummary", accountSummary.Value);
                else
                    sqlparams[4] = new SqlParameter("@AccountSummary", DBNull.Value);

                if(accountActive.HasValue)
                    sqlparams[5] = new SqlParameter("@AccountActive", accountActive.Value);
                else
                    sqlparams[5] = new SqlParameter("@AccountActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_GetCOAReport + additionalFilter, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool Update_ChartofAccount(int LengthofFirstLevel)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Trans_Update_ChartofAccount");
            try
            {
                this.UpdateDataByKey(sqltrans);
                if (this.CodeCombinationID.Trim().Length == LengthofFirstLevel)
                {
                    this.UpdateAccountType(sqltrans, LengthofFirstLevel);
                }

                SqlHelper.CommitTransaction(sqltrans);
                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.RollBackTransaction(sqltrans, "Trans_Update_ChartofAccount");
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.RollBackTransaction(sqltrans, "Trans_Update_ChartofAccount");
                SqlHelper.LogException(ex);
                throw ex;
            }
            return result;
        }

        public static bool InsertMultipleRecords(int companyID, int chartOfAccountID, Guid createdBy, string createdByValue, DataTable dtCodeCombinationDetails)
        {
            SqlTransaction objTransaction = SqlHelper.GetTransaction("Insert_CodeCombinationDetails");
            bool isSuccessful = true;

            try
            {
                foreach (DataRow drCodeCombinationDetail in dtCodeCombinationDetails.Rows)
                {
                    GL_CodeCombinationDetail objDetailNew = new GL_CodeCombinationDetail();

                    objDetailNew.AccountTypeID = Convert.ToInt32(drCodeCombinationDetail["AccountTypeID"]);
                    objDetailNew.ChartOfAccountID = chartOfAccountID;
                    objDetailNew.CodeCombinationID = drCodeCombinationDetail["CodeCombinationID"].ToString();
                    objDetailNew.CompanyID = companyID;
                    objDetailNew.CreateDate = DateTime.Now;
                    objDetailNew.CreatedBy = createdBy;
                    objDetailNew.CreatedByValue = createdByValue;
                    objDetailNew.Description = drCodeCombinationDetail["Description"].ToString();
                    objDetailNew.DetailBudgetingAllowedFlag = Convert.ToBoolean(drCodeCombinationDetail["DetailBudgetingAllowedFlag"]);
                    objDetailNew.DetailPostingAllowedFlag = Convert.ToBoolean(drCodeCombinationDetail["DetailPostingAllowedFlag"]);

                    if (drCodeCombinationDetail["HeadAccountCode"] != null && drCodeCombinationDetail["HeadAccountCode"] != DBNull.Value)
                        objDetailNew.HeadAccountCode = drCodeCombinationDetail["HeadAccountCode"].ToString();

                    objDetailNew.IsActive = true;
                    objDetailNew.SerialNo = Convert.ToInt32(drCodeCombinationDetail["SerialNo"]);
                    objDetailNew.Status = true;
                    objDetailNew.SummaryFlag = Convert.ToBoolean(drCodeCombinationDetail["SummaryFlag"]);

                    try
                    {
                        if (!objDetailNew.Insert(objTransaction))
                        {
                            isSuccessful = false;
                            break;
                        }
                    }
                    catch (SqlException exc)
                    {
                        if (exc.Number == 2627)  //  Primary key violation
                        {
                            GL_CodeCombinationDetail objDetailEdit = new GL_CodeCombinationDetail(chartOfAccountID, 
                                drCodeCombinationDetail["CodeCombinationID"].ToString(), companyID);

                            if (objDetailEdit.IsDataLoaded)
                            {
                                objDetailEdit.IsActive = true;
                                objDetailEdit.Status = true;
                                objDetailEdit.UpdateDate = DateTime.Now;
                                objDetailEdit.UpdatedBy = createdBy;
                                objDetailEdit.UpdatedByValue = createdByValue;

                                objDetailEdit.UpdateDataByKey(objTransaction);
                            }
                        }
                        else
                            throw exc;
                    }
                }

                return true;
            }
            catch(Exception exc) { return false; }
            finally
            {
                if (isSuccessful)
                    objTransaction.Commit();
                else
                    objTransaction.Rollback("Insert_CodeCombinationDetails");
            }
        }

        public static DataTable GetCOADesctiptionList(string description)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@Description", description);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDescriptionList,sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataForAssignment(int AccountTypeID)
        {
            DataTable dt = null;

            try
            {


                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@AccountTypeID", AccountTypeID);


                const string Query_SelectAllDataForAssignment = @"
SELECT *, CONVERT(BIT,'False')AS Checked FROM [GL_CodeCombinationDetail] 
inner join SET_AccountType on SET_AccountType.AccountTypeID = [GL_CodeCombinationDetail].AccountTypeID
WHERE [GL_CodeCombinationDetail] .[Status] = 1 AND [IsActive] = 1 
and [GL_CodeCombinationDetail].DetailPostingAllowedFlag = 1
and [GL_CodeCombinationDetail].AccountTypeID = @AccountTypeID
ORDER BY GL_CodeCombinationDetail.CodeCombinationID
";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDataForAssignment, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static bool DeleteFormTableDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int chartOfAccountID, string codeCombinationID, int companyID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@ChartOfAccountID", chartOfAccountID);
                sqlparams[1] = new SqlParameter("@CodeCombinationID", codeCombinationID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                const string Query_DeleteFormTableDataByKey = @" Delete from [GL_CodeCombinationDetail] WHERE [ChartOfAccountID] = @ChartOfAccountID and [CodeCombinationID] = @CodeCombinationID and [CompanyID] = @CompanyID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteFormTableDataByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteFormTableDataByKey, sqlparams) > 0)
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

        public static DataTable GetAllDataForImport()
        {
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Import).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetSalariesPayableType(int companyID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);


                const string Query_GetSalariesPayableType = @"select * from GL_CodeCombinationDetail where Description like '%salaries%payable%'";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetSalariesPayableType, sqlparams).Tables[0];
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
