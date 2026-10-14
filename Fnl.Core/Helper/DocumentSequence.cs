using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Data;
using Common.Utils;

using FnI.BusinessAccessLayer.Setup;

namespace FnI.Core.Helper
{
    public class DocumentSequence
    {
        public static int GetDocumentNo(int documentTypeId, int companyId, int periodID, int fiscalID = 0)
        {
            

            DataTable dt = SET_DocumentSequence.HasChilds(documentTypeId, companyId, periodID,true,null, fiscalID);


            int documentno = int.Parse(dt.Rows[0]["DocumentNo"].ToString());
            DataRow[] row = dt.Select("HasRangeDefined = 1");

            if (row.Count() > 0)
            {
                bool isProperRangeFound = false;

                foreach (DataRow r in row)
                {
                    if (documentno >= int.Parse(r["FromNo"].ToString()) && documentno <= int.Parse(r["ToNo"].ToString()))
                    {
                        isProperRangeFound = true;
                        break;
                    }

                    if (documentno < int.Parse(r["FromNo"].ToString()))
                    {
                        documentno = int.Parse(r["FromNo"].ToString());
                        isProperRangeFound = true;
                        break;
                    }
                }

                if (!isProperRangeFound)
                    throw new BusinessLogicException("Not any valid Document Number range found");
            }

            return documentno;
        }

        public static int GetDocumentNoforPF(int documentTypeId, int companyId, int periodID, string DatabaseName, int fiscalID = 0)
        {


            DataTable dt = SET_DocumentSequence.HasChildsForPF(documentTypeId, companyId, periodID, DatabaseName, true, null, fiscalID);


            int documentno = int.Parse(dt.Rows[0]["DocumentNo"].ToString());
            DataRow[] row = dt.Select("HasRangeDefined = 1");

            if (row.Count() > 0)
            {
                bool isProperRangeFound = false;

                foreach (DataRow r in row)
                {
                    if (documentno >= int.Parse(r["FromNo"].ToString()) && documentno <= int.Parse(r["ToNo"].ToString()))
                    {
                        isProperRangeFound = true;
                        break;
                    }

                    if (documentno < int.Parse(r["FromNo"].ToString()))
                    {
                        documentno = int.Parse(r["FromNo"].ToString());
                        isProperRangeFound = true;
                        break;
                    }
                }

                if (!isProperRangeFound)
                    throw new BusinessLogicException("Not any valid Document Number range found");
            }

            return documentno;
        }

        public static int GetDocumentNoAS(int documentTypeId, int companyId, int periodID)
        {
            DataTable dt = SET_DocumentSequence.HasChildsforAS(documentTypeId, companyId, periodID);
            int documentno = int.Parse(dt.Rows[0]["DocumentNo"].ToString());
            DataRow[] row = dt.Select("HasRangeDefined = 1");
            bool isProperRangeFound = false;

            if (row.Count() > 0)
            {
                foreach (DataRow r in row)
                {
                    if (documentno >= int.Parse(r["FromNo"].ToString()) && documentno <= int.Parse(r["ToNo"].ToString()))
                    {
                        isProperRangeFound = true;
                        break;
                    }

                    if (documentno < int.Parse(r["FromNo"].ToString()))
                    {
                        documentno = int.Parse(r["FromNo"].ToString());
                        isProperRangeFound = true;
                        break;
                    }
                }

                if (!isProperRangeFound)
                    throw new BusinessLogicException("Not any valid Document Number range found");
            }

            return documentno;
        }

        public static bool IsBackDateAllow(int companyID, int documentTypeID, Guid UserID, DateTime documentDate, SqlConnection obj_SQLConnection = null)
        {
            if (FnI.Core.Web.HttpApplicationState.AppParameters.SecurityOnDocumentType)
            {
                if (documentDate.Date < DateTime.Now.Date)
                    return !SET_DocumentType.GetAllIsBackDateAllow(companyID, documentTypeID, UserID, obj_SQLConnection);
                else
                    return true;
            }
            else 
                return true;
        }

        public static bool isThisMonthAllowed(int companyID, int documentTypeID,int MonthID, Guid UserID, DateTime documentDate, SqlConnection obj_SQLConnection = null)
        {
            if (FnI.Core.Web.HttpApplicationState.AppParameters.SecurityOnDocumentType)
            {
                if (documentDate.Date < DateTime.Now.Date)
                    return !SET_DocumentType.GetAllBlockedMonths(companyID, documentTypeID,MonthID, UserID, obj_SQLConnection);
                else
                    return true;
            }
            else
                return true;
        }

        public static bool IsAllowPreviousMonth(int companyID, int documentTypeID, Guid UserID, DateTime documentDate, SqlConnection obj_SQLConnection = null)
        {
            if (FnI.Core.Web.HttpApplicationState.AppParameters.SecurityOnDocumentType)
            {
               DataRow dr = SET_PeriodCustom.Get_FirstLastDateOfMonth(DateTime.Now.AddMonths(-1));
               DateTime previousDate = Convert.ToDateTime(dr["EndDate"].ToString());
               if (documentDate.Date <= previousDate)
                    return !SET_DocumentType.GetAllIsAllowPreviousMonth(companyID, documentTypeID, UserID, obj_SQLConnection);
                else
                    return true;
            }
            else
                return true;
        }
    }
}
