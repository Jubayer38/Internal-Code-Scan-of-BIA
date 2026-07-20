using BIA.DAL.DBManager;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using BIA.Entity.ViewModel;

namespace BIA.DAL.Repositories
{
    public class DALForbiddenWords
    {
        private readonly OracleDataManagerV2 _odm = new OracleDataManagerV2();

        //public async Task<List<string>> GetForbiddenWordsFromDBAsync()
        //{
        //    var words = new List<string>();

        //    OracleParameter poWordsParam = new OracleParameter("PO_WORDS", OracleDbType.RefCursor, ParameterDirection.Output);
        //    DataTable dt = await _odm.SelectProcedure("GET_FORBIDDEN_WORDS", poWordsParam);

        //    if (dt == null || dt.Rows.Count == 0) return words;

        //    foreach (DataRow row in dt.Rows)
        //    {
        //        var word = row[0]?.ToString().Trim();
        //        if (!string.IsNullOrWhiteSpace(word))
        //            words.Add(word.ToLowerInvariant());
        //    }

        //    return words;
        //}

        public async Task<List<ForbiddenWords>> GetForbiddenWordsFromDBAsync()
        {
            var words = new List<ForbiddenWords>();

            using (OracleParameter poWordsParam = new OracleParameter("PO_WORDS", OracleDbType.RefCursor, ParameterDirection.Output))
            {
                using (DataTable dt = await _odm.SelectProcedure("GET_FORBIDDEN_WORDS", poWordsParam))
                {
                    if (dt == null || dt.Rows.Count == 0) return words;

                    foreach (DataRow row in dt.Rows)
                    {
                        words.Add(new ForbiddenWords
                        {
                            Word = row["NOT_ALLOWED_WORDS"]?.ToString().Trim(),
                            Alternate = row["ALTERNATE_WORD"]?.ToString().Trim()
                        });
                    }
                }
            }

            return words;
        }
    }
}
