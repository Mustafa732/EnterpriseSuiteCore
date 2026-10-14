using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_Period
    {
        #region Constant Queries

        const string Query_SelectAllPeriodAndType_ByUserId = @" SELECT UsersInPeriod.UserId, set_Period.PeriodID, UsersInPeriod.CompanyID, set_Period.PeriodSetName, 
                SET_PeriodType.PeriodTypeName, set_Period.StartDate, set_Period.EndDate, CONVERT(bit, 'True') AS IsAssigned , 0 As FiscalID
            FROM Set_Period 
                INNER JOIN SET_PeriodType ON SET_PeriodType.PeriodTypeID = set_Period.PeriodTypeID 
                INNER JOIN UsersInPeriod ON UsersInPeriod.PeriodID = set_Period.PeriodID 
            WHERE set_Period.[Status] = 1 AND SET_Period.[IsActive] = 1 AND SET_PeriodType.[Status] = 1 AND UserId = @UserId; ";

        const string Query_SelectAllPeriodCustomAndType_ByUserId = @" SELECT UsersInPeriod.UserId, Set_PeriodCustom.PeriodID, UsersInPeriod.CompanyID, Set_PeriodCustom.PeriodSetName, 
                Set_PeriodCustom.PeriodTypeName, Set_PeriodCustom.StartDate, Set_PeriodCustom.EndDate, 
                CONVERT(bit, 'True') AS IsAssigned , Set_PeriodCustom.FiscalId 
            FROM Set_PeriodCustom 
                --INNER JOIN SET_PeriodType ON SET_PeriodType.PeriodTypeID = Set_PeriodCustom.PeriodTypeID 
                INNER JOIN UsersInPeriod ON UsersInPeriod.FiscalID = Set_PeriodCustom.FiscalId 
				AND  UsersInPeriod.PeriodID = Set_PeriodCustom.PeriodID 
            WHERE Set_PeriodCustom.[Status] = 1 AND Set_PeriodCustom.[IsActive] = 1  AND UserId = @UserId; ";

        const string Query_SelectAll_ByPeriodIds = @" SELECT @UserId UserId, set_Period.PeriodID, @CompanyID CompanyID, set_Period.PeriodSetName, 
                SET_PeriodType.PeriodTypeName, set_Period.StartDate, set_Period.EndDate, CONVERT(bit, 'False') AS IsAssigned 
            FROM set_Period 
                INNER JOIN SET_PeriodType ON SET_PeriodType.PeriodTypeID = set_Period.PeriodTypeID";    //  Please be careful, WHERE clause is being appended from GetAllData(string strPeriodIDs, Guid? userId, int companyID) function

        const string Query_SelectAll_ByPeriodCustomIds = @" SELECT @UserId UserId, SET_PeriodCustom.PeriodID, @CompanyID CompanyID, SET_PeriodCustom.PeriodSetName, 
                SET_PeriodCustom.PeriodTypeName, SET_PeriodCustom.StartDate, SET_PeriodCustom.EndDate, CONVERT(bit, 'False') AS IsAssigned ,SET_PeriodCustom.FiscalId
            FROM SET_PeriodCustom
                 ";

        const string Query_SelectAllPeriodAndTypesFiscal = @" SELECT set_PeriodCustom.PeriodID, set_PeriodCustom.PeriodSetName, set_PeriodCustom.PeriodTypeName, set_PeriodCustom.StartDate, 
                set_PeriodCustom.EndDate, 'False' AS IsAssigned ,FiscalId,CompanyID
            FROM set_PeriodCustom 
            WHERE set_PeriodCustom.[Status] = 1 AND set_PeriodCustom.[IsActive] = 1";

        const string Query_SelectAllPeriodAndTypes = @" SELECT set_Period.PeriodID, set_Period.PeriodSetName, SET_PeriodType.PeriodTypeName, set_Period.StartDate, 
                set_Period.EndDate, 'False' AS IsAssigned 
            FROM set_Period 
                INNER JOIN SET_PeriodType ON SET_PeriodType.PeriodTypeID = set_Period.PeriodTypeID 
            WHERE set_Period.[Status] = 1 AND SET_Period.[IsActive] = 1 AND SET_PeriodType.[Status] = 1; ";

        const string IsValidPeriod_ByPeriodDateAndPeriodID = @" SELECT Set_Period.StartDate, Set_Period.EndDate, 
                CASE WHEN tbl.HasPeriodID > 0 THEN 'True' ELSE 'False' END AS IsValidPeriod 
            FROM Set_Period 
                LEFT JOIN
                (
                    SELECT COUNT(PeriodID) AS HasPeriodID, PeriodID 
                    FROM Set_Period WHERE PeriodID = @PeriodID AND @PeriodDate BETWEEN StartDate AND EndDate AND [Status] = 1 AND [IsActive] = @IsActive 
                    GROUP BY PeriodID
                ) tbl ON tbl.PeriodID = SET_Period.PeriodID 
            WHERE Set_Period.PeriodID = @PeriodID AND Set_Period.[Status] = 1 AND SET_Period.[IsActive] = @IsActive 
            GROUP BY Set_Period.StartDate, Set_Period.EndDate, tbl.HasPeriodID; ";

        const string Query_GetPeriodList = @" 
SELECT [SET_Period].[PeriodID], [SET_Period].[PeriodSetName], [SET_Period].[Description], [SET_Period].[StartDate], [SET_Period].[EndDate], 
    [SET_Period].[PeriodTypeID], [SET_PeriodType].[PeriodTypeName], [SET_Period].[PeriodYear], [SET_Period].[PeriodNumber], [SET_Period].[IsActive] AS Status 
FROM [SET_Period] 
    INNER JOIN [SET_PeriodType] ON [SET_Period].[PeriodTypeID] = [SET_PeriodType].[PeriodTypeID] 
WHERE [SET_Period].Status = 1 AND ([SET_Period].IsActive = @IsActive OR @IsActive IS NULL) AND (SET_Period.PeriodTypeID = @PeriodTypeID OR @PeriodTypeID IS NULL) ";


        const string Query_GetPeriodList_Previous = @" 
SELECT [SET_Period].[PeriodID], [SET_Period].[PeriodSetName], [SET_Period].[Description], [SET_Period].[StartDate], [SET_Period].[EndDate], 
    [SET_Period].[PeriodTypeID], [SET_PeriodType].[PeriodTypeName], [SET_Period].[PeriodYear], [SET_Period].[PeriodNumber], [SET_Period].[IsActive] AS Status 
FROM [SET_Period] 
    INNER JOIN [SET_PeriodType] ON [SET_Period].[PeriodTypeID] = [SET_PeriodType].[PeriodTypeID] 
WHERE [SET_Period].Status = 1 and [SET_Period].Status =1 and [SET_Period].PeriodID < @PeriodID ";



        #endregion

        #region Class Methods

        public static DataRow SetFiscalyearDate(int periodID,int FiscalID)
        {
            DataRow drToReturn = null;

            try
            {
//                string Query = @"
//                        select * from SET_Period 
//                            where PeriodID =  @PeriodID and  Status = 1 and IsActive = 1 
//                ";


                string Query = @"
                                                select 

                        SET_Period.PeriodId,
                        isnull(SET_PeriodCustom.PeriodSetName,SET_Period.PeriodSetName)PeriodSetName,
                        SET_Period.Description,

                        
                        isnull(SET_PeriodCustom.StartDate,SET_Period.StartDate)StartDate,
                        isnull(SET_PeriodCustom.EndDate,SET_Period.EndDate)EndDate,

                        SET_Period.PeriodTypeID,
                        SET_Period.PeriodYear,
                        SET_Period.PeriodNumber,
                        SET_Period.CreatedBy,
                        SET_Period.CreateDate,
                        SET_Period.UpdatedBy,
                        SET_Period.UpdateDate,
                        SET_Period.CreatedByValue,
                        SET_Period.UpdatedByValue,
                        SET_Period.Status,
                        SET_Period.IsActive


                         from SET_Period

                        left join SET_PeriodCustom on SET_PeriodCustom.PeriodId =  SET_Period.PeriodID  {0} and SET_PeriodCustom.Status =1

                            where SET_Period.PeriodID =  @PeriodID and  SET_Period.Status = 1 and SET_Period.IsActive = 1 
                ";




                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);


                string append = string.Empty; 
                
                if(FiscalID>0)
                    append = " and SET_PeriodCustom.FiscalID =  " + FiscalID;


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query,append), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }


        public static DataTable GetAllActiveDataByID(int periodID)
        {
            DataTable dt = null;

            SqlParameter [] sqlpar = new SqlParameter[1];
            sqlpar[0] = new SqlParameter("@PeriodID", periodID);

            const string Query_SelectDataBYID = @" SELECT * FROM [SET_Period] WHERE [Status] = 1 AND [IsActive] = 1 AND [PeriodID]= @PeriodID ";

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectDataBYID,sqlpar).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllPeriodAndTypeDataByUserId(Guid userID,bool isFiscalSystem=false)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserId", userID);

                string query = string.Empty;

                if(isFiscalSystem)
                    query = Query_SelectAllPeriodCustomAndType_ByUserId;
                else
                    query = Query_SelectAllPeriodAndType_ByUserId;

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

        public static DataTable GetAllData(string strPeriodIDs,string strFiscalIDs, Guid? userId, int companyID, bool isFiscalSystem)
        {
            DataTable dtToReturn = null;

            try
            {
                string periodids = string.Empty;
                string strWhereRoleids = string.Empty;

                if (isFiscalSystem)
                {
                    periodids = strFiscalIDs;
                    strWhereRoleids = strFiscalIDs != string.Empty ? " WHERE set_PeriodCustom.[Status] = 1 and set_PeriodCustom.FiscalID not in ( " + strFiscalIDs + ")" : " where set_Period.[Status] = 1 ";
                }
                else
                {
                    periodids = strPeriodIDs;
                    strWhereRoleids = strPeriodIDs != string.Empty ? " WHERE set_Period.[Status] = 1 and SET_PeriodType.[Status] = 1 and set_Period.PeriodID not in ( " + strPeriodIDs + ")" : " where set_Period.[Status] = 1 and SET_PeriodType.[Status] = 1 ";
                }

                
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@UserId", userId);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                string query = string.Empty;

                if (isFiscalSystem)
                {
                    query = Query_SelectAll_ByPeriodCustomIds;
                    strWhereRoleids += " and SET_PeriodCustom.companyID = " + companyID + "";

                }
                else
                    query = Query_SelectAll_ByPeriodIds;


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query + strWhereRoleids, sqlparams);

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

        public static DataTable GetAllPeriodAndTypeData(bool isFiscalSystem = false)
        {
            DataTable dtToReturn = null;

            try
            {
                string query = string.Empty;

                if (isFiscalSystem)
                    query = Query_SelectAllPeriodAndTypesFiscal;
                else
                    query = Query_SelectAllPeriodAndTypes;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query);

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

        public static DataTable ValidPeriod(int periodID, DateTime periodDate, bool isActive = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@PeriodDate", periodDate);
                sqlparams[2] = new SqlParameter("@IsActive", isActive);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, IsValidPeriod_ByPeriodDateAndPeriodID, sqlparams);

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
        
        public static DataTable GetPeriodList(Nullable<int> periodType = null, Nullable<int> isActive = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                if (periodType.HasValue && !periodType.ToString().Equals("-1"))
                    sqlparams[0] = new SqlParameter("@PeriodTypeID", periodType.Value);
                else
                    sqlparams[0] = new SqlParameter("@PeriodTypeID", DBNull.Value);

                if (isActive.HasValue && !isActive.ToString().Equals("-1"))
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPeriodList, sqlparams);

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


        public static DataTable GetPeriodList_Previous(int PeriodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];


                if (PeriodID>0)
                    sqlparams[0] = new SqlParameter("@PeriodID", PeriodID);
                else
                    sqlparams[0] = new SqlParameter("@PeriodID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPeriodList_Previous, sqlparams);

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


        public static DataTable GetPeriodList_PreviousFiscal(int PeriodID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];


                if (CurrentFiscalID > 0)
                    sqlparams[0] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                else
                    sqlparams[0] = new SqlParameter("@CurrentFiscalID", DBNull.Value);


                string query = @"SELECT [SET_PeriodCustom].FiscalId, [SET_PeriodCustom].[PeriodSetName], [SET_PeriodCustom].[StartDate],[SET_PeriodCustom].EndDate
FROM [SET_PeriodCustom] 
   
WHERE [SET_PeriodCustom].Status = 1 and [SET_PeriodCustom].Status =1 and [SET_PeriodCustom].FiscalID < @CurrentFiscalID ";

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

        public static DataRow GetPeriodStartDate(int periodID)
        {
            DataRow drToReturn = null;

            #region
            const string Query_GetStartDate = @"Select StartDate from SET_Period where PeriodID=@PeriodID and Status=1";
            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                    sqlparams[0] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetStartDate, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataRow GetPeriodStartDate(int periodID, int CurrentFiscalID)
        {
            DataRow drToReturn = null;

            #region
            string Query_GetStartDate = string.Empty;
            if(CurrentFiscalID>0)
                Query_GetStartDate = @"Select StartDate from SET_PeriodCustom where PeriodID=@PeriodID  and FiscalID = @CurrentFiscalID and Status=1";
            else
                Query_GetStartDate = @"Select StartDate from SET_Period where PeriodID=@PeriodID and Status=1";

            
            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetStartDate, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataRow GetEndDateOfPrevoiusFiscal(int periodID, int CurrentFiscalID)
        {
            DataRow drToReturn = null;

            #region
            string Query = string.Empty;

            Query = @"Select DATEADD(day,-1,StartDate) as EndDateOfPrevoisFiscal from SET_PeriodCustom where PeriodID=@PeriodID  and FiscalID = @CurrentFiscalID and Status=1";

            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }



        public static DataRow GetCompanyFistFiscalDate()
        {
            DataRow drToReturn = null;

            #region
            string Query_GetStartDate = string.Empty;
            Query_GetStartDate = @"SELECT MIN(StartDate)StartDate FROM SET_PeriodCustom where status = 1";
            #endregion
            try
            {
              
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetStartDate);

                if (ds != null && ds.Tables.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static int GetCompanyPrevoiusFiscalID(int CurrentFiscalID=0, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
             int fiscalid=0 ;
            SqlParameter[] sqlparams = new SqlParameter[1];
            sqlparams[0] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
            #region
            string Query_GetStartDate = string.Empty;
            Query_GetStartDate = @"SELECT MAX(FiscalId) FROM SET_PeriodCustom where status = 1 
AND EndDate <(

SELECT StartDate FROM SET_PeriodCustom where status = 1 ANd FiscalId=@CurrentFiscalID)";
            #endregion
            try
            {
                if (objTransaction != null)
                    fiscalid = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetStartDate, sqlparams));
                else if (objConnection != null)
                    fiscalid = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetStartDate, sqlparams));
                else
                    fiscalid = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetStartDate, sqlparams));

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

           return fiscalid;
        }

        public static DataRow GetPeriodStartEndDate(int periodID, int CurrentFiscalID)
        {
            DataRow drToReturn = null;

            #region
            string Query_GetStartDate = string.Empty;
            if (CurrentFiscalID > 0)
                Query_GetStartDate = @"Select StartDate,EndDate from SET_PeriodCustom where PeriodID=@PeriodID  and FiscalID = @CurrentFiscalID and Status=1";
            else
                Query_GetStartDate = @"Select StartDate,EndDate from SET_Period where PeriodID=@PeriodID and Status=1";


            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetStartDate, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        #endregion

        public static DataRow GetPeriodLastDate(int periodID, int CurrentFiscalID)
        {
            DataRow drToReturn = null;

            #region
            string Query_GetStartDate = string.Empty;
            if (CurrentFiscalID > 0)
                Query_GetStartDate = @"Select EndDate from SET_PeriodCustom where PeriodID=@PeriodID  and FiscalID = @CurrentFiscalID and Status=1";
            else
                Query_GetStartDate = @"Select EndDate from SET_Period where PeriodID=@PeriodID and Status=1";


            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetStartDate, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }


        public static DataTable GetPreviousFiscalID(int companyID, int periodID, int fiscalID)
        {
            DataTable dt = null;

            SqlParameter[] sqlpar = new SqlParameter[3];
            sqlpar[0] = new SqlParameter("@CompanyID", companyID);
            sqlpar[1] = new SqlParameter("@PeriodID", periodID);
            sqlpar[2] = new SqlParameter("@FiscalID", fiscalID);

            const string Query_SelectDataBYID = @"Select top 1 Set_PeriodCustom.FiscalID, Set_PeriodCustom.CompanyID from Set_PeriodCustom 
          left join SET_Period on SET_Period.PeriodId =  SET_Period.PeriodID and SET_Period.Status =1  
  where Set_PeriodCustom.Status = 1 AND Set_PeriodCustom.CompanyID = @CompanyID 
     AND Set_PeriodCustom.PeriodID =  @PeriodID AND Set_PeriodCustom.FiscalId < @FiscalID
    order by FiscalId desc ";

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectDataBYID, sqlpar).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static int GetFiscalIDByDate(int CompanyId, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate)
        {
            int fiscalID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyId);
                sqlparams[1] = new SqlParameter("@FromDate", FromDate);
                sqlparams[2] = new SqlParameter("@ToDate", ToDate);


                string query = @"Select FiscalId From SET_PeriodCustom Where CompanyID = @CompanyID And @FromDate Between StartDate And EndDate And @ToDate Between StartDate And EndDate";
                

                fiscalID = int.Parse(SqlHelper.ExecuteScalar(CommandType.Text, query, sqlparams).ToString());

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return fiscalID;
        }

        public static DataTable GetFiscalIDByDate_dt(int CompanyId, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate)
        {
            DataTable dt = new DataTable();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyId);
                sqlparams[1] = new SqlParameter("@FromDate", FromDate);
                sqlparams[2] = new SqlParameter("@ToDate", ToDate);


                string query = @"Select FiscalId From SET_PeriodCustom Where CompanyID = @CompanyID  
AND (StartDate Between @FromDate And @ToDate OR EndDate Between @FromDate And @ToDate)";


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                dt = ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static int GetLastFiscalID(int CompanyId,SqlTransaction objTransaction = null)
        {
            int fiscalID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyId);


                string query = @"select top 1 fiscalID from set_PeriodCustom where CompanyID = 1 order by EndDate desc";

                if (objTransaction != null)
                    fiscalID = int.Parse(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, query, sqlparams).ToString());
                else
                    fiscalID = int.Parse(SqlHelper.ExecuteScalar(CommandType.Text, query, sqlparams).ToString());

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return fiscalID;
        }

    }
}

