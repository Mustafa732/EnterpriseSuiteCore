using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.GL.Transaction
{
    public partial class BudgetAppropriationDetail
    {
       

//        const string Query_GetDataByKey = @"Select BudgetAppropriationDetail.BudgetAppropriationDetailID,
//		   BudgetAppropriationDetail.BudgetAppropriationHeaderID,
//		   BudgetAppropriationDetail.BudgetAppropriationDetailID,
//		   BudgetAppropriationDetail.Period,
//		   BudgetAppropriationDetail.Allocation,
//		   BudgetAppropriationDetail.Appropriation,
//		   BudgetAppropriationDetail.Available,
//		   BudgetAppropriationDetail.Amount,
//		   BudgetAppropriationDetail.Requirments,
//            '' AS [Action]
//	from BudgetAppropriationDetail
//	Where CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1
//    AND BudgetAppropriationHeaderID = @BudgetAppropriationHeaderID";

        const string Query_GetDataByKey = @"
   Select Distinct APD.BudgetAppropriationDetailID,
    APD.BudgetAppropriationHeaderID,
    APD.BudgetAppropriationDetailID,
    APD.Period,
    APD.Allocation,
    --APD.Appropriation + APD.Amount As Appropriation,
    --APD.Appropriation As Appropriation,
    (case when APD.Appropriation=0 then APD.Amount else APD.Appropriation end) As Appropriation,
     (
            BudgetAllocationDetail.BudgetAmount - 
            (
                Select
                sum(BudgetAppropriationDetail.Amount)
                From BudgetAppropriationDetail
                Inner Join BudgetAllocationHeader ON (BudgetAllocationHeader.BudgetAllocationHeaderID = BudgetAllocationDetail.BudgetAllocationHeaderID)
                And BudgetAllocationHeader.CompanyID = BudgetAllocationDetail.CompanyID
                And BudgetAllocationHeader.PeriodID = BudgetAllocationDetail.PeriodID
                AND BudgetAllocationDetail.CompanyID = @CompanyID
                AND BudgetAllocationDetail.PeriodID = @PeriodID
                And BudgetAllocationDetail.Period = BudgetAppropriationDetail.Period
                and BudgetAppropriationDetail.Period = BudgetAllocationDetail.Period
                and BudgetAppropriationDetail.BudgetAppropriationHeaderID = @BudgetAppropriationHeaderID
                Where
                BudgetAppropriationDetail.Status = 1
        )
       -- -
        --(APD.Appropriation + APD.Amount)
		+
		(
				Select isnull(sum(ReAppAmount),0) from BudgetReAppropriation
				Where
				BudgetReAppropriation.Status = 1
                and BudgetReAppropriation.CompanyID=@CompanyID
				and BudgetReAppropriation.PeriodID=@PeriodID
				and BudgetReAppropriation.DepartmentID = BudgetAppropriationHeader.DepartmentID
                --and BudgetReAppropriation.accountcode = BudgetAppropriationHeader.accountcode
                And BudgetReAppropriation.Allocation = APD.Allocation
		)
) As Available,
    APD.Amount,
    APD.Requirments,
    LookupDetail.EnumeratedDescription as PeriodDetail,
    IsNull(( 
		Select Top 1 ReAppAmount 
		From BudgetReAppropriation
			Inner Join BudgetAppropriationHeader On
			BudgetReAppropriation.AccountCode = BudgetAppropriationHeader.AccountCode
			And BudgetReAppropriation.CostCenter = BudgetAppropriationHeader.CostCenter
			And BudgetReAppropriation.CompanyID = BudgetAppropriationHeader.CompanyID
			And BudgetReAppropriation.PeriodID = BudgetAppropriationHeader.PeriodID
			AND BudgetAppropriationHeader.Status = 1
			Inner Join BudgetAppropriationDetail ON
			BudgetAppropriationDetail.BudgetAppropriationHeaderID = BudgetAppropriationHeader.BudgetAppropriationHeaderID
			AND BudgetAppropriationDetail.CompanyID = BudgetAppropriationHeader.CompanyID
			And BudgetAppropriationDetail.PeriodID = BudgetAppropriationHeader.PeriodID
			AND BudgetAppropriationDetail.Status = 1
			AND BudgetAppropriationDetail.Period = BudgetReAppropriation.Period
			Where 
            BudgetReAppropriation.Period = APD.Period
			And BudgetReAppropriation.Status = 1
			and BudgetReAppropriation.CompanyID=@CompanyID
			and BudgetReAppropriation.PeriodID=@PeriodID
			and BudgetReAppropriation.DepartmentID = BudgetAppropriationHeader.DepartmentID
			and BudgetReAppropriation.CostCenter = BudgetAppropriationHeader.CostCenter
			And BudgetReAppropriation.Allocation = APD.Allocation
),0) As ReAppropriation,
    '' AS [Action]
    from BudgetAppropriationDetail As APD
		
		Inner Join BudgetAppropriationHeader on 
		BudgetAppropriationHeader.BudgetAppropriationHeaderID = APD.BudgetAppropriationHeaderID
		AND BudgetAppropriationHeader.CompanyID = APD.CompanyID
		AND BudgetAppropriationHeader.PeriodID = APD.PeriodID
		AND BudgetAppropriationHeader.Status = 1
		Inner Join BudgetAllocationHeader ON 
		BudgetAllocationHeader.AccountCode = BudgetAppropriationHeader.AccountCode
		AND BudgetAllocationHeader.CostCenter = BudgetAppropriationHeader.CostCenter
		AND BudgetAllocationHeader.CompanyID = BudgetAppropriationHeader.CompanyID
		AND BudgetAllocationHeader.PeriodID = BudgetAppropriationHeader.PeriodID
		AND BudgetAllocationHeader.Status = 1
		Inner Join BudgetAllocationDetail on
		BudgetAllocationDetail.BudgetAllocationHeaderID = BudgetAllocationHeader.BudgetAllocationHeaderID
		and BudgetAllocationDetail.Period = APD.Period
		left Join BudgetReAppropriation ON
		BudgetReAppropriation.CostCenter = BudgetAppropriationHeader.CostCenter
		AND BudgetReAppropriation.AccountCode = BudgetAppropriationHeader.AccountCode
		AND BudgetReAppropriation.CompanyID = BudgetAppropriationHeader.CompanyID
		AND BudgetReAppropriation.PeriodID = BudgetAppropriationHeader.PeriodID
		AND BudgetReAppropriation.Status = 1
        Inner Join LookupDetail on (LookupDetail.LookupDetailID = APD.Period)
        Inner Join LookupHeader On (LookupHeader.LookupID = LookupDetail.LookupHeaderID)
        AND LookupHeader.LookupID = (
        case when LookupDetail.LookupDetailID <= 12 then 16 else 
        case when LookupDetail.LookupDetailID > 12 and LookupDetail.LookupDetailID  <= 16 THEN 18 else 
        case when LookupDetail.LookupDetailID = 17 then 17 else 0 
        end
        end
        end)
        Where (APD.CompanyID = @CompanyID
        and APD.PeriodID = @PeriodID
        and APD.Status = 1 
        AND APD.BudgetAppropriationHeaderID = @BudgetAppropriationHeaderID)
    ";


        const string Query_GetAllocationApp = @"



Select BudgetAppropriationDetail.Allocation,
(
    BudgetAppropriationDetail.Allocation - 
    (
        Select
        sum(BudgetAppropriationDetail.Amount)
        From BudgetAppropriationDetail
        Inner Join BudgetAppropriationHeader ON 
        BudgetAppropriationHeader.BudgetAppropriationHeaderID = BudgetAppropriationDetail.BudgetAppropriationHeaderID
        And BudgetAppropriationHeader.CompanyID = BudgetAppropriationDetail.CompanyID
        And BudgetAppropriationHeader.PeriodID = BudgetAppropriationDetail.PeriodID
        AND BudgetAppropriationHeader.CompanyID = @CompanyID
        AND BudgetAppropriationHeader.PeriodID = @PeriodID
        And BudgetAppropriationDetail.Period = @Period
        Where
        BudgetAppropriationDetail.Status = 1
        AND BudgetAppropriationHeader.AccountCode = @AC
        And BudgetAppropriationHeader.CostCenter = @CC
    )
	+
	(
		Select isnull(sum(ReAppAmount),0) from BudgetReAppropriation
		Where
		BudgetReAppropriation.Status = 1
		AND BudgetReAppropriation.AccountCode = @AC
		And BudgetReAppropriation.CostCenter = @CC
		And BudgetReAppropriation.Period = @Period
	)
) As Available,
BudgetAppropriationDetail.Amount
From BudgetAppropriationDetail
Inner Join BudgetAppropriationHeader On 
(BudgetAppropriationHeader.BudgetAppropriationHeaderID = BudgetAppropriationDetail.BudgetAppropriationHeaderID)
And BudgetAppropriationHeader.CompanyID = BudgetAppropriationDetail.CompanyID
And BudgetAppropriationHeader.PeriodID = BudgetAppropriationDetail.PeriodID
Inner join BudgetAllocationHeader ON 
BudgetAllocationHeader.CostCenter = BudgetAppropriationHeader.CostCenter
AND BudgetAllocationHeader.AccountCode = BudgetAppropriationHeader.AccountCode
AND BudgetAllocationHeader.CompanyID = BudgetAppropriationHeader.CompanyID
AND BudgetAllocationHeader.PeriodID = BudgetAppropriationHeader.PeriodID
AND BudgetAllocationHeader.Status = 1
Inner join BudgetAllocationDetail ON
BudgetAllocationDetail.BudgetAllocationHeaderID = BudgetAllocationHeader.BudgetAllocationHeaderID
ANd BudgetAllocationDetail.CompanyID = BudgetAllocationHeader.CompanyID
AND BudgetAllocationDetail.PeriodID = BudgetAllocationHeader.PeriodID
AND BudgetAllocationDetail.Status = 1
Where BudgetAppropriationDetail.CompanyID = @CompanyID
And BudgetAppropriationDetail.PeriodID = @PeriodID
And BudgetAppropriationDetail.Period = @Period
And BudgetAppropriationHeader.AccountCode = @AC
AND BudgetAppropriationHeader.CostCenter = @CC



";

        const string query_updateAppropriation = @"Update BudgetAppropriationDetail
Set BudgetAppropriationDetail.Appropriation = BudgetAppropriationDetail.Amount
From BudgetAppropriationDetail
Inner Join BudgetAppropriationHeader ON (BudgetAppropriationHeader.BudgetAppropriationHeaderID = BudgetAppropriationDetail.BudgetAppropriationHeaderID)
AND BudgetAppropriationHeader.companyID = BudgetAppropriationDetail.CompanyID
AND BudgetAppropriationHeader.PeriodID = BudgetAppropriationDetail.PeriodID
Inner Join BudgetAllocationHeader on (BudgetAllocationHeader.AccountCode = BudgetAppropriationHeader.AccountCode)
AND BudgetAllocationHeader.CostCenter = BudgetAppropriationHeader.CostCenter
AND BudgetAppropriationHeader.AccountCode = @AC
AND BudgetAppropriationHeader.CostCenter = @CC
Where
BudgetAppropriationDetail.CompanyID = @CompanyID AND BudgetAppropriationDetail.PeriodID = @PeriodID
AND BudgetAppropriationDetail.Status = 1";

        const string query_updateAppropriationReApp = @"
Update BudgetAppropriationDetail
Set BudgetAppropriationDetail.Appropriation = BudgetAppropriationDetail.Appropriation
From BudgetAppropriationDetail
Inner Join BudgetAppropriationHeader ON (BudgetAppropriationHeader.BudgetAppropriationHeaderID = BudgetAppropriationDetail.BudgetAppropriationHeaderID)
AND BudgetAppropriationHeader.companyID = BudgetAppropriationDetail.CompanyID
AND BudgetAppropriationHeader.PeriodID = BudgetAppropriationDetail.PeriodID
Inner Join BudgetAllocationHeader on (BudgetAllocationHeader.AccountCode = BudgetAppropriationHeader.AccountCode)
AND BudgetAllocationHeader.CostCenter = BudgetAppropriationHeader.CostCenter
AND BudgetAppropriationHeader.AccountCode = @AC
AND BudgetAppropriationHeader.CostCenter = @CC
Where
BudgetAppropriationDetail.CompanyID = @CompanyID AND BudgetAppropriationDetail.PeriodID = @PeriodID
AND BudgetAppropriationDetail.Status = 1";

        const string Query_UpdateAvailable = @"Update BudgetAppropriationDetail
Set BudgetAppropriationDetail.Available = BudgetAppropriationDetail.Allocation - BudgetAppropriationDetail.Appropriation
From BudgetAppropriationDetail
Inner Join BudgetAppropriationHeader ON (BudgetAppropriationHeader.BudgetAppropriationHeaderID = BudgetAppropriationDetail.BudgetAppropriationHeaderID)
AND BudgetAppropriationHeader.companyID = BudgetAppropriationDetail.CompanyID
AND BudgetAppropriationHeader.PeriodID = BudgetAppropriationDetail.PeriodID
Inner Join BudgetAllocationHeader on (BudgetAllocationHeader.AccountCode = BudgetAppropriationHeader.AccountCode)
AND BudgetAllocationHeader.CostCenter = BudgetAppropriationHeader.CostCenter
AND BudgetAppropriationHeader.AccountCode = @AC
AND BudgetAppropriationHeader.CostCenter = @CC
Where
BudgetAppropriationDetail.CompanyID = @CompanyID AND BudgetAppropriationDetail.PeriodID = @PeriodID
AND BudgetAppropriationDetail.Status = 1";

        const string Query_UpdateAvailableReApp = @"Update BudgetAppropriationDetail
Set BudgetAppropriationDetail.Available = BudgetAppropriationDetail.Available + @Reapp
From BudgetAppropriationDetail
Inner Join BudgetAppropriationHeader ON (BudgetAppropriationHeader.BudgetAppropriationHeaderID = BudgetAppropriationDetail.BudgetAppropriationHeaderID)
AND BudgetAppropriationHeader.companyID = BudgetAppropriationDetail.CompanyID
AND BudgetAppropriationHeader.PeriodID = BudgetAppropriationDetail.PeriodID
Inner Join BudgetAllocationHeader on (BudgetAllocationHeader.AccountCode = BudgetAppropriationHeader.AccountCode)
AND BudgetAllocationHeader.CostCenter = BudgetAppropriationHeader.CostCenter
AND BudgetAppropriationHeader.AccountCode = @AC
AND BudgetAppropriationHeader.CostCenter = @CC
Where
BudgetAppropriationDetail.CompanyID = @CompanyID AND BudgetAppropriationDetail.PeriodID = @PeriodID
AND BudgetAppropriationDetail.Status = 1";

        const string Query_UpdateAmmount = @"Update BudgetAppropriationDetail
Set BudgetAppropriationDetail.Amount = BudgetAppropriationDetail.Amount + @Amount
From BudgetAppropriationDetail
Inner Join BudgetAppropriationHeader ON (BudgetAppropriationHeader.BudgetAppropriationHeaderID = BudgetAppropriationDetail.BudgetAppropriationHeaderID)
AND BudgetAppropriationHeader.companyID = BudgetAppropriationDetail.CompanyID
AND BudgetAppropriationHeader.PeriodID = BudgetAppropriationDetail.PeriodID
Inner Join BudgetAllocationHeader on (BudgetAllocationHeader.AccountCode = BudgetAppropriationHeader.AccountCode)
AND BudgetAllocationHeader.CostCenter = BudgetAppropriationHeader.CostCenter
AND BudgetAppropriationHeader.AccountCode = @AC
AND BudgetAppropriationHeader.CostCenter = @CC
Where
BudgetAppropriationDetail.CompanyID = @CompanyID AND BudgetAppropriationDetail.PeriodID = @PeriodID
AND BudgetAppropriationDetail.Status = 1";

        public static bool Get_updateAmmount(int companyID, int periodID, int amount, string ac, int cc)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];


                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Amount", amount);
                sqlparams[3] = new SqlParameter("@AC", ac);
                sqlparams[4] = new SqlParameter("@CC", cc);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateAmmount, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool Get_updateAppropriation(int companyID, int periodID, string ac, int cc)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

             
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AC", ac);
                sqlparams[3] = new SqlParameter("@CC", cc);
              
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, query_updateAppropriation, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool Get_updateAppropriationForReapp(int companyID, int periodID, string ac, int cc , int Reapp)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];


                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AC", ac);
                sqlparams[3] = new SqlParameter("@CC", cc);
                sqlparams[4] = new SqlParameter("@Reapp", Reapp);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, query_updateAppropriationReApp, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool Get_UpdateAvailable(int companyID, int periodID, string ac, int cc)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];


                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AC", ac);
                sqlparams[3] = new SqlParameter("@CC", cc);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateAvailable, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool Get_UpdateAvailableReApp(int companyID, int periodID, string ac, int cc,int reapp)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];


                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AC", ac);
                sqlparams[3] = new SqlParameter("@CC", cc);
                sqlparams[4] = new SqlParameter("@Reapp", reapp);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateAvailableReApp, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataRow GetAllocationAppropriation(int companyid, int periodid, int period, string AC, int CC)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@Period", period);
                sqlparams[3] = new SqlParameter("@AC", AC);
                sqlparams[4] = new SqlParameter("@CC", CC);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllocationApp, sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTables(int budgetAppropriationheaderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@BudgetAppropriationHeaderID", budgetAppropriationheaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKey, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }
    }
}
