using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
  public partial   class HRMS_OrganoGram
  {
      #region Constant String 


      const string Query_jobDescription = @"

select
HRMS_organogramdetail.DetailOrganogramID
,HRMS_OrganoGram.OrganogramID
, desg.JobDesignationName as Designation
,rp.JobDesignationName as ReportingTo
,rptpostion.JobDesignationName as ReportingPosition

from  dbo.HRMS_OrganoGram
inner join SET_JobDesignation desg on desg.JobDesignationID =  HRMS_OrganoGram.DesignationID
and desg.Status =1 
inner join SET_JobDesignation rp on rp.JobDesignationID =  HRMS_OrganoGram.ReportingPosition
and rp.Status =1 

inner join HRMS_organogramdetail on HRMS_organogramdetail.OrganogramID = HRMS_OrganoGram.OrganogramID
and HRMS_organogramdetail.CompanyID = HRMS_OrganoGram.CompanyID
and HRMS_organogramdetail.PeriodID = HRMS_OrganoGram.PeriodID
and HRMS_organogramdetail.Status =1 



inner join SET_JobDesignation rptpostion on rptpostion.JobDesignationID =  HRMS_organogramdetail.DesignationID
and rptpostion.Status =1 
 
 
 where HRMS_OrganoGram.CompanyID =  @CompanyID
 and  HRMS_OrganoGram.PeriodID =  @PeriodID  
    {0}

";

      const string  Query_GetDataByOrganoGramID = @"
SELECT '' as [Action],
Header.JobDesignationName as ReportingPosition,
HRMS_OrganoGram.DesignationID,HeaderDesg.JobDesignationName as Designation,HRMS_OrganoGram.[FileName],
HRMS_OrganoGram.Organogramid
,HRMS_OrganoGram.[Attachment]
FROM HRMS_OrganoGram 

inner join SET_JobDesignation Header on Header.JobDesignationID = HRMS_OrganoGram.ReportingPosition
--and Header.CompanyID = HRMS_OrganoGram.CompanyID
and Header.Status = 1

inner join SET_JobDesignation HeaderDesg on HeaderDesg.JobDesignationID = HRMS_OrganoGram.DesignationID
--and HeaderDesg.CompanyID = HRMS_OrganoGram.CompanyID
and HeaderDesg.Status = 1

WHERE HRMS_OrganoGram.Status = 1 AND HRMS_OrganoGram.CompanyID = @CompanyID
{0}

";

      const string Query_DeleteDataByOrganogramID = @"

delete from HRMS_organogram where organogramid = @OrganogramID and CompanyID =@CompanyID
";

      const string Query_GetDataByOrganoGramsID = @"
SELECT '' as [Action],Detail.JobDesignationName as DesignationName,HRMS_OrganoGram.DesignationID as HeaderDesignationID,
HRMS_OrganoGram.[FileName],HRMS_OrganoGram.ReportingPosition,
HRMS_OrganoGram.Organogramid,HRMS_organogramdetail.DesignationID
,HRMS_OrganoGram.[Attachment]
FROM HRMS_OrganoGram 
 
inner join HRMS_organogramdetail on HRMS_organogramdetail.Organogramid = HRMS_OrganoGram.Organogramid
and HRMS_organogramdetail.CompanyID = HRMS_OrganoGram.CompanyID
and HRMS_organogramdetail.Status = 1

inner join SET_JobDesignation Detail on Detail.JobDesignationID = HRMS_organogramdetail.DesignationID
--and Detail.CompanyID = HRMS_organogramdetail.CompanyID
and Detail.Status = 1

WHERE HRMS_OrganoGram.Status = 1 AND HRMS_OrganoGram.CompanyID = @CompanyID

{0}

";

      const string Query_GetData_byOrganogramhdnID = @"
select * 
FROM HRMS_OrganoGram where Organogramid=@OrganogramID

";
      #endregion

      #region Methods

      public static bool InsertOrganoGramHeaderandDetail(FnI.BusinessAccessLayer.HRMS.Payroll.Setup.HRMS_OrganoGram obj, List<FnI.BusinessAccessLayer.HRMS.Payroll.Setup.HRMS_OrganoGramDetail> colOrganogramItems)
      {
          bool result = false;
          SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertHeaderAndDetail");
          try
          {
              obj.OrganogramID = (int)FnI.BusinessAccessLayer.HRMS.Payroll.Setup.HRMS_OrganoGram.GetNewOrganogramID(obj.CompanyID,obj.PeriodID);

              if (!obj.Insert(sqltrans))
                  throw new BusinessLogicException("Record is not inserted in OrganoGram Header");

              foreach (FnI.BusinessAccessLayer.HRMS.Payroll.Setup.HRMS_OrganoGramDetail td in colOrganogramItems)
              {

                  td.OrganogramID = obj.OrganogramID;
                  td.InsertWithAutoID(sqltrans);
              }

              result = true;
          }

          catch (BusinessLogicException ex)
          {
              SqlHelper.LogException(ex);
              throw ex;

          }

          finally
          {
              if (result)
                  SqlHelper.CommitTransaction(sqltrans);
              else
                  SqlHelper.RollBackTransaction(sqltrans, "InsertHeaderAndDetail");
          }

          return result;
      }


      public static DataTable GetDataByDesignationID(int CompanyID, int DesignationID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
      {
          DataTable dr = null;
          StringBuilder sb = new StringBuilder();
          try
          {
              SqlParameter[] sqlparams = new SqlParameter[2];
              sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);             
              sqlparams[1] = new SqlParameter("@DesignationID", DesignationID);

              DataSet ds = null;

              if (DesignationID > 0)
                  sb.Append(" and HRMS_OrganoGram.DesignationID = " + DesignationID);
              if (objTransaction != null)
                  ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDataByOrganoGramID, sb.ToString()), sqlparams);
              else if (objConnection != null)
                  ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDataByOrganoGramID, sb.ToString()), sqlparams);
              else
                  ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByOrganoGramID, sb.ToString()), sqlparams);

              if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                  dr = ds.Tables[0];
          }
          catch (Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }

          return dr;
      }


      public static DataRow GetDataByOrganoGramhdnID(int OrganogramhdnID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
      {
          DataRow dr = null;

          try
          {
              SqlParameter[] sqlparams = new SqlParameter[1];
              sqlparams[0] = new SqlParameter("@OrganogramID", OrganogramhdnID);

              DataSet ds = null;

              if (objTransaction != null)
                  ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byOrganogramhdnID, sqlparams);
              else if (objConnection != null)
                  ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byOrganogramhdnID, sqlparams);
              else
                  ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byOrganogramhdnID, sqlparams);

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



      public static DataTable Get_JobDescriptionReportData(int company , int period , Nullable<int> designation)
      {

          StringBuilder sb = new StringBuilder();
          DataTable dt = null;
          try {

              SqlParameter[] sqlparam = new SqlParameter[3];
              sqlparam[0] = new SqlParameter("@CompanyID",company );
              sqlparam[1] = new SqlParameter("@PeriodID",period);

              if (designation != null)
              {

                  sb.Append(" and desg.JobDesignationID =  " + designation + " ");
              }
              
              
              //Query_jobDescription
              DataSet ds = null;
              ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_jobDescription,sb), sqlparam);

              if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                  dt = ds.Tables[0];
          }
          catch(Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }


          return dt;
      }

      public static DataTable GetDataByOrganoGramID(int CompanyID, int OrganogramID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
      {
          DataTable dr = null;
          StringBuilder sb = new StringBuilder();
          try
          {
              SqlParameter[] sqlparams = new SqlParameter[2];
              sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
             
              sqlparams[1] = new SqlParameter("@OrganogramID", OrganogramID);

              DataSet ds = null;

              if (OrganogramID > 0)
                  sb.Append(" and HRMS_OrganoGram.OrganogramID = " + OrganogramID);
              if (objTransaction != null)
                  ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDataByOrganoGramsID, sb.ToString()), sqlparams);
              else if (objConnection != null)
                  ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDataByOrganoGramsID, sb.ToString()), sqlparams);
              else
                  ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByOrganoGramsID, sb.ToString()), sqlparams);

              if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                  dr = ds.Tables[0];
          }
          catch (Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }

          return dr;
      }




      public static bool DeleteDataByOrganoGramID(int OrganogramID, int CompanyID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
      {
          bool result = false;
       
          try
          {
              SqlParameter[] sqlparams = new SqlParameter[2];
              sqlparams[0] = new SqlParameter("@OrganogramID", OrganogramID);
              sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
             
                         
              if(objTransaction == null)
				{
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDataByOrganogramID, sqlparams) > 0)
						result = true;
				}
				else
				{
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteDataByOrganogramID, sqlparams) > 0)
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

      public static DataTable GetDataByOrganogramChart()
      {
          DataTable dr = new DataTable();
          StringBuilder sb = new StringBuilder();
          try
          {
              string qry = @"Select top 1
-1 as DetailOrganogramID
,HRMS_organogram.DesignationID
,SET_JobDesignation.JobDesignationName as Position,
0 as ManagerID, 
NULL as ManagerName,
HRMS_SEt_Employee.Picture,
HRMS_SEt_Employee.PersonID as EmployeeID, 
HRMS_SEt_Employee.FullName as EmployeeName
 From HRMS_organogram

Inner join HRMS_SEt_Employee on HRMS_SEt_Employee.FK_JobDesignationID = HRMS_organogram.DesignationID
Inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_organogram.ReportingPosition
Order By HRMS_organogram.OrganogramID";


              DataSet ds = null;

              ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(qry, sb.ToString()));

              if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                  dr = ds.Tables[0];
          }
          catch (Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }

          return dr;
      }

      public static DataTable GetDataForOrganogramByManagerID(int ManagerID)
      {
          DataTable dr = new DataTable();

          SqlParameter[] sqlparams = new SqlParameter[1];
          sqlparams[0] = new SqlParameter("@ManagerID", ManagerID);

          StringBuilder sb = new StringBuilder();
          try
          {
              string qry = @"
Select HRMS_OrganoGramDetail.DetailOrganogramID 
,HRMS_OrganoGramDetail.DesignationID
,SET_JobDesignation.JobDesignationName as Position
,HRMS_organogram.DesignationID as ManagerID
,Manager.JobDesignationName as ManagerName
,(Select Top 1 Picture from HRMS_SET_Employee where HRMS_SET_Employee.FK_JobDesignationID = SET_JobDesignation.JobDesignationID) as Picture
,(Select Top 1 PersonID from HRMS_SET_Employee where HRMS_SET_Employee.FK_JobDesignationID = SET_JobDesignation.JobDesignationID) as EmployeeID
,(Select Top 1 FullName from HRMS_SET_Employee where HRMS_SET_Employee.FK_JobDesignationID = SET_JobDesignation.JobDesignationID) as EmployeeName
--,HRMS_SET_Employee.PersonID as EmployeeID
--,HRMS_SET_Employee.FullName as EmployeeName
from HRMS_OrganoGramDetail
Inner join HRMS_organogram on HRMS_OrganoGramDetail.OrganogramID = HRMS_organogram.OrganogramID
And HRMS_OrganoGramDetail.CompanyID = HRMS_organogram.CompanyID
Inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_OrganoGramDetail.DesignationID
Inner join SET_JobDesignation Manager on Manager.JobDesignationID = HRMS_organogram.DesignationID
--Inner join HRMS_SET_Employee on HRMS_SET_Employee.FK_JobDesignationID = SET_JobDesignation.JobDesignationID
Where HRMS_organogram.ReportingPosition = @ManagerID
ORder by HRMS_OrganoGramDetail.DetailOrganogramID
";

              DataSet ds = null;

              ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(qry, sb.ToString()),sqlparams);

              if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                  dr = ds.Tables[0];
          }
          catch (Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }

          return dr;
      }

      public static DataTable GetDataByOrganogramChild()
      {
          DataTable dr = new DataTable();
          StringBuilder sb = new StringBuilder();
          try
          {
              string qry = @"
Select HRMS_organogram.OrganogramID, HRMS_OrganoGramDetail.DetailOrganogramID 
,HRMS_OrganoGramDetail.DesignationID
,SET_JobDesignation.JobDesignationName as Position
,HRMS_organogram.DesignationID as ManagerID
,Manager.JobDesignationName as ManagerName

,(Select Top 1 Picture from HRMS_SET_Employee where HRMS_SET_Employee.FK_JobDesignationID = SET_JobDesignation.JobDesignationID) as Picture
,(Select Top 1 PersonID from HRMS_SET_Employee where HRMS_SET_Employee.FK_JobDesignationID = SET_JobDesignation.JobDesignationID) as EmployeeID
,(Select Top 1 FullName from HRMS_SET_Employee where HRMS_SET_Employee.FK_JobDesignationID = SET_JobDesignation.JobDesignationID) as EmployeeName
--,HRMS_SET_Employee.PersonID as EmployeeID
--,HRMS_SET_Employee.FullName as EmployeeName

from HRMS_OrganoGramDetail
Inner join HRMS_organogram on HRMS_OrganoGramDetail.OrganogramID = HRMS_organogram.OrganogramID
And HRMS_OrganoGramDetail.CompanyID = HRMS_organogram.CompanyID
Inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_OrganoGramDetail.DesignationID
Inner join SET_JobDesignation Manager on Manager.JobDesignationID = HRMS_organogram.DesignationID
--Inner join HRMS_SET_Employee on HRMS_SET_Employee.FK_JobDesignationID = SET_JobDesignation.JobDesignationID
";

              DataSet ds = null;

              ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(qry, sb.ToString()));

              if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                  dr = ds.Tables[0];
          }
          catch (Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }

          return dr;
      }

    }
#endregion
}
