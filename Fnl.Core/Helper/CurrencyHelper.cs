using System;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.Core.Helper
{
    public class CurrencyHelper
    {
        #region Const string

        const string Query_GetCurrencyIdByCompanyID = @"
Select SET_Currency.CurrencyID, SET_Currency.CurrencyName
From Set_Company
    INNER JOIN SET_OperatingUnit on SET_OperatingUnit.OperatingUnitID = Set_Company.OperatingUnitID
    INNER JOIN SET_Currency on SET_Currency.CurrencyID = SET_OperatingUnit.BaseCurrencyID
Where Set_Company.CompanyId = @CompanyID ";

        #endregion

        #region Class Methods -.NET 8 MVC Version

        public static (SelectList Currencies, bool IsEnabled, int BaseCurrencyID) LoadWithBaseCurrency(int companyID, string mode)
        {
            SET_Company objCompany = new SET_Company(companyID);
            bool isEnabled = true;
            int baseCurrencyID = 0;

            if (objCompany.IsDataLoaded)
            {
                baseCurrencyID = objCompany.OperatingUnitId_OperatingUnit.BaseCurrencyID;

                if (objCompany.OperatingUnitId_OperatingUnit.MultiCurrency.HasValue &&
                    objCompany.OperatingUnitId_OperatingUnit.MultiCurrency.Value)
                    isEnabled = true;
                else
                    isEnabled = false;

                var allCurrencies = GetAllCurrencies();

                // FIX: DataTable ko List<SelectListItem> me convert karo
                var items = allCurrencies.AsEnumerable().Select(row => new SelectListItem
                {
                    Value = row["CurrencyID"].ToString(),
                    Text = row["CurrencyName"].ToString()
                }).ToList();

                var selectList = new SelectList(items, "Value", "Text", baseCurrencyID.ToString());
                return (selectList, isEnabled, baseCurrencyID);
            }

            // fallback
            var fallback = GetAllCurrencies();
            var fallbackItems = fallback.AsEnumerable().Select(row => new SelectListItem
            {
                Value = row["CurrencyID"].ToString(),
                Text = row["CurrencyName"].ToString()
            }).ToList();

            return (new SelectList(fallbackItems, "Value", "Text"), true, 0);
        }

        public static List<SelectListItem> GetCurrencySelectList(int companyID, string mode, out bool isEnabled)
        {
            var result = LoadWithBaseCurrency(companyID, mode);
            isEnabled = result.IsEnabled;

            // SelectList ko List<SelectListItem> me convert
            return result.Currencies.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text,
                Selected = x.Value == result.BaseCurrencyID.ToString()
            }).ToList();
        }

        private static DataTable GetAllCurrencies()
        {
            try
            {
                return SET_Currency.GetAllActiveData();
            }
            catch
            {
                return new DataTable();
            }
        }

        public static DataRow GetCurrencyByCompanyIDint(int companyID)
        {
            DataRow dr = null;
            try
            {
                string Query = Query_GetCurrencyIdByCompanyID;
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw;
            }
            return dr;
        }

        public static int GetDefaultCurrency(int companyID, SqlConnection objConnection = null)
        {
            int currencyID = -1;
            DataSet ds = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetCurrencyIdByCompanyID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCurrencyIdByCompanyID, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0 && ds.Tables[0].Rows != null && ds.Tables[0].Rows.Count > 0)
                    currencyID = Convert.ToInt32(ds.Tables[0].Rows[0]["CurrencyID"]);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw;
            }
            return currencyID;
        }

        #endregion
    }
}