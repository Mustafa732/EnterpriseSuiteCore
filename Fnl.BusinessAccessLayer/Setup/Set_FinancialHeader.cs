using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class Set_FinancialHeader
    {
        #region Contant Queries

        const string Query_Select_FinancialHeader = @" SELECT *, '' AS Action FROM Set_FinancialHeader WHERE FinancialName in ({0}) ORDER BY ElementSequence ";

        const string Query_Select_By_ReportName = @" SELECT *, '' AS Action FROM Set_FinancialHeader WHERE FinancialName = @FinancialName And FinancialCode= @FinancialCode ORDER BY ElementSequence ";

        const string Query_GetFinDetail_MaxId = @"SELECT IsNull(max(FinancialDetailID), 0) + 1 as FinancialDetailID FROM Set_FinancialDetail";

        const string Query_Select_By_PrimaryKey = @" 
SELECT FinancialID, FinancialName, NewPage, case ElementType when 'H' then 'Heading' when 'D' then 'Details' else '' end as ElementType, Alignment, Bold, ElementName, 
    NoteNo, ExpectedResultInDb, DataIn, FixedInput, Amount, 
    case DataToBeCollected when 'D' then 'Debit' when 'C' then 'Credit' when 'B' then 'Both' else '' end as DataToBeCollected, Formula, '' AS Action 
FROM Set_FinancialHeader WHERE Set_FinancialHeader.FinancialID = @financialID ";

        const string Query_Delete_ByKey = @" delete from SET_FinancialDetail where FinancialHeaderID = @FinancialID; 
delete from SET_FinancialHeader where FinancialID = @FinancialID ";

        const string Query_GetUniqueFinancialReportName = @" Select Distinct FinancialName,FinancialCode From Set_FinancialHeader {0}";

        const string Query_Select_By_NoteNo = @" SELECT * FROM Set_FinancialHeader WHERE FinancialCode = @NoteNo {0} ";

        const string Query_Get_AllSequence = @"Select ElementSequence from SET_FinancialHeader where ElementSequence = @ElementSequence AND FinancialName = @FinancialName";
        const string Query_SelectNoteNo_By_ReportName = @" SELECT * FROM Set_FinancialHeader WHERE NoteNo is not null {0}  ";
        #endregion

        #region Public Methods

        public static DataTable Get_FinacialHeader(string reportsName, SqlConnection objConnection = null, string NoteNo = "",bool IsMonthwise=false,string CompanyID = "1")
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet dsToReturn = null;
                string filterHeaders = "";
                string Segmentfilter = "";
                string query = @" SELECT  FinancialID	,
 FinancialName,
NewPage,
ElementType,
Alignment,
Bold ,
ElementSequence	,

  ElementName,
 NoteNo,
ExpectedResultInDb,
DataIn,
FixedInput,
 Amount,
 DataToBeCollected,
 Formula,
FinancialCode,
 Segment,
            COACodeFrom	,
             COACodeTo,'' as CodeCombinationID, '' AS Action, 1 as OrderBy

FROM Set_FinancialHeader WHERE 
--Set_FinancialHeader.companyid in @CompanyID and 
FinancialName in ({0}) {1}  {2} ORDER BY ElementSequence ";

               

                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                if (NoteNo != "")
                {
                    sqlparams[1] = new SqlParameter("@NoteNo", NoteNo);
                    Segmentfilter = " AND ( NoteNo =@NoteNo OR NoteChild=@NoteNo )";
                }
                else
                    sqlparams[1] = new SqlParameter("@NoteNo", DBNull.Value);
                if (IsMonthwise)
                {
                    filterHeaders = " AND ElementSequence not in (1,2,3,4)";
                
                }

                CompanyID = "(" + CompanyID + ")";

                if (objConnection != null)
                    dsToReturn = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query, reportsName, Segmentfilter, filterHeaders).Replace("@CompanyID", CompanyID), sqlparams);
                else
                    dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, reportsName, Segmentfilter, filterHeaders), sqlparams);

                if (dsToReturn != null && dsToReturn.Tables.Count > 0)
                    dtToReturn = dsToReturn.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable Get_DataByReportName(string financialName,string financialCode)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FinancialName", financialName);
                sqlparams[1] = new SqlParameter("@FinancialCode", financialCode);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_ReportName, sqlparams);

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

        public bool InsertFinancialReport(List<Set_FinancialHeader> lst_FinancialHeaders, List<Set_FinancialDetail> lst_FinancialDetails)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertFinancialReport");

            try
            {
                foreach (Set_FinancialHeader obj_FinancialHeader in lst_FinancialHeaders)
                {
                    obj_FinancialHeader.FinancialID = GetNewFinancialID(sqltrans);
                    obj_FinancialHeader.Insert(sqltrans);

                    List<Set_FinancialDetail> lst_CurrentDetails = 
                        lst_FinancialDetails.Where(x => x.HeaderSequence == obj_FinancialHeader.ElementSequence).ToList<Set_FinancialDetail>();

                    foreach (Set_FinancialDetail sfd in lst_CurrentDetails)
                    {
                        sfd.FinancialHeaderID = obj_FinancialHeader.FinancialID;
                        sfd.FinancialDetailID = short.Parse(Set_FinancialHeader.GetFinancialDetailID(sqltrans).ToString());
                        sfd.Insert(sqltrans);
                    }
                }

                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "InsertFinancialReport");
            }

            return result;
        }

        public static int GetFinancialDetailID(SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;
            try
            {
                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetFinDetail_MaxId));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetFinDetail_MaxId));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetFinDetail_MaxId));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static bool DeleteHeaderAndDetailRecord(List<Set_FinancialHeader> lst_FinancialHeaderDelete, List<Set_FinancialDetail> colFinDetailsDelete)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DelFinancialDetails");

            try
            {
               
                #region Header Deletion & respective details
                foreach (Set_FinancialHeader objhdr in lst_FinancialHeaderDelete)
                {
                    Set_FinancialHeader.DeleteDataByKey(sqltrans, objhdr.FinancialID);
                    
                }

                foreach (Set_FinancialDetail objdet in colFinDetailsDelete)
                {

                    Set_FinancialDetail.DeleteDataByKey(objdet.FinancialHeaderID, objdet.FinancialDetailID, sqltrans);
                }
               

                #endregion

                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "DelFinancialDetails");
            }

            return result;
        }

        public bool UpdateFinHeaderAndDetails(List<Set_FinancialHeader> lst_FinancialHeaderAdd, List<Set_FinancialHeader> lst_FinancialHeaderUpdate, 
            List<Set_FinancialHeader> lst_FinancialHeaderDelete, List<Set_FinancialDetail> colFinDetailsAdd, 
            List<Set_FinancialDetail> colFinDetailsUpdate, List<Set_FinancialDetail> colFinDetailsDelete)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateFinancialDetails");

            try
            {
                #region Header Insertion & respective details

                foreach (Set_FinancialHeader obj_FinancialHeader in lst_FinancialHeaderAdd)
                {
                    obj_FinancialHeader.FinancialID = GetNewFinancialID(sqltrans);
                    obj_FinancialHeader.Insert(sqltrans);

                    List<Set_FinancialDetail> lst_CurrentDetails =
                        colFinDetailsAdd.Where(x => x.HeaderSequence == obj_FinancialHeader.ElementSequence).ToList<Set_FinancialDetail>();

                    foreach (Set_FinancialDetail sfd in lst_CurrentDetails)
                    {
                        sfd.FinancialHeaderID = obj_FinancialHeader.FinancialID;
                        sfd.FinancialDetailID = short.Parse(Set_FinancialHeader.GetFinancialDetailID(sqltrans).ToString());
                        sfd.Insert(sqltrans);
                    }
                    
                    lst_CurrentDetails = colFinDetailsUpdate.Where(x => x.HeaderSequence == obj_FinancialHeader.ElementSequence).ToList<Set_FinancialDetail>();

                    foreach (Set_FinancialDetail sfd in lst_CurrentDetails)
                    { //  When Header is added then Updated detail will also be inserted not updated and no record will be deleted
                        sfd.FinancialHeaderID = obj_FinancialHeader.FinancialID;
                        sfd.FinancialDetailID = short.Parse(Set_FinancialHeader.GetFinancialDetailID(sqltrans).ToString());
                        sfd.Insert(sqltrans);
                    }
                }

                #endregion

                #region Header Updation & respective details

                foreach (Set_FinancialHeader obj_FinancialHeader in lst_FinancialHeaderUpdate)
                {
                    obj_FinancialHeader.UpdateDataByKey(sqltrans);

                    List<Set_FinancialDetail> lst_CurrentDetails =
                        colFinDetailsAdd.Where(x => x.HeaderSequence == obj_FinancialHeader.ElementSequence).ToList<Set_FinancialDetail>();

                    foreach (Set_FinancialDetail sfd in lst_CurrentDetails)
                    {
                        sfd.FinancialHeaderID = obj_FinancialHeader.FinancialID;
                        sfd.FinancialDetailID = short.Parse(Set_FinancialHeader.GetFinancialDetailID(sqltrans).ToString());
                        sfd.Insert(sqltrans);
                    }

                    lst_CurrentDetails = colFinDetailsUpdate.Where(x => x.HeaderSequence == obj_FinancialHeader.ElementSequence).ToList<Set_FinancialDetail>();

                    foreach (Set_FinancialDetail sfd in lst_CurrentDetails)
                        sfd.UpdateDataByKey(sqltrans);

                    lst_CurrentDetails = colFinDetailsDelete.Where(x => x.HeaderSequence == obj_FinancialHeader.ElementSequence).ToList<Set_FinancialDetail>();

                    foreach (Set_FinancialDetail sfd in lst_CurrentDetails)
                        Set_FinancialDetail.DeleteDataByKey(sfd.FinancialHeaderID, sfd.FinancialDetailID, sqltrans);
                }

                #endregion

                #region Header Deletion & respective details

                foreach (Set_FinancialHeader obj_FinancialHeader in lst_FinancialHeaderDelete)
                {
                    Set_FinancialHeader.DeleteDataByKey(sqltrans, obj_FinancialHeader.FinancialID);

                    List<Set_FinancialDetail> lst_CurrentDetails =
                        colFinDetailsAdd.Where(x => x.HeaderSequence == obj_FinancialHeader.ElementSequence).ToList<Set_FinancialDetail>();

                    foreach (Set_FinancialDetail sfd in lst_CurrentDetails)
                        Set_FinancialDetail.DeleteDataByKey(sfd.FinancialHeaderID, sfd.FinancialDetailID, sqltrans);

                    lst_CurrentDetails = colFinDetailsUpdate.Where(x => x.HeaderSequence == obj_FinancialHeader.ElementSequence).ToList<Set_FinancialDetail>();

                    foreach (Set_FinancialDetail sfd in lst_CurrentDetails)
                        Set_FinancialDetail.DeleteDataByKey(sfd.FinancialHeaderID, sfd.FinancialDetailID, sqltrans);

                    lst_CurrentDetails = colFinDetailsDelete.Where(x => x.HeaderSequence == obj_FinancialHeader.ElementSequence).ToList<Set_FinancialDetail>();

                    foreach (Set_FinancialDetail sfd in lst_CurrentDetails)
                        Set_FinancialDetail.DeleteDataByKey(sfd.FinancialHeaderID, sfd.FinancialDetailID, sqltrans);
                }

                #endregion

                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateFinancialDetails");
            }

            return result;
        }

        public static bool DeleteDataByKey(SqlTransaction objTransaction, int FinancialID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@FinancialID", FinancialID);

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

        public static DataTable Get_UniqueFinacialReportNames(string whereFilter = "", SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet dsToReturn = null;

                if (objConnection != null)
                    dsToReturn = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetUniqueFinancialReportName, whereFilter));
                else
                    dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetUniqueFinancialReportName, whereFilter));

                if (dsToReturn != null && dsToReturn.Tables.Count > 0)
                    dtToReturn = dsToReturn.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        /// <summary>
        /// Get all Finanacial Header records
        /// </summary>
        /// <param name="noteNo">Note Number</param>
        /// <param name="sequenceAscending">Element Sequence</param>
        /// <param name="objConnection"></param>
        /// <returns></returns>
        public static DataTable Get_Data_By_NoteNo(string noteNo, bool sequenceAscending = true, SqlConnection objConnection = null)
        {
            DataSet dsToReturn = null;
            DataTable dtToReturn = null;
            string orderFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@NoteNo", noteNo);

                if (sequenceAscending)
                    orderFilter = " ORDER BY ElementSequence ";
                else
                    orderFilter = " ORDER BY ElementSequence DESC";

                if (objConnection != null)
                    dsToReturn = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_Select_By_NoteNo, orderFilter), sqlparams);
                else
                    dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Select_By_NoteNo, orderFilter), sqlparams);

                if (dsToReturn != null && dsToReturn.Tables.Count > 0)
                    dtToReturn = dsToReturn.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetAllSequence(short ElementSeq,string Finname)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@ElementSequence", ElementSeq);
                sqlparams[1] = new SqlParameter("@FinancialName", Finname);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_AllSequence, sqlparams);

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
        public static DataTable Get_NoteNoByReportName(string ReportName="", bool sequenceAscending = true, SqlConnection objConnection = null)
        {
            DataSet dsToReturn = null;
            DataTable dtToReturn = null;
            string orderFilter = string.Empty;
            string filters = string.Empty;
            try
            {
               
                if (sequenceAscending)
                    orderFilter = " ORDER BY ElementSequence ";
                else
                    orderFilter = " ORDER BY ElementSequence DESC";
                if (ReportName != "")
                    filters = " AND FinancialName='" + ReportName + "'";
                if (objConnection != null)
                    dsToReturn = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectNoteNo_By_ReportName,filters, orderFilter));
                else
                    dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectNoteNo_By_ReportName, filters, orderFilter));

                if (dsToReturn != null && dsToReturn.Tables.Count > 0)
                    dtToReturn = dsToReturn.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        #endregion
    }
}
