using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;


namespace FnI.BusinessAccessLayer.GL.Setup
{
   public partial class GL_Notes_Header
       
    {

       

        public static bool InsertNotesHeaderandDetail(FnI.BusinessAccessLayer.GL.Setup.GL_Notes_Header obj, List<FnI.BusinessAccessLayer.GL.Setup.GL_NotesDetails> colnoteItems)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertNotesHeaderAndDetail");
            try
            {
               obj.NoteID = (int)FnI.BusinessAccessLayer.GL.Setup.GL_Notes_Header.GetNewNoteID(obj.CompanyID);

                if (!obj.Insert(sqltrans))
                    throw new BusinessLogicException("Record is not inserted in Notes Header");

                foreach (FnI.BusinessAccessLayer.GL.Setup.GL_NotesDetails td in colnoteItems)
                {
                    td.NoteID = obj.NoteID;
                    td.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertNotesHeaderAndDetail");
            }

            return result;
        }

        public static bool UpdateRackHeaderandDetail(FnI.BusinessAccessLayer.GL.Setup.GL_Notes_Header obj, List<FnI.BusinessAccessLayer.GL.Setup.GL_NotesDetails> colnotesItems)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateNotesHeaderAndDetail");
            try
            {


                if (!obj.UpdateDataByKey(sqltrans))
                    throw new BusinessLogicException("Record is not updated in Notes Header");
                 //foreach (GL_NotesDetails ardDelete in colSPDetDelete)
                 //    GL_NotesDetails.delete(obj.NoteID,obj.CompanyID,sqltrans);
                GL_NotesDetails.delete(obj.NoteID,obj.CompanyID, sqltrans);

                foreach (GL_NotesDetails td in colnotesItems)
                {
                    td.NoteID = obj.NoteID;
                    td.Insert(sqltrans);
                }
                result = true;

             //   foreach (FnI.BusinessAccessLayer.GL.Setup.GL_NotesDetails td in colnotesItems)
              //  {
              //      td.NoteID = obj.NoteID;
             //       td.Insert(sqltrans);
            //    }
            //    result = true;

              // foreach (FnI.BusinessAccessLayer.GL.Setup.GL_NotesDetails ardUpate in colTranDetUpdate)
               //   ardUpate.Insert(sqlTrans);
              //  GL_NotesDetails.delete(obj.NoteID, obj.CompanyID, sqltrans);
               
             //   result = true;

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertNotesHeaderAndDetail");
            }

            return result;
        }

     
    }
}
