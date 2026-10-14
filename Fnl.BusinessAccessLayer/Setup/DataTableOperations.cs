using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public class DataTableOperations
    {
        public static DataTable MergeDataTable(DataTable dt1, DataTable dt2)
        {
            /*
             * Summary dtSource -> dtDestination => dtResult 
             * 
             */

            DataTable dtResult = null;
            dtResult = dt1.Clone();

            foreach (DataRow dr in dt1.Rows)
            {
                dtResult.Rows.Add(dr.ItemArray);
            }

            foreach (DataRow dr in dt2.Rows)
            {
                dtResult.Rows.Add(dr.ItemArray);
            }

            return dtResult;

        }

        public static DataTable DataTableGroupBy(DataTable dtSource)
        {
            DataTable dtResult = null;

            dtResult = dtSource.Clone();  //copy structure

            //dtResult = dtSource.AsEnumerable().GroupBy(DataRowComparer.Default).Select();



            return dtResult;

        }

        public static DataTable GetFiscalRecordsAndInProcessPrevious(DataTable dt, int CurrentFiscalID, bool previousData = false, string datecolumnname = "")
        {

            DataTable dt_PreviousRecord = new DataTable();
            DataRow drFiscal = SET_PeriodCustom.GetDataByKey(CurrentFiscalID);
            dt_PreviousRecord = dt.Clone();

            if (previousData)
            {
                if (dt.Select(" DocumentDate < '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "'").Length > 0)
                    dt_PreviousRecord = dt.Select(" DocumentDate < '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "'").CopyToDataTable();
                else
                    dt_PreviousRecord.Clear();
            }
            if (datecolumnname != "")
            {
                if (dt.Select(datecolumnname + "  >= '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "' and " + datecolumnname + " <= '" + DateTime.Parse(drFiscal["EndDate"].ToString()).ToShortDateString() + "'").Length > 0)
                    dt = dt.Select(datecolumnname + "  >= '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "' and " + datecolumnname + " <= '" + DateTime.Parse(drFiscal["EndDate"].ToString()).ToShortDateString() + "'").CopyToDataTable();
                else
                    dt.Clear();
            }
            else
            {
                if (dt.Select(" DocumentDate >= '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "' and DocumentDate <= '" + DateTime.Parse(drFiscal["EndDate"].ToString()).ToShortDateString() + "'").Length > 0)
                    dt = dt.Select(" DocumentDate >= '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "' and DocumentDate <= '" + DateTime.Parse(drFiscal["EndDate"].ToString()).ToShortDateString() + "'").CopyToDataTable();
                else
                    dt.Clear();
            }

            if (dt_PreviousRecord != null)
                dt.Merge(dt_PreviousRecord);

            try
            {

                dt = dt.Select("", "order by " + datecolumnname == "" ? "DocumentDate" : datecolumnname + " desc").CopyToDataTable();
            }
            catch { }


            return dt;
        }


        public static DataTable GetFiscalRecordsWebControllers(DataTable dt, int CurrentFiscalID, bool fetchPreviousInProcess = false)
        {


            DataTable dt_PreviousRecord = new DataTable();

            DataRow drFiscal = SET_PeriodCustom.GetDataByKey(CurrentFiscalID);

            dt_PreviousRecord = dt.Clone();

            if (fetchPreviousInProcess)
            {

                if (dt.Select(" DocumentDate <= '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "' and StatusCode = 5").Length > 0)
                    dt_PreviousRecord = dt.Select(" DocumentDate <= '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "' and StatusCode = 5").CopyToDataTable();
                else
                    dt_PreviousRecord.Clear();
            }

            if (dt.Select(" DocumentDate >= '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "' and DocumentDate <= '" + DateTime.Parse(drFiscal["EndDate"].ToString()).ToShortDateString() + "'").Length > 0)
                dt = dt.Select(" DocumentDate >= '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "' and DocumentDate <= '" + DateTime.Parse(drFiscal["EndDate"].ToString()).ToShortDateString() + "'").CopyToDataTable();
            else
                dt.Clear();

            if (dt_PreviousRecord != null)
                dt.Merge(dt_PreviousRecord);

            return dt;
        }


        public static DataTable GetFiscalRecords(DataTable dt, int CurrentFiscalID, bool previousData = false, string datecolumnname = "", string PageName = "")
        {



            DataTable dt_PreviousRecord = new DataTable();

            DataRow drFiscal = SET_PeriodCustom.GetDataByKey(CurrentFiscalID);



            dt_PreviousRecord = dt.Clone();

            if (previousData)
            {
                if (datecolumnname != "")
                {
                    if (dt.Select(datecolumnname + " < '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "'").Length > 0)
                        dt_PreviousRecord = dt.Select(datecolumnname + " < '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "'").CopyToDataTable();
                    else
                        dt_PreviousRecord.Clear();
                }
                else
                {

                    if (dt.Select(" DocumentDate < '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "'").Length > 0)
                        dt_PreviousRecord = dt.Select(" DocumentDate < '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "'").CopyToDataTable();
                    else
                        dt_PreviousRecord.Clear();

                }

            }
            if (datecolumnname != "")
            {
                if (dt.Select(datecolumnname + "  >= '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "' and " + datecolumnname + " <= '" + DateTime.Parse(drFiscal["EndDate"].ToString()).ToShortDateString() + "'").Length > 0)
                    dt = dt.Select(datecolumnname + "  >= '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "' and " + datecolumnname + " <= '" + DateTime.Parse(drFiscal["EndDate"].ToString()).ToShortDateString() + "'").CopyToDataTable();
                else
                    dt.Clear();
            }
            else
            {
                if (dt.Select(" DocumentDate >= '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "' and DocumentDate <= '" + DateTime.Parse(drFiscal["EndDate"].ToString()).ToShortDateString() + "'").Length > 0)
                    dt = dt.Select(" DocumentDate >= '" + DateTime.Parse(drFiscal["StartDate"].ToString()).ToShortDateString() + "' and DocumentDate <= '" + DateTime.Parse(drFiscal["EndDate"].ToString()).ToShortDateString() + "'").CopyToDataTable();
                else
                    dt.Clear();
            }

            if (dt_PreviousRecord != null)
                dt.Merge(dt_PreviousRecord);

            try
            {

                if (PageName == "Receipt")
                    dt = dt.Select("", "order by " + datecolumnname == "" ? "DocumentDate" : datecolumnname + " asc").CopyToDataTable();
                else
                    dt = dt.Select("", "order by " + datecolumnname == "" ? "DocumentDate" : datecolumnname + " desc").CopyToDataTable();
            }
            catch { }

            return dt;
        }

        public static DataTable GetGINData(DateTime? fromdate,DateTime? todate,string documentType)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@fromdate", fromdate);
                sqlparams[1] = new SqlParameter("@todate", todate);

                string query = @"


select gl_transactiondetail.TransactionId,gl_transactiondetail.amount, StockAmount,gl_transactiondetail.CodeCombinationID from (
select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * averagerate)StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID
	,inv_item.CodeCombinationID
 from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID     
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
inner join inv_item on inv_item.itemid =INV_STOCKTRANSACTIONDETAIL.itemid 
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE SET_DOCUMENTTYPE.DocumentType LIKE '%gin%%'

group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID,inv_item.CodeCombinationID   -----COST OF SALES ACCOUNT CCID
)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo and gl_transactionheader.FiscalID = t.FiscalID
and gl_transactionheader.documentdate between  @fromdate and @todate
inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
and t.CodeCombinationID = gl_transactiondetail.CodeCombinationID 

";



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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



        public static DataTable GetTransferOutData(DateTime? fromdate, DateTime? todate, string documentType)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@fromdate", fromdate);
                sqlparams[1] = new SqlParameter("@todate", todate);

                string query = @"

select gl_transactiondetail.TransactionId,gl_transactiondetail.amount, StockAmount,gl_transactiondetail.CodeCombinationID from (
select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * averagerate)StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID
	,inv_item.CodeCombinationID
 from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID     
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
inner join inv_item on inv_item.itemid =INV_STOCKTRANSACTIONDETAIL.itemid 
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE SET_DOCUMENTTYPE.DocumentType LIKE '%transfer%out%%'

group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID,inv_item.CodeCombinationID   -----COST OF SALES ACCOUNT CCID
)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo and gl_transactionheader.FiscalID = t.FiscalID
and gl_transactionheader.documentdate between  @fromdate and @todate
inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
and t.CodeCombinationID = gl_transactiondetail.CodeCombinationID 

";



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public static DataTable GetTransferInData(DateTime? fromdate, DateTime? todate, string documentType)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@fromdate", fromdate);
                sqlparams[1] = new SqlParameter("@todate", todate);

                string query = @"

select gl_transactiondetail.TransactionId,gl_transactiondetail.amount, StockAmount,gl_transactiondetail.CodeCombinationID from (
select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * transactioncost)StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID
	,inv_item.CodeCombinationID
 from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID     
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
inner join inv_item on inv_item.itemid =INV_STOCKTRANSACTIONDETAIL.itemid 
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE SET_DOCUMENTTYPE.DocumentType LIKE '%transfer%in%stock%'
and SET_DOCUMENTTYPE.DocumentType not LIKE '%transfer%in%fa%'
group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID,inv_item.CodeCombinationID   -----COST OF SALES ACCOUNT CCID
)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo and gl_transactionheader.FiscalID = t.FiscalID
and gl_transactionheader.documentdate between  @fromdate and @todate
inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
and t.CodeCombinationID = gl_transactiondetail.CodeCombinationID 

";



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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


        public static DataTable GetDispatchData(DateTime? fromdate, DateTime? todate, string documentType,int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@fromdate", fromdate);
                sqlparams[1] = new SqlParameter("@todate", todate);
                sqlparams[2] = new SqlParameter("@fiscalid", CurrentFiscalID);

                string query = @"
select * from (
select gl_transactiondetail.TransactionId, gl_transactiondetail.Amount, StockAmount,gl_transactiondetail.CodeCombinationID from (

select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * averagerate)StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID
	,CodeCombinationID
 from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID 
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1 and INV_STOCKTRANSACTIONHEADER.FiscalID = @fiscalid
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
inner join inv_item on inv_item.itemid =INV_STOCKTRANSACTIONDETAIL.itemid 
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE SET_DOCUMENTTYPE.DocumentType LIKE '%DISPATCH%'
and SET_DOCUMENTTYPE.DOCUMENTTYPE not LIKE '%DISPATCH%retur%'
group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID,CodeCombinationID

)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo 
and gl_transactionheader.FiscalID = t.FiscalID and gl_transactionheader.documentdate between  @fromdate and @todate

inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
and t.CodeCombinationID = gl_transactiondetail.CodeCombinationID 

where gl_transactiondetail.transactionid in (

select gl_transactiondetail.TransactionId from (
select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * averagerate)StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID 
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
and INV_STOCKTRANSACTIONHEADER.FiscalID = @fiscalid
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE SET_DOCUMENTTYPE.DocumentType LIKE '%DISPATCH%'
and SET_DOCUMENTTYPE.DOCUMENTTYPE not LIKE '%DISPATCH%retur%'
group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID
)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo and gl_transactionheader.FiscalID = t.FiscalID
and gl_transactionheader.documentdate between  @fromdate and @todate
inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
group by gl_transactiondetail.TransactionId having count(gl_transactiondetail.TransactionId)>2)
 and gl_transactiondetail.codecombinationid in 
 ('5-01-01-01-01-0005',
'5-01-01-01-01-0010',
'5-01-01-01-02-0005',
'1-02-01-01-01-0005',
'1-02-01-01-02-0005',
'1-02-01-01-03-0005')

union all

select gl_transactiondetail.TransactionId, gl_transactiondetail.Amount, StockAmount,gl_transactiondetail.CodeCombinationID from (



select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * averagerate)StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID
	,CostOfSalesAccountCCID
 from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID 
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
inner join inv_item on inv_item.itemid =INV_STOCKTRANSACTIONDETAIL.itemid 
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE SET_DOCUMENTTYPE.DocumentType LIKE '%DISPATCH%'
and SET_DOCUMENTTYPE.DOCUMENTTYPE not LIKE '%DISPATCH%retur%'
group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID,CostOfSalesAccountCCID
)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo 
and gl_transactionheader.FiscalID = t.FiscalID and gl_transactionheader.documentdate between  @fromdate and @todate 

inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
and t.CostOfSalesAccountCCID = gl_transactiondetail.CodeCombinationID 

where gl_transactiondetail.transactionid in (

select gl_transactiondetail.TransactionId from (
select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * averagerate)StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID 
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE SET_DOCUMENTTYPE.DocumentType LIKE '%DISPATCH%'
and SET_DOCUMENTTYPE.DOCUMENTTYPE not LIKE '%DISPATCH%retur%'
group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID
)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo and gl_transactionheader.FiscalID = t.FiscalID
and gl_transactionheader.documentdate between  @fromdate and @todate
inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
group by gl_transactiondetail.TransactionId having count(gl_transactiondetail.TransactionId)>2)
 and gl_transactiondetail.codecombinationid in 
 ('5-01-01-01-01-0005',
'5-01-01-01-01-0010',
'5-01-01-01-02-0005',
'1-02-01-01-01-0005',
'1-02-01-01-02-0005',
'1-02-01-01-03-0005')


) t



";



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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


        public static DataTable GetDispatchReturnData(DateTime? fromdate, DateTime? todate, string documentType, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@fromdate", fromdate);
                sqlparams[1] = new SqlParameter("@todate", todate);
                sqlparams[2] = new SqlParameter("@fiscalid", CurrentFiscalID);

                string query = @"


select * from (
select gl_transactiondetail.TransactionId, gl_transactiondetail.Amount, StockAmount,gl_transactiondetail.CodeCombinationID from (

select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * isnull(averagerate,transactioncost))StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID
	,InventoryAccountCCID
 from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID 
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1 and INV_STOCKTRANSACTIONHEADER.FiscalID = @fiscalid
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
inner join inv_item on inv_item.itemid =INV_STOCKTRANSACTIONDETAIL.itemid 
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE  SET_DOCUMENTTYPE.DOCUMENTTYPE LIKE '%DISPATCH%return%'
group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID,InventoryAccountCCID

)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo 
and gl_transactionheader.FiscalID = t.FiscalID and gl_transactionheader.documentdate between  @fromdate and @todate

inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
and t.InventoryAccountCCID = gl_transactiondetail.CodeCombinationID 

where gl_transactiondetail.transactionid in (

select gl_transactiondetail.TransactionId from (
select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * isnull(averagerate,transactioncost))StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID 
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
and INV_STOCKTRANSACTIONHEADER.FiscalID = 7
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE SET_DOCUMENTTYPE.DocumentType LIKE '%DISPATCH%return%'
group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID
)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo and gl_transactionheader.FiscalID = t.FiscalID
and gl_transactionheader.documentdate between  @fromdate and @todate
inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
group by gl_transactiondetail.TransactionId having count(gl_transactiondetail.TransactionId)>2)
 and gl_transactiondetail.codecombinationid in 
('1-02-01-01-01-0005',
'1-02-01-01-02-0005',
'1-02-02-01-01-0030',
'4-01-01-01-01-0005',
'4-01-01-01-02-0005',
'4-01-01-02-01-0025',
'5-01-01-01-01-0005',
'5-01-01-01-02-0005')

union all 


select gl_transactiondetail.TransactionId, gl_transactiondetail.Amount, StockAmount,gl_transactiondetail.CodeCombinationID from (

select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * isnull(averagerate,transactioncost))StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID
	,SalesAccountCCID
 from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID 
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1 and INV_STOCKTRANSACTIONHEADER.FiscalID = @fiscalid
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
inner join inv_item on inv_item.itemid =INV_STOCKTRANSACTIONDETAIL.itemid 
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE  SET_DOCUMENTTYPE.DOCUMENTTYPE LIKE '%DISPATCH%return%'
group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID,SalesAccountCCID

)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo 
and gl_transactionheader.FiscalID = t.FiscalID and gl_transactionheader.documentdate between  @fromdate and @todate

inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
and t.SalesAccountCCID = gl_transactiondetail.CodeCombinationID 

where gl_transactiondetail.transactionid in (

select gl_transactiondetail.TransactionId from (
select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * isnull(averagerate,transactioncost))StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID 
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
and INV_STOCKTRANSACTIONHEADER.FiscalID = 7
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE SET_DOCUMENTTYPE.DocumentType LIKE '%DISPATCH%return%'
group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID
)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo and gl_transactionheader.FiscalID = t.FiscalID
and gl_transactionheader.documentdate between  @fromdate and @todate
inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
group by gl_transactiondetail.TransactionId having count(gl_transactiondetail.TransactionId)>2)
 and gl_transactiondetail.codecombinationid in 
('1-02-01-01-01-0005',
'1-02-01-01-02-0005',
'1-02-02-01-01-0030',
'4-01-01-01-01-0005',
'4-01-01-01-02-0005',
'4-01-01-02-01-0025',
'5-01-01-01-01-0005',
'5-01-01-01-02-0005')



union all

select gl_transactiondetail.TransactionId, gl_transactiondetail.Amount, StockAmount,gl_transactiondetail.CodeCombinationID from (



select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * isnull(averagerate,transactioncost))StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID
	,salesAccountCCID
 from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID 
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1 and INV_STOCKTRANSACTIONHEADER.FiscalID = @fiscalid
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
inner join inv_item on inv_item.itemid =INV_STOCKTRANSACTIONDETAIL.itemid 
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE SET_DOCUMENTTYPE.DocumentType LIKE '%DISPATCH%return%'
group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID,salesAccountCCID
)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo 
and gl_transactionheader.FiscalID = t.FiscalID and gl_transactionheader.documentdate between  @fromdate and @todate

inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
and t.salesAccountCCID = gl_transactiondetail.CodeCombinationID 

where gl_transactiondetail.transactionid in (

select gl_transactiondetail.TransactionId from (
select INV_STOCKTRANSACTIONHEADER.DocumentTypeID, INV_STOCKTRANSACTIONHEADER.DocumentNo,sum(abs(transactionquantity) * isnull(averagerate,transactioncost))StockAmount,INV_STOCKTRANSACTIONHEADER.FiscalID from 
INV_STOCKTRANSACTIONDETAIL
INNER JOIN INV_STOCKTRANSACTIONHEADER ON INV_STOCKTRANSACTIONHEADER.STOCKTRANSACTIONID = INV_STOCKTRANSACTIONDETAIL.STOCKTRANSACTIONID 
and INV_STOCKTRANSACTIONHEADER.Status = 1 and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
INNER JOIN SET_DOCUMENTTYPE ON SET_DOCUMENTTYPE.DocumentTypeID = INV_STOCKTRANSACTIONHEADER.DocumentTypeID and SET_DOCUMENTTYPE.ChartOFAccountID = 1
and INV_STOCKTRANSACTIONHEADER.StatusCode = 1
WHERE 
 SET_DOCUMENTTYPE.DOCUMENTTYPE LIKE '%DISPATCH%retur%'
group by INV_STOCKTRANSACTIONHEADER.DocumentTypeID,INV_STOCKTRANSACTIONHEADER.DocumentNo,INV_STOCKTRANSACTIONHEADER.FiscalID
)t
inner join gl_transactionheader on gl_transactionheader.DocumentTypeID = t.DocumentTypeID and gl_transactionheader.DocumentNo = t.DocumentNo and gl_transactionheader.FiscalID = t.FiscalID
and gl_transactionheader.documentdate between  @fromdate and @todate
inner join gl_transactiondetail on gl_transactiondetail.transactionid = gl_transactionheader.transactionid 
group by gl_transactiondetail.TransactionId having count(gl_transactiondetail.TransactionId)>2)
 and gl_transactiondetail.codecombinationid in 

('1-02-01-01-01-0005',
'1-02-01-01-02-0005',
'1-02-02-01-01-0030',
'4-01-01-01-01-0005',
'4-01-01-01-02-0005',
'4-01-01-02-01-0025',
'5-01-01-01-01-0005',
'5-01-01-01-02-0005')
) t




";



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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



        public static bool UpdateGLEntry(int TransactionId, decimal StockAmount, string	CodeCombinationID)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@TransactionId", TransactionId);
                sqlparams[1] = new SqlParameter("@StockAmount", StockAmount);
                sqlparams[2] = new SqlParameter("@CodeCombinationID", CodeCombinationID);

                string Query = @" update gl_transactiondetail set amountfc=@StockAmount ,amount = @StockAmount where transactionid = @TransactionId and codecombinationid = @CodeCombinationID;


";


                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        public static bool UpdateDebitSide(int TransactionId, decimal StockAmount)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@TransactionId", TransactionId);
                sqlparams[1] = new SqlParameter("@StockAmount", StockAmount);
                

                string Query = @" update gl_transactiondetail set amountfc=@StockAmount ,amount = @StockAmount where transactionid = @TransactionId and TransactionType ='Debit';


";


                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }



        public static bool UpdateHeaderGL(int TransactionId)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@TransactionId", TransactionId);


                string Query = @" 

update gl_transactionheader set controldebit =
isnull((select isnull(sum(amountfc),0) from 
gl_transactiondetail  where status = 1 and gl_transactiondetail.transactiontype = 'Debit'  and gl_transactiondetail.transactionid = gl_transactionheader.transactionid

group by transactionid),0)
where gl_transactionheader.transactionid =  @TransactionId;

update gl_transactionheader set controlcredit =
isnull((select isnull(sum(amountfc),0) from 
gl_transactiondetail  where status = 1 and gl_transactiondetail.transactiontype = 'Credit'  and gl_transactiondetail.transactionid = gl_transactionheader.transactionid
and gl_transactionheader.transactionid =  @TransactionId
group by transactionid),0)

where gl_transactionheader.transactionid =  @TransactionId;

";


                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        public static DataTable GetTransferUpdateDebit(int TransactionID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@TransactionID", TransactionID);
           

                string query = @"


                    select * from 
                    gl_transactiondetail where transactionid = @TransactionID 

                ";  



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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


        public static decimal GetCreditTotal(int TransactionID)
        {
            decimal creditTotal = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@TransactionID", TransactionID);


                string query = @"
                    select sum(Amount)Amount from 
                    gl_transactiondetail where transactionid = @TransactionID 
                    and TransactionType ='Credit'
                ";



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    creditTotal = Convert.ToDecimal(ds.Tables[0].Rows[0]["Amount"].ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return creditTotal;
        }

        public static DataTable GetGeneralLedgerData(DateTime? fromdate, DateTime? todate, int? departmentId, int? constcenterId, string CodeCombinationID, int CurrentFiscalID, int CompanyID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@fromdate", fromdate);
                sqlparams[1] = new SqlParameter("@todate", todate);
                sqlparams[2] = new SqlParameter("@fiscalid", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@CostCenterID", constcenterId);
                sqlparams[4] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[5] = new SqlParameter("@DepartmentID", departmentId);

                if (CodeCombinationID != "")
                    sb.Append(" and gl_codecombinationdetail.CodeCombinationID='" + CodeCombinationID + "'");
                if(constcenterId.HasValue)
                    sb.Append("  AND gl_transactiondetail.CostCenterID=@CostCenterID");
                if(departmentId.HasValue)
                    sb.Append("  AND gl_transactiondetail.DepartmentID=@DepartmentID");


                #region
                string query = @"select set_accounttype.AccountTypeName,gl_codecombinationdetail.codecombinationid,gl_codecombinationdetail.Description, sum((case when transactiontype = 'Credit' then amount else 0 end))Credit, 
sum((case when transactiontype = 'Debit' then amount else 0 end))Debit from gl_transactiondetail
inner join gl_transactionheader on gl_transactionheader.transactionid = gl_transactiondetail.transactionid
and gl_transactionheader.CompanyID = gl_transactiondetail.CompanyID
and gl_transactionheader.documentdate  between @fromdate and @todate
and gl_transactionheader.status = 1 and gl_transactionheader.statuscode = 1
and gl_transactiondetail.status = 1
inner join gl_codecombinationdetail  on gl_codecombinationdetail.codecombinationid = gl_transactiondetail.codecombinationid 
inner join set_accounttype on set_accounttype.accounttypeid = gl_codecombinationdetail.accounttypeid and set_accounttype.accounttypeid in (11)
where gl_transactionheader.CompanyID = @CompanyID
{0}
group by set_accounttype.AccountTypeName,gl_codecombinationdetail.codecombinationid,gl_codecombinationdetail.Description
union all
select set_accounttype.AccountTypeName,gl_codecombinationdetail.codecombinationid,gl_codecombinationdetail.Description, sum((case when transactiontype = 'Credit' then amount else 0 end))Credit, 
sum((case when transactiontype = 'Debit' then amount else 0 end))Debit from gl_transactiondetail
inner join gl_transactionheader on gl_transactionheader.transactionid = gl_transactiondetail.transactionid
and gl_transactionheader.CompanyID = gl_transactiondetail.CompanyID
and gl_transactionheader.documentdate between @fromdate and @todate
and gl_transactionheader.status = 1 and gl_transactionheader.statuscode = 1
and gl_transactiondetail.status = 1
inner join gl_codecombinationdetail  on gl_codecombinationdetail.codecombinationid = gl_transactiondetail.codecombinationid 
inner join set_accounttype on set_accounttype.accounttypeid = gl_codecombinationdetail.accounttypeid and set_accounttype.accounttypeid in (12)
where gl_transactionheader.CompanyID = @CompanyID
{0}
group by set_accounttype.AccountTypeName,gl_codecombinationdetail.codecombinationid,gl_codecombinationdetail.Description
";
                #endregion



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(query, sb), sqlparams);

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


        public static DataTable GetGeneralLedgerDataForHSM(DateTime? fromdate, DateTime? todate, string CodeCombinationID, int CurrentFiscalID, int? constcenterId = 0, int CompanyID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@fromdate", fromdate);
                sqlparams[1] = new SqlParameter("@todate", todate);
                sqlparams[2] = new SqlParameter("@fiscalid", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@CostCenterID", constcenterId);
                sqlparams[4] = new SqlParameter("@CompanyID", CompanyID);
                if (CodeCombinationID != "")
                    sb.Append(" and gl_codecombinationdetail.CodeCombinationID='" + CodeCombinationID + "'");
                if (constcenterId.HasValue)
                    sb.Append("  AND gl_transactiondetail.CostCenterID=@CostCenterID");


                #region
                string query = @"


select Distinct isnull(gl_transactiondetail.CostCenterID,0) as CostCenterID,  isnull(gl_transactiondetail.DepartmentID,0) as DepartmentID, set_accounttype.AccountTypeName,gl_codecombinationdetail.codecombinationid,gl_codecombinationdetail.Description, sum((case when transactiontype = 'Credit' then amount else 0 end))Credit, 
sum((case when transactiontype = 'Debit' then amount else 0 end))Debit  from gl_transactiondetail
inner join gl_transactionheader on gl_transactionheader.transactionid = gl_transactiondetail.transactionid
and gl_transactionheader.CompanyID = gl_transactiondetail.CompanyID
and gl_transactionheader.documentdate  between @fromdate and @todate
and gl_transactionheader.status = 1 and gl_transactionheader.statuscode = 1
and gl_transactiondetail.status = 1
inner join gl_codecombinationdetail  on gl_codecombinationdetail.codecombinationid = gl_transactiondetail.codecombinationid 
inner join set_accounttype on set_accounttype.accounttypeid = gl_codecombinationdetail.accounttypeid and set_accounttype.accounttypeid in (11)
where gl_transactionheader.CompanyID = @CompanyID
{0}
group by set_accounttype.AccountTypeName,gl_codecombinationdetail.codecombinationid,gl_codecombinationdetail.Description,gl_transactiondetail.CostCenterID, gl_transactiondetail.DepartmentID
union all
select Distinct isnull(gl_transactiondetail.CostCenterID,0) as CostCenterID,  isnull(gl_transactiondetail.DepartmentID,0) as DepartmentID, set_accounttype.AccountTypeName,gl_codecombinationdetail.codecombinationid,gl_codecombinationdetail.Description, sum((case when transactiontype = 'Credit' then amount else 0 end))Credit, 
sum((case when transactiontype = 'Debit' then amount else 0 end))Debit  from gl_transactiondetail
inner join gl_transactionheader on gl_transactionheader.transactionid = gl_transactiondetail.transactionid
and gl_transactionheader.CompanyID = gl_transactiondetail.CompanyID
and gl_transactionheader.documentdate between @fromdate and @todate
and gl_transactionheader.status = 1 and gl_transactionheader.statuscode = 1
and gl_transactiondetail.status = 1
inner join gl_codecombinationdetail  on gl_codecombinationdetail.codecombinationid = gl_transactiondetail.codecombinationid 
inner join set_accounttype on set_accounttype.accounttypeid = gl_codecombinationdetail.accounttypeid and set_accounttype.accounttypeid in (12)
where gl_transactionheader.CompanyID = @CompanyID
{0}
group by set_accounttype.AccountTypeName,gl_codecombinationdetail.codecombinationid,gl_codecombinationdetail.Description,gl_transactiondetail.CostCenterID,  gl_transactiondetail.DepartmentID


";
                #endregion



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(query, sb), sqlparams);

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

    }
}
