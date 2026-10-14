using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_SalesTax
    {
        #region Queries

        const string Query_Select_SalesTax_ByBetweenFromAndTo = @" Select top 1 * from SET_SalesTax 
            where @SelectedDate between FromDate and ToDate and ProductID = @ProductID ";

        const string Query_IncomeTaxAmount = @" select * from [HRMS_IncomeTaxParameters]  where PeriodID =  @PeriodID";


        #endregion

        #region Class Methods

        public static DataTable Get_SalesTax_ByBetweenFromAndToDate(DateTime selectedDate, int itemCategoryID)
        {
            DataTable dtToRetrun = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@SelectedDate", selectedDate);
                sqlparams[1] = new SqlParameter("@ProductID", itemCategoryID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_SalesTax_ByBetweenFromAndTo, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToRetrun = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToRetrun;
        }
        
        public static void GetCurrentTaxInfo(DataRow drow, decimal rate, int quantity, ref decimal amountInclusiveOfTaxes, ref decimal amountExclusiveOfTaxes, 
            ref decimal salesTaxAmount, ref decimal exciseDutyAmount, ref decimal fedAmount, ref decimal vatAmount, bool isSaleTaxExempted, bool isExciseDutyExempted, 
            bool isFEDExempted)
        {
            amountInclusiveOfTaxes = rate * quantity;
            decimal totalTaxPercent = 0;

            if (int.Parse(drow["SalesTaxFixRate"].ToString()) == 0)
            {
                totalTaxPercent += (!isSaleTaxExempted ? decimal.Parse(drow["SalesTaxPercent"].ToString()) : 0);
                totalTaxPercent += (!isExciseDutyExempted ? decimal.Parse(drow["ExciseDuty"].ToString()) : 0);
                totalTaxPercent += (!isFEDExempted ? decimal.Parse(drow["FED"].ToString()) : 0);
                amountExclusiveOfTaxes = amountInclusiveOfTaxes - (amountInclusiveOfTaxes * (totalTaxPercent / (100 + totalTaxPercent)));
                salesTaxAmount = (!isSaleTaxExempted ? decimal.Parse(drow["SalesTaxPercent"].ToString()) : 0) * amountExclusiveOfTaxes / 100;
                exciseDutyAmount = (!isExciseDutyExempted ? decimal.Parse(drow["ExciseDuty"].ToString()) : 0) * amountExclusiveOfTaxes / 100;
                fedAmount = (!isFEDExempted ? decimal.Parse(drow["FED"].ToString()) : 0) * amountExclusiveOfTaxes / 100;
            }
            else
            {
                decimal FixedAmount = rate * int.Parse(drow["SalesTaxFixRate"].ToString());
                salesTaxAmount = (!isSaleTaxExempted ? decimal.Parse(drow["SalesTaxPercent"].ToString()) : 0) * FixedAmount / 100;
                exciseDutyAmount = (!isExciseDutyExempted ? decimal.Parse(drow["ExciseDuty"].ToString()) : 0) * FixedAmount / 100;
                fedAmount = (!isFEDExempted ? decimal.Parse(drow["FED"].ToString()) : 0) * FixedAmount / 100;
                amountExclusiveOfTaxes = amountInclusiveOfTaxes - (salesTaxAmount + exciseDutyAmount + fedAmount);
            }
            
            amountInclusiveOfTaxes = Math.Round(amountInclusiveOfTaxes, 2);
            amountExclusiveOfTaxes = Math.Round(amountExclusiveOfTaxes, 2);
            salesTaxAmount = Math.Round(salesTaxAmount, 2);
            exciseDutyAmount = Math.Round(exciseDutyAmount, 2);
            fedAmount = Math.Round(fedAmount, 2);
            vatAmount = Math.Round(vatAmount, 2);
        }

        #endregion

        public static DataTable GetIncomeTaxAmount(int CurrentPeriodID,SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            DataTable dtToRetrun = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@PeriodID", CurrentPeriodID);
                //sqlparams[1] = new SqlParameter("@ProductID", itemCategoryID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_IncomeTaxAmount, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToRetrun = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToRetrun;
        }

        public static DataTable GetIncomeTaxAmountNew(int CurrentPeriodID, DateTime Date, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            DataTable dtToRetrun = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@PeriodID", CurrentPeriodID);
                sqlparams[1] = new SqlParameter("@Date", Date);

                const string Query = @"
SELECT * FROM (
SELECT PeriodID, IncomeTaxParametersID, FromAmount, ToAmount, Percentage, ExceedAmount, FixedTaxAmount, EffectiveDate,
(CASE WHEN EXISTS (SELECT 1 FROM HRMS_IncomeTaxParameters dt WHERE YEAR(dt.EffectiveDate) = YEAR(HRMS_IncomeTaxParameters.EffectiveDate) + 1) 
THEN (SELECT TOP 1 DATEADD(day, -1, dt.EffectiveDate) FROM HRMS_IncomeTaxParameters dt WHERE YEAR(dt.EffectiveDate) = YEAR(HRMS_IncomeTaxParameters.EffectiveDate) + 1)
ELSE '9999-12-31' END) as EffectiveToDate, IsActive, Status, CreatedBy, CreateDate, CreatedByValue, UpdateDate, UpdatedBy, UpdatedByValue
FROM HRMS_IncomeTaxParameters
)Fintab
WHERE @Date BETWEEN Fintab.EffectiveDate AND Fintab.EffectiveToDate
";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToRetrun = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToRetrun;
        }
    }
}
