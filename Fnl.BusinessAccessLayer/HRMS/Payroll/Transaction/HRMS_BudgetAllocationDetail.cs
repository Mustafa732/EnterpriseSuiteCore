using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.AR.Setup;
using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Setup;
using System.Globalization;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{

    public partial class HRMS_BudgetAllocationDetail
    {


        const string Query_BudgetAllocationReport = @"
	Select
	'Allocation' As Type,
	HRMS_BudgetAllocationHeader.AllocationDate ,
	null As AppropriationDate,
	null As ReAppropriationDate,
	LookupDetail.EnumeratedDescription As Period,
	GL_CodeCombinationDetail.Description As EXPENSES,
	Set_CostCenter.CostCenterName As Department,
	BudgetAmount As Allocation,
	'' as Appropriation ,
	'' as Available ,
	'' as Amount ,
	'' as ReAppAmount
	
	From HRMS_BudgetAllocationHeader
	Inner Join HRMS_BudgetAllocationDetail ON (HRMS_BudgetAllocationDetail.BudgetAllocationHeaderID = HRMS_BudgetAllocationHeader.BudgetAllocationHeaderID)
			AND HRMS_BudgetAllocationDetail.CompanyID = HRMS_BudgetAllocationHeader.CompanyID
			AND HRMS_BudgetAllocationDetail.PeriodID = HRMS_BudgetAllocationHeader.PeriodID 
			AND HRMS_BudgetAllocationDetail.Status = 1 
			And HRMS_BudgetAllocationHeader.Status = 1
			Inner Join LookupDetail on (LookupDetail.LookupDetailID = HRMS_BudgetAllocationDetail.Period)
			Inner Join LookupHeader On (LookupHeader.LookupID = LookupDetail.LookupHeaderID)
					AND LookupHeader.LookupID = (
						case when LookupDetail.LookupDetailID <= 12 then 14 else 
							case when LookupDetail.LookupDetailID > 12 and LookupDetail.LookupDetailID  <= 16 THEN 15 else 
								case when LookupDetail.LookupDetailID = 17 then 16 else 0 
								end
							end
						end)
							Inner Join GL_CodeCombinationDetail On (GL_CodeCombinationDetail.CompanyID = HRMS_BudgetAllocationHeader.CompanyID) AND 
            Gl_CodeCombinationDetail.Status = 1
			AND Gl_CodeCombinationDetail.IsActive = 1
			Inner Join Set_CostCenter On(Set_CostCenter.CostCenterID = HRMS_BudgetAllocationHeader.CostCenter)
			And Set_CostCenter.Status = 1
Where HRMS_BudgetAllocationHeader.CompanyID = @CompanyID and HRMS_BudgetAllocationHeader.PeriodID = @PeriodID
And (HRMS_BudgetAllocationHeader.CostCenter = @cc Or @cc is null)
And (LookupDetail.LookupHeaderID = @frequency or @frequency is null)
 And GL_CodeCombinationDetail.Description like '%' + @Desc + '%'

Union All

Select 
	'Appropriation' As Type,
	null As AllocationDate ,
	isnull(convert(varchar(100) ,HRMS_BudgetAppropriationHeader.AppropriationDate, 105), '') As AppropriationDate,
	null As ReAppropriationDate,
	LookupDetail.EnumeratedDescription As Period,
	GL_CodeCombinationDetail.Description As EXPENSES,
	Set_CostCenter.CostCenterName As Department,
	isnull(HRMS_BudgetAppropriationDetail.Allocation,0) As Allocation,
	isnull(HRMS_BudgetAppropriationDetail.Appropriation,0) as Appropriation ,
	isnull(HRMS_BudgetAppropriationDetail.Available,0) as Available ,
	isnull(HRMS_BudgetAppropriationDetail.Amount,0) as Amount ,
	'' As ReAppAmount
	
	From HRMS_BudgetAppropriationHeader
	Inner Join HRMS_BudgetAppropriationDetail On (HRMS_BudgetAppropriationDetail.BudgetAppropriationHeaderID = HRMS_BudgetAppropriationHeader.BudgetAppropriationHeaderID)
			AND HRMS_BudgetAppropriationHeader.CompanyID = HRMS_BudgetAppropriationDetail.CompanyID
			AND HRMS_BudgetAppropriationHeader.PeriodID = HRMS_BudgetAppropriationDetail.PeriodID 
			AND HRMS_BudgetAppropriationDetail.Status = 1 
			And HRMS_BudgetAppropriationDetail.Status = 1
			Inner Join LookupDetail on (LookupDetail.LookupDetailID = HRMS_BudgetAppropriationDetail.Period)
			Inner Join LookupHeader On (LookupHeader.LookupID = LookupDetail.LookupHeaderID)
					AND LookupHeader.LookupID = (
						case when LookupDetail.LookupDetailID <= 12 then 14 else 
							case when LookupDetail.LookupDetailID > 12 and LookupDetail.LookupDetailID  <= 16 THEN 15 else 
								case when LookupDetail.LookupDetailID = 17 then 16 else 0 
								end
							end
						end)
							Inner Join GL_CodeCombinationDetail On (GL_CodeCombinationDetail.CompanyID = HRMS_BudgetAppropriationHeader.CompanyID) AND 
            Gl_CodeCombinationDetail.Status = 1
			AND Gl_CodeCombinationDetail.IsActive = 1
			Inner Join Set_CostCenter On(Set_CostCenter.CostCenterID = HRMS_BudgetAppropriationHeader.CostCenter)
			And Set_CostCenter.Status = 1
Where HRMS_BudgetAppropriationHeader.CompanyID = @CompanyID and HRMS_BudgetAppropriationHeader.PeriodID = @PeriodID
And (HRMS_BudgetAppropriationHeader.CostCenter = @cc Or @cc is null)
And (LookupDetail.LookupHeaderID = @frequency or @frequency is null)
 And GL_CodeCombinationDetail.Description like '%' + @Desc + '%'

Union All

Select 
    'Re-Appro' As Type,
	null As AllocationDate ,
	null As AppropriationDate,
	HRMS_BudgetReAppropriation.AppropriationDate As ReAppropriationDate,
	LookupDetail.EnumeratedDescription As Period,
	GL_CodeCombinationDetail.Description As EXPENSES,
	Set_CostCenter.CostCenterName As Department,
	Allocation As Allocation,
	'' As Appropriation ,
	Available As Available ,
	'' As Amount ,
	isnull(HRMS_BudgetReAppropriation.ReAppAmount,0) as ReAppAmount
	
	From HRMS_BudgetReAppropriation
	
	
		Inner Join LookupDetail on (LookupDetail.LookupDetailID = HRMS_BudgetReAppropriation.Period)
			
			Inner Join LookupHeader On (LookupHeader.LookupID = LookupDetail.LookupHeaderID)
					AND LookupHeader.LookupID = (
						case when LookupDetail.LookupDetailID <= 12 then 14 else 
							case when LookupDetail.LookupDetailID > 12 and LookupDetail.LookupDetailID  <= 16 THEN 15 else 
								case when LookupDetail.LookupDetailID = 17 then 16 else 0 
								end
							end
						end)
						
			Inner Join GL_CodeCombinationDetail On (GL_CodeCombinationDetail.CompanyID = HRMS_BudgetReAppropriation.CompanyID) AND 
            Gl_CodeCombinationDetail.Status = 1
			AND Gl_CodeCombinationDetail.IsActive = 1
			Inner Join Set_CostCenter On(Set_CostCenter.CostCenterID = HRMS_BudgetReAppropriation.CostCenter)
			And Set_CostCenter.Status = 1

			Where HRMS_BudgetReAppropriation.CompanyID = @CompanyID and HRMS_BudgetReAppropriation.PeriodID = @PeriodID
            And (HRMS_BudgetReAppropriation.CostCenter = @cc Or @cc is null)
            And (LookupDetail.LookupHeaderID = @frequency or @frequency is null)
            And GL_CodeCombinationDetail.Description like '%' + @Desc + '%'
			

";



        const string Query_GetDataByKey = @" 
Select HRMS_BudgetAllocationDetail.BudgetAllocationDetailID,
		  HRMS_BudgetAllocationDetail.BudgetAllocationHeaderID,
		   HRMS_BudgetAllocationDetail.BudgetAmount,
		   (Case when HRMS_BudgetAllocationDetail.Period = 1 then 'January'
				when HRMS_BudgetAllocationDetail.Period = 2 then 'Febuary'
				when HRMS_BudgetAllocationDetail.Period = 3 then 'March'
				when HRMS_BudgetAllocationDetail.Period = 4 then 'April'
				when HRMS_BudgetAllocationDetail.Period = 5 then 'May'
				when HRMS_BudgetAllocationDetail.Period = 6 then 'June'
				when HRMS_BudgetAllocationDetail.Period = 7 then 'July'
				when HRMS_BudgetAllocationDetail.Period = 8 then 'August'
				when HRMS_BudgetAllocationDetail.Period = 9 then 'September'
				when HRMS_BudgetAllocationDetail.Period = 10 then 'October'
				when HRMS_BudgetAllocationDetail.Period = 11 then 'November'
				when HRMS_BudgetAllocationDetail.Period = 12 then 'December'
				when HRMS_BudgetAllocationDetail.Period = 13 then 'FirstQuater'
				when HRMS_BudgetAllocationDetail.Period = 14 then 'SecondQuater'
				when HRMS_BudgetAllocationDetail.Period = 15 then 'ThirdQuater'
				when HRMS_BudgetAllocationDetail.Period = 16 then 'ForthQuater'
				when HRMS_BudgetAllocationDetail.Period = 17 then 'Yearly'
			  END)
	 AS Period,
            '' AS [Action]
	from HRMS_BudgetAllocationDetail
	Where CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1
    AND BudgetAllocationHeaderID = @BudgetAllocationHeaderID";



        const string Query_GetPeriod = @"

Select HRMS_BudgetAllocationDetail.Period,LookupDetail.EnumeratedDescription,HRMS_BudgetAllocationDetail.BudgetAmount
From HRMS_BudgetAllocationDetail
Inner Join HRMS_BudgetAllocationHeader ON (HRMS_BudgetAllocationHeader.BudgetAllocationHeaderID = HRMS_BudgetAllocationDetail.BudgetAllocationHeaderID)
AND HRMS_BudgetAllocationHeader.CompanyID = HRMS_BudgetAllocationDetail.CompanyID
AND HRMS_BudgetAllocationHeader.PeriodID = HRMS_BudgetAllocationDetail.PeriodID
AND HRMS_BudgetAllocationHeader.CompanyID = @CompanyID
AND HRMS_BudgetAllocationHeader.PeriodID = @PeriodID
AND HRMS_BudgetAllocationHeader.Status = 1
AND HRMS_BudgetAllocationDetail.Status = 1
And HRMS_BudgetAllocationHeader.CostCenter = @CC
Inner Join LookupDetail on (LookupDetail.LookupDetailID = HRMS_BudgetAllocationDetail.Period)
Inner Join LookupHeader On (LookupHeader.LookupID = LookupDetail.LookupHeaderID)
AND LookupHeader.LookupID = (
case when LookupDetail.LookupDetailID <= 12 then 14 else 
 case when LookupDetail.LookupDetailID > 12 and LookupDetail.LookupDetailID  <= 16 THEN 15 else 
   case when LookupDetail.LookupDetailID = 17 then 16 else 0 
  end
 end
end)
";

        const string a = @"Select HRMS_BudgetAllocationDetail.BudgetAmount,HRMS_BudgetAllocationDetail.Appropriation
from HRMS_BudgetAllocationDetail
Inner Join HRMS_BudgetAllocationHeader ON (HRMS_BudgetAllocationHeader.BudgetAllocationHeaderID = HRMS_BudgetAllocationDetail.BudgetAllocationHeaderID)
And HRMS_BudgetAllocationHeader.CompanyID = HRMS_BudgetAllocationDetail.CompanyID
And HRMS_BudgetAllocationHeader.PeriodID = HRMS_BudgetAllocationDetail.PeriodID
AND HRMS_BudgetAllocationDetail.CompanyID = @CompanyID
AND HRMS_BudgetAllocationDetail.PeriodID = @PeriodID
And HRMS_BudgetAllocationDetail.Period = @Period";

        const string Query_GetAllocationAmount = @"
Select 
HRMS_BudgetAllocationDetail.BudgetAmount,
HRMS_BudgetAppropriationDetail.Appropriation,
(
Select 
SUM(HRMS_BudgetAppropriationDetail.Amount)
From HRMS_BudgetAppropriationDetail
Inner Join HRMS_BudgetAllocationHeader ON (HRMS_BudgetAllocationHeader.BudgetAllocationHeaderID = HRMS_BudgetAllocationDetail.BudgetAllocationHeaderID)
				And HRMS_BudgetAllocationHeader.CompanyID = HRMS_BudgetAllocationDetail.CompanyID
				And HRMS_BudgetAllocationHeader.PeriodID = HRMS_BudgetAllocationDetail.PeriodID
				AND HRMS_BudgetAllocationDetail.CompanyID = @CompanyID
				AND HRMS_BudgetAllocationDetail.PeriodID = @PeriodID
Where
	HRMS_BudgetAppropriationDetail.Status = 1
				And HRMS_BudgetAllocationHeader.CostCenter = @CC
				AND HRMS_BudgetAppropriationDetail.Period = @Period

) AS Amount,
            (
            HRMS_BudgetAllocationDetail.BudgetAmount - 
            (
                Select
                sum(HRMS_BudgetAppropriationDetail.Amount)
                From HRMS_BudgetAppropriationDetail
                Inner Join HRMS_BudgetAllocationHeader ON (HRMS_BudgetAllocationHeader.BudgetAllocationHeaderID = HRMS_BudgetAllocationDetail.BudgetAllocationHeaderID)
                And HRMS_BudgetAllocationHeader.CompanyID = HRMS_BudgetAllocationDetail.CompanyID
                And HRMS_BudgetAllocationHeader.PeriodID = HRMS_BudgetAllocationDetail.PeriodID
                AND HRMS_BudgetAllocationDetail.CompanyID = @CompanyID
                AND HRMS_BudgetAllocationDetail.PeriodID = @PeriodID
                And HRMS_BudgetAllocationDetail.Period = @Period
                and HRMS_BudgetAppropriationDetail.Period = HRMS_BudgetAllocationDetail.Period
                Where
                HRMS_BudgetAppropriationDetail.Status = 1
                And HRMS_BudgetAllocationHeader.CostCenter = @CC
            )

+
		(
				Select isnull(sum(ReAppAmount),0) from HRMS_BudgetReAppropriation
				Where
				HRMS_BudgetReAppropriation.Status = 1
 				And HRMS_BudgetReAppropriation.CostCenter = @CC
				And HRMS_BudgetReAppropriation.Period = @Period
		)
		
        
) As Available,
IsNull(( 
		Select Top 1 ReAppAmount 
		From HRMS_BudgetReAppropriation
			Inner Join HRMS_BudgetAppropriationHeader On
			HRMS_BudgetReAppropriation.CostCenter = HRMS_BudgetAppropriationHeader.CostCenter
			And HRMS_BudgetReAppropriation.CompanyID = HRMS_BudgetAppropriationHeader.CompanyID
			And HRMS_BudgetReAppropriation.PeriodID = HRMS_BudgetAppropriationHeader.PeriodID
			AND HRMS_BudgetAppropriationHeader.Status = 1
			Inner Join HRMS_BudgetAppropriationDetail ON
			HRMS_BudgetAppropriationDetail.BudgetAppropriationHeaderID = HRMS_BudgetAppropriationHeader.BudgetAppropriationHeaderID
			AND HRMS_BudgetAppropriationDetail.CompanyID = HRMS_BudgetAppropriationHeader.CompanyID
			And HRMS_BudgetAppropriationDetail.PeriodID = HRMS_BudgetAppropriationHeader.PeriodID
			AND HRMS_BudgetAppropriationDetail.Status = 1
			AND HRMS_BudgetAppropriationDetail.Period =HRMS_ BudgetReAppropriation.Period
			Where 
			HRMS_BudgetReAppropriation.Period = @Period
			ANd HRMS_BudgetReAppropriation.CompanyID = @CompanyID
			ANd HRMS_BudgetReAppropriation.PeriodID = @PeriodID
			And HRMS_BudgetReAppropriation.Status = 1
		Order By HRMS_BudgetReAppropriation.CreateDate Desc
),0) As ReAppropriation
from HRMS_BudgetAllocationDetail
Inner Join HRMS_BudgetAllocationHeader ON (HRMS_BudgetAllocationHeader.BudgetAllocationHeaderID = HRMS_BudgetAllocationDetail.BudgetAllocationHeaderID)
And HRMS_BudgetAllocationHeader.CompanyID = HRMS_BudgetAllocationDetail.CompanyID
And HRMS_BudgetAllocationHeader.PeriodID = HRMS_BudgetAllocationDetail.PeriodID
AND HRMS_BudgetAllocationDetail.CompanyID = @CompanyID
AND HRMS_BudgetAllocationDetail.PeriodID = @PeriodID
And HRMS_BudgetAllocationDetail.Period = @Period
Inner Join HRMS_BudgetAppropriationHeader on 
HRMS_BudgetAppropriationHeader.CostCenter = HRMS_BudgetAllocationHeader.CostCenter
AND HRMS_BudgetAppropriationHeader.CompanyID = HRMS_BudgetAllocationHeader.CompanyID
AND HRMS_BudgetAppropriationHeader.Status = 1
Inner Join HRMS_BudgetAppropriationDetail on (HRMS_BudgetAppropriationDetail.BudgetAppropriationHeaderID = HRMS_BudgetAppropriationHeader.BudgetAppropriationHeaderID)
AND HRMS_BudgetAppropriationDetail.CompanyID = HRMS_BudgetAppropriationHeader.CompanyID
ANd HRMS_BudgetAppropriationDetail.Period = HRMS_BudgetAllocationDetail.Period
AND HRMS_BudgetAppropriationDetail.Status = 1
And HRMS_BudgetAllocationHeader.CostCenter = @CC
and HRMS_BudgetAllocationDetail.Period = @Period";

        const string Query_GetAllocationOnFirst = @"Select HRMS_BudgetAllocationDetail.BudgetAmount
from HRMS_BudgetAllocationDetail
Inner Join HRMS_BudgetAllocationHeader ON (HRMS_BudgetAllocationHeader.BudgetAllocationHeaderID = HRMS_BudgetAllocationDetail.BudgetAllocationHeaderID)
And HRMS_BudgetAllocationHeader.CompanyID = HRMS_BudgetAllocationDetail.CompanyID
And HRMS_BudgetAllocationHeader.PeriodID = HRMS_BudgetAllocationDetail.PeriodID
AND HRMS_BudgetAllocationDetail.CompanyID = @CompanyID
AND HRMS_BudgetAllocationDetail.PeriodID = @PeriodID
AND HRMS_BudgetAllocationDetail.Period = @Period
And HRMS_BudgetAllocationHeader.CostCenter = @CC";

        public static DataRow GetAllocationOnFirst(int companyid, int periodid, int period ,  int cc)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@Period", period);
                sqlparams[3] = new SqlParameter("@CC", cc);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllocationOnFirst, sqlparams);

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

        public static DataRow GetAllcationAmmount(int companyid, int periodid, int period,  int cc)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@Period", period);
                sqlparams[3] = new SqlParameter("@CC", cc);
               
                  
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllocationAmount, sqlparams);

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


        public static DataTable BudgetAllocationReport(int companyid, int periodid, int frequency, int costcenter, string expense, DateTime startDate, DateTime endDate)
        {
            DataTable dtToReturn = null;
            
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);

                if (costcenter == -1)
                {
                   sqlparams[2] = new SqlParameter("@cc", DBNull.Value);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@cc", costcenter);
                }


                if (expense == "All")
                {
                    sqlparams[3] = new SqlParameter("@Desc", "");
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@Desc", expense);
                }


                if (frequency == -1)
                {
                    sqlparams[4] = new SqlParameter("@frequency", DBNull.Value);
                }
                else if (frequency == 0)
                {
                    sqlparams[4] = new SqlParameter("@frequency", 14);
                }
                else if (frequency == 1)
                {
                    sqlparams[4] = new SqlParameter("@frequency", 15);
                }
                else if (frequency == 2)
                {
                    sqlparams[4] = new SqlParameter("@frequency", 16);
                }

                DateTime dateStart = (startDate);
                dateStart.ToString("DD-MMM-YYYY");

                DateTime dateEnd = (endDate);
                dateEnd.ToString("dd-MMM-yyyy"); 
       
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_BudgetAllocationReport , sqlparams);

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


        public static DataTable GetPeriod(int companyid, int periodid,  int CostCenter)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@CC", CostCenter);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPeriod, sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTables(int budgetallocationheaderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@BudgetAllocationHeaderID", budgetallocationheaderID);
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
